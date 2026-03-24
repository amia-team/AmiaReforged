using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.ValueObjects;
using Anvil;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;
using NLog;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Application;

/// <summary>
/// Hooks into NPC conversation events to detect dialogue-enabled NPCs
/// and launch the custom NUI conversation window instead of the default NWN dialogue.
///
/// Maintains an internal registry of <c>dialogueTreeId → speakerTag</c> ownership so that
/// tag reassignment is safe: only NPCs whose local variable still matches the tree being
/// unregistered are affected, preventing accidental removal of hooks owned by a different tree.
///
/// Supports dynamic registration: when a dialogue tree is created/updated/deleted via the
/// admin panel API, call <see cref="RegisterNpcsForTreeAsync"/>,
/// <see cref="UnregisterNpcsForTreeAsync"/>, or <see cref="UpdateNpcRegistrationAsync"/>
/// to hot-wire NPCs without a server restart.
/// </summary>
[ServiceBinding(typeof(DialogueNpcHook))]
public sealed class DialogueNpcHook
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Local variable name set on NPCs to indicate they use the World Engine dialogue system.
    /// Value is the dialogue tree ID.
    /// </summary>
    public const string DialogueTreeVarName = "we_dialogue_tree";

    /// <summary>Tracks which creatures already have the hook registered to avoid double-subscribe.</summary>
    private readonly HashSet<NwCreature> _hookedCreatures = new();
    private readonly object _hookLock = new();

    /// <summary>
    /// Ownership registry: maps each <c>dialogueTreeId</c> to the <c>speakerTag</c> it currently
    /// claims. Used to safely unregister without clobbering NPCs owned by a different tree.
    /// </summary>
    private readonly Dictionary<string, string> _treeToTag = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _registryLock = new();

    [Inject] private Lazy<DialogueService>? DialogueService { get; init; }

    public DialogueNpcHook()
    {
        // Hook any NPCs that already have the local variable set (e.g. from a previous
        // server session where the variable was stamped at runtime but persisted in save).
        List<NwCreature> dialogueNpcs = NwObject.FindObjectsOfType<NwCreature>()
            .Where(c => !string.IsNullOrEmpty(
                c.GetObjectVariable<LocalVariableString>(DialogueTreeVarName).Value))
            .ToList();

        foreach (NwCreature npc in dialogueNpcs)
        {
            string treeId = npc.GetObjectVariable<LocalVariableString>(DialogueTreeVarName).Value;
            if (!string.IsNullOrEmpty(treeId))
            {
                lock (_registryLock)
                {
                    _treeToTag[treeId] = npc.Tag;
                }
            }
            HookCreature(npc);
        }

        Log.Info(
            "DialogueNpcHook registered — {NpcCount} dialogue-enabled NPCs found at startup, {TreeCount} trees in registry",
            dialogueNpcs.Count, _treeToTag.Count);

        // Subscribe to module load to register NPCs from the database.
        // At module load time, all areas/creatures are fully spawned and the DB is reachable.
        NwModule.Instance.OnModuleLoad += HandleModuleLoad;
    }

    /// <summary>
    /// On module load, queries the database for all dialogue trees with a speaker tag
    /// and registers matching NPCs. This ensures NPCs are wired up on boot without
    /// requiring a manual re-save from the admin panel.
    /// </summary>
    private void HandleModuleLoad(ModuleEvents.OnModuleLoad _)
    {
        try
        {
            PwContextFactory? factory = AnvilCore.GetService<PwContextFactory>();
            if (factory == null)
            {
                Log.Warn("DialogueNpcHook: PwContextFactory not available at module load — skipping DB registration");
                return;
            }

            using PwEngineContext context = factory.CreateDbContext();
            List<PersistedDialogueTree> trees = context.DialogueTrees
                .Where(t => t.SpeakerTag != null && t.SpeakerTag != "")
                .Select(t => new PersistedDialogueTree
                {
                    DialogueTreeId = t.DialogueTreeId,
                    Title = t.Title,
                    SpeakerTag = t.SpeakerTag
                })
                .ToList();

            int totalRegistered = 0;
            foreach (PersistedDialogueTree tree in trees)
            {
                if (string.IsNullOrWhiteSpace(tree.SpeakerTag)) continue;
                int count = RegisterNpcsForTree(tree.SpeakerTag, tree.DialogueTreeId);
                totalRegistered += count;
            }

            Log.Info(
                "DialogueNpcHook: module load registration complete — {TreeCount} trees, {NpcCount} NPCs registered",
                trees.Count, totalRegistered);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "DialogueNpcHook: failed to register dialogue NPCs from database at module load");
        }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Dynamic registration API (called from controllers / services)
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Finds all creatures with the given <paramref name="speakerTag"/>, sets the
    /// <c>we_dialogue_tree</c> local variable, and registers the conversation hook.
    /// Also records the tree→tag ownership in the internal registry.
    /// Must be called from the main server thread.
    /// </summary>
    /// <param name="speakerTag">The NPC creature tag to match.</param>
    /// <param name="dialogueTreeId">The dialogue tree ID to assign.</param>
    /// <returns>Number of NPCs registered.</returns>
    public int RegisterNpcsForTree(string speakerTag, string dialogueTreeId)
    {
        if (string.IsNullOrWhiteSpace(speakerTag) || string.IsNullOrWhiteSpace(dialogueTreeId))
            return 0;

        // Record ownership
        lock (_registryLock)
        {
            _treeToTag[dialogueTreeId] = speakerTag;
        }

        List<NwCreature> matchingNpcs = NwObject.FindObjectsWithTag<NwCreature>(speakerTag).ToList();
        int registered = 0;

        foreach (NwCreature npc in matchingNpcs)
        {
            npc.GetObjectVariable<LocalVariableString>(DialogueTreeVarName).Value = dialogueTreeId;
            HookCreature(npc);
            registered++;
        }

        Log.Info("DialogueNpcHook: registered {Count} NPCs with tag '{Tag}' → tree '{TreeId}'",
            registered, speakerTag, dialogueTreeId);

        return registered;
    }

    /// <summary>
    /// Async wrapper for <see cref="RegisterNpcsForTree"/> that switches to the main thread first.
    /// Safe to call from API controller context.
    /// </summary>
    public async Task<int> RegisterNpcsForTreeAsync(string speakerTag, string dialogueTreeId)
    {
        await NwTask.SwitchToMainThread();
        return RegisterNpcsForTree(speakerTag, dialogueTreeId);
    }

    /// <summary>
    /// Unregisters NPCs for a specific dialogue tree. Looks up the speaker tag from the
    /// internal registry and only clears the local variable / unhooks creatures whose
    /// <c>we_dialogue_tree</c> value still matches <paramref name="dialogueTreeId"/>.
    /// This ensures that if the tag was already reassigned to a different tree, those NPCs
    /// are left untouched.
    /// Must be called from the main server thread.
    /// </summary>
    /// <param name="dialogueTreeId">The dialogue tree ID to unregister.</param>
    /// <returns>Number of NPCs unregistered.</returns>
    public int UnregisterNpcsForTree(string dialogueTreeId)
    {
        if (string.IsNullOrWhiteSpace(dialogueTreeId))
            return 0;

        string? speakerTag;
        lock (_registryLock)
        {
            if (!_treeToTag.TryGetValue(dialogueTreeId, out speakerTag))
            {
                Log.Debug("DialogueNpcHook: no registry entry for tree '{TreeId}' — nothing to unregister",
                    dialogueTreeId);
                return 0;
            }
            _treeToTag.Remove(dialogueTreeId);
        }

        if (string.IsNullOrWhiteSpace(speakerTag))
            return 0;

        // Check if any other tree still claims this same tag
        bool tagStillOwnedByOtherTree;
        lock (_registryLock)
        {
            tagStillOwnedByOtherTree = _treeToTag.ContainsValue(speakerTag);
        }

        List<NwCreature> matchingNpcs = NwObject.FindObjectsWithTag<NwCreature>(speakerTag).ToList();
        int unregistered = 0;

        foreach (NwCreature npc in matchingNpcs)
        {
            string? currentTreeId = npc.GetObjectVariable<LocalVariableString>(DialogueTreeVarName).Value;

            // Only clear NPCs that still belong to this tree — don't clobber another tree's NPCs
            if (currentTreeId != dialogueTreeId) continue;

            npc.GetObjectVariable<LocalVariableString>(DialogueTreeVarName).Delete();

            // Only unhook the event if no other tree is using this tag
            // (if another tree owns the tag, it will re-stamp the var on its next register)
            if (!tagStillOwnedByOtherTree)
            {
                UnhookCreature(npc);
            }

            unregistered++;
        }

        Log.Info(
            "DialogueNpcHook: unregistered {Count} NPCs (tag '{Tag}') for tree '{TreeId}'{Shared}",
            unregistered, speakerTag, dialogueTreeId,
            tagStillOwnedByOtherTree ? " — tag still claimed by another tree, hooks kept" : "");

        return unregistered;
    }

    /// <summary>
    /// Async wrapper for <see cref="UnregisterNpcsForTree(string)"/> that switches to the main thread first.
    /// Safe to call from API controller context.
    /// </summary>
    /// <param name="dialogueTreeId">The dialogue tree ID to unregister.</param>
    public async Task<int> UnregisterNpcsForTreeAsync(string dialogueTreeId)
    {
        await NwTask.SwitchToMainThread();
        return UnregisterNpcsForTree(dialogueTreeId);
    }

    /// <summary>
    /// Handles a speaker tag change for a specific dialogue tree. Unregisters the old tag
    /// (tree-aware, only affecting NPCs still owned by this tree) and registers the new tag.
    /// Safe to call from API controller context.
    /// </summary>
    /// <param name="dialogueTreeId">The dialogue tree being updated.</param>
    /// <param name="newSpeakerTag">The new speaker tag (may be null to clear).</param>
    /// <returns>Tuple of (NPCs unregistered from old tag, NPCs registered on new tag).</returns>
    public async Task<(int unregistered, int registered)> UpdateNpcRegistrationAsync(
        string dialogueTreeId, string? newSpeakerTag)
    {
        await NwTask.SwitchToMainThread();

        // Look up the old tag from the registry before unregistering
        string? oldSpeakerTag;
        lock (_registryLock)
        {
            _treeToTag.TryGetValue(dialogueTreeId, out oldSpeakerTag);
        }

        int unregistered = 0;
        int registered = 0;

        // Unregister old tag if it changed or the new tag is being cleared
        bool tagChanged = !string.Equals(oldSpeakerTag, newSpeakerTag, StringComparison.OrdinalIgnoreCase);
        if (tagChanged && !string.IsNullOrWhiteSpace(oldSpeakerTag))
        {
            unregistered = UnregisterNpcsForTree(dialogueTreeId);
        }

        // Register new tag (also handles same-tag refresh — re-stamps the local var)
        if (!string.IsNullOrWhiteSpace(newSpeakerTag))
        {
            registered = RegisterNpcsForTree(newSpeakerTag, dialogueTreeId);
        }

        if (tagChanged)
        {
            Log.Info(
                "DialogueNpcHook: tag change for tree '{TreeId}': '{OldTag}' → '{NewTag}' " +
                "(unregistered {Unregistered}, registered {Registered})",
                dialogueTreeId, oldSpeakerTag ?? "(none)", newSpeakerTag ?? "(none)",
                unregistered, registered);
        }

        return (unregistered, registered);
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Registry inspection (for diagnostics / testing)
    // ═══════════════════════════════════════════════════════════════════

    /// <summary>Returns the speaker tag currently registered for the given tree, or null.</summary>
    public string? GetRegisteredTag(string dialogueTreeId)
    {
        lock (_registryLock)
        {
            return _treeToTag.GetValueOrDefault(dialogueTreeId);
        }
    }

    /// <summary>Returns a snapshot of all tree→tag registrations.</summary>
    public IReadOnlyDictionary<string, string> GetRegistrySnapshot()
    {
        lock (_registryLock)
        {
            return new Dictionary<string, string>(_treeToTag, StringComparer.OrdinalIgnoreCase);
        }
    }

    /// <summary>Returns the number of currently hooked creatures.</summary>
    public int HookedCreatureCount
    {
        get { lock (_hookLock) { return _hookedCreatures.Count; } }
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Internal hook management
    // ═══════════════════════════════════════════════════════════════════

    private void HookCreature(NwCreature npc)
    {
        lock (_hookLock)
        {
            if (!_hookedCreatures.Add(npc)) return; // already hooked
        }

        npc.OnConversation += OnNpcConversation;
        Log.Debug("DialogueNpcHook: hooked NPC '{Name}' (tag={Tag})", npc.Name, npc.Tag);
    }

    private void UnhookCreature(NwCreature npc)
    {
        lock (_hookLock)
        {
            if (!_hookedCreatures.Remove(npc)) return; // wasn't hooked
        }

        npc.OnConversation -= OnNpcConversation;
        Log.Debug("DialogueNpcHook: unhooked NPC '{Name}' (tag={Tag})", npc.Name, npc.Tag);
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Conversation event handler
    // ═══════════════════════════════════════════════════════════════════

    private async void OnNpcConversation(CreatureEvents.OnConversation eventData)
    {
        NwCreature npc = eventData.Creature;

        // Get the player who initiated the conversation
        NwCreature? speakerCreature = NWScript.GetLastSpeaker().ToNwObject<NwCreature>();
        NwPlayer? player = speakerCreature?.ControllingPlayer;
        if (player?.LoginCreature == null) return;

        // Check if this NPC has a World Engine dialogue tree assigned
        string? dialogueTreeId = npc.GetObjectVariable<LocalVariableString>(DialogueTreeVarName).Value;
        if (string.IsNullOrEmpty(dialogueTreeId)) return;

        // Note: We handle the conversation entirely via NUI. If the NPC has no .dlg file,
        // the default dialogue simply won't show. No SkipEvent needed on creature events.

        if (DialogueService?.Value == null)
        {
            Log.Warn("DialogueService not available for NPC conversation hook");
            return;
        }

        // Already in a conversation?
        if (DialogueService.Value.HasActiveSession(player))
        {
            player.SendServerMessage("You are already in a conversation.", ColorConstants.Orange);
            return;
        }

        // NPC already talking to someone else?
        if (DialogueService.Value.IsNpcBusy(npc))
        {
            NwPlayer? talkingTo = DialogueService.Value.GetPlayerTalkingTo(npc);
            string otherName = talkingTo?.LoginCreature?.Name ?? "someone";
            player.SendServerMessage($"{npc.Name} is already speaking with {otherName}.", ColorConstants.Orange);
            return;
        }

        // Resolve character ID
        Guid characterId = ResolveCharacterId(player);
        if (characterId == Guid.Empty)
        {
            player.SendServerMessage("Unable to identify your character.", ColorConstants.Orange);
            return;
        }

        // Capture NWN object strings before the await — after the async call completes we may
        // resume on a thread-pool thread where NWN object property access is unsafe.
        string playerName = player.PlayerName;
        string npcName = npc.Name;

        // Start the dialogue
        DialogueTreeId treeId = new(dialogueTreeId);
        bool started = await DialogueService.Value.StartDialogueAsync(player, npc, treeId, characterId);

        if (!started)
        {
            Log.Warn("Failed to start dialogue '{TreeId}' for {Player} with {Npc}",
                dialogueTreeId, playerName, npcName);
        }
    }

    private static Guid ResolveCharacterId(NwPlayer player)
    {
        try
        {
            NwItem? pcKey = player.LoginCreature?.Inventory.Items.FirstOrDefault(i => i.ResRef == "ds_pckey");
            if (pcKey == null) return Guid.Empty;

            string dbToken = pcKey.Name.Split("_")[1];
            return Guid.TryParse(dbToken, out Guid guid) ? guid : Guid.Empty;
        }
        catch
        {
            return Guid.Empty;
        }
    }
}
