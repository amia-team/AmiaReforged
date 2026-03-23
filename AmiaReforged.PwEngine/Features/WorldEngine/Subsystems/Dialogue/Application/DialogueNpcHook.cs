using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.ValueObjects;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Application;

/// <summary>
/// Hooks into NPC conversation events to detect dialogue-enabled NPCs
/// and launch the custom NUI conversation window instead of the default NWN dialogue.
/// Finds all NPCs with the "we_dialogue_tree" local variable and subscribes to their OnConversation event.
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

    [Inject] private Lazy<DialogueService>? DialogueService { get; init; }

    public DialogueNpcHook()
    {
        // Find all creatures in the module that have the dialogue tree variable set
        List<NwCreature> dialogueNpcs = NwObject.FindObjectsOfType<NwCreature>()
            .Where(c => !string.IsNullOrEmpty(
                c.GetObjectVariable<LocalVariableString>(DialogueTreeVarName).Value))
            .ToList();

        foreach (NwCreature npc in dialogueNpcs)
        {
            npc.OnConversation += OnNpcConversation;
            Log.Info("DialogueNpcHook: registered conversation hook for NPC '{Name}' (tag={Tag})",
                npc.Name, npc.Tag);
        }

        Log.Info("DialogueNpcHook registered — {Count} dialogue-enabled NPCs found", dialogueNpcs.Count);
    }

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

        // Resolve character ID
        Guid characterId = ResolveCharacterId(player);
        if (characterId == Guid.Empty)
        {
            player.SendServerMessage("Unable to identify your character.", ColorConstants.Orange);
            return;
        }

        // Start the dialogue
        DialogueTreeId treeId = new(dialogueTreeId);
        bool started = await DialogueService.Value.StartDialogueAsync(player, npc, treeId, characterId);

        if (!started)
        {
            Log.Warn("Failed to start dialogue '{TreeId}' for {Player} with {Npc}",
                dialogueTreeId, player.PlayerName, npc.Name);
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
