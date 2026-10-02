using System.Collections.Concurrent;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.Enums;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using Anvil.API;
using Anvil.Services;
using NLog;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Application.Commands;

/// <summary>
/// Handles <see cref="ExecuteDialogueActionCommand"/> by dispatching to the appropriate
/// World Engine subsystem based on the action type.
/// </summary>
[ServiceBinding(typeof(ICommandHandlerMarker))]
[ServiceBinding(typeof(IEventHandlerMarker))]
[ServiceBinding(typeof(ExecuteDialogueActionHandler))]
public sealed class ExecuteDialogueActionHandler
    : ICommandHandler<ExecuteDialogueActionCommand>,
        IEventHandler<CommandExecutedEvent<UpdateDialogueTreeCommand>>,
        IEventHandler<CommandExecutedEvent<DeleteDialogueTreeCommand>>,
        IEventHandlerMarker
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Cached store references keyed by NPC ObjectId.
    /// Stores the resref, tag, and NwStore reference so repeated opens skip the
    /// <c>GetNearestObjectsByType</c> scan and avoid re-creating the store object.
    /// Cleared when a dialogue tree is updated or deleted via the admin panel. The clear is
    /// triggered asynchronously by the <see cref="CommandExecutedEvent{UpdateDialogueTreeCommand}"/>
    /// and <see cref="CommandExecutedEvent{DeleteDialogueTreeCommand}"> domain events that the
    /// command dispatcher publishes for successful mutations (see the two
    /// <c>HandleAsync</c> event callbacks below). A successful HTTP response therefore does not
    /// guarantee the cache has been cleared yet — it is cleared once the event subscriber
    /// completes on the background event-bus thread.
    /// </summary>
    private readonly ConcurrentDictionary<uint, CachedStore> _storeCache = new();

    private sealed record CachedStore(string ResRef, string Tag, NwStore Store);

    [Inject] private Lazy<IWorldEngineFacade>? WorldEngine { get; init; }

    public async Task<CommandResult> HandleAsync(ExecuteDialogueActionCommand command,
        CancellationToken cancellationToken = default)
    {
        DialogueAction action = command.Action;
        NwPlayer player = command.Player;
        if (action.ActionType is DialogueActionType.StartQuest or DialogueActionType.CompleteQuest or DialogueActionType.ChangeReputation or DialogueActionType.SetQuestStage or DialogueActionType.GrantKnowledge)
        {
            if (WorldEngine?.Value is null) return CommandResult.Fail("World Engine is unavailable");
            try { return await DialogueCodexActions.ExecuteAsync(WorldEngine.Value, action, SharedKernel.CharacterId.From(command.CharacterId)); }
            catch (Exception ex) { Log.Error(ex, "Dialogue Codex action failed"); return CommandResult.Fail(ex.Message); }
        }
        if (action.ActionType == DialogueActionType.Custom) return CommandResult.Fail("Custom dialogue actions are not supported");

        await NwTask.SwitchToMainThread();
        NwCreature? creature = player.LoginCreature;

        if (creature == null)
            return CommandResult.Fail("Player has no login creature");

        try
        {
            return action.ActionType switch
            {
                DialogueActionType.GiveItem => HandleGiveItem(action, creature),
                DialogueActionType.TakeItem => HandleTakeItem(action, creature),
                DialogueActionType.GiveGold => HandleGiveGold(action, creature),
                DialogueActionType.TakeGold => HandleTakeGold(action, creature),
                DialogueActionType.SetLocalVariable => HandleSetLocalVariable(action, creature, command.Npc),
                DialogueActionType.OpenShop => HandleOpenShop(action, creature, command.Npc),
                _ => CommandResult.Fail($"Unknown dialogue action type: {action.ActionType}")
            };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error executing dialogue action {ActionType}", action.ActionType);
            return CommandResult.Fail($"Action failed: {ex.Message}");
        }
    }

    private CommandResult HandleGiveItem(DialogueAction action, NwCreature creature)
    {
        string itemTag = action.GetRequiredParam("itemTag");
        int quantity = int.TryParse(action.GetParam("quantity") ?? "1", out int q) ? q : 1;

        for (int i = 0; i < quantity; i++)
        {
            if (creature.Location == null) return CommandResult.Fail("Player has no valid location to receive items");
            NwItem? item = NwItem.Create(itemTag, creature.Location);
            if (item != null)
            {
                creature.AcquireItem(item);
            }
            else
            {
                Log.Warn("Failed to create item '{ItemTag}' for dialogue action", itemTag);
                return CommandResult.Fail($"Failed to create item '{itemTag}'");
            }
        }

        Log.Info("Dialogue action: Gave {Quantity}x '{ItemTag}' to {Player}", quantity, itemTag, creature.Name);
        return CommandResult.Ok();
    }

    private CommandResult HandleTakeItem(DialogueAction action, NwCreature creature)
    {
        string itemTag = action.GetRequiredParam("itemTag");
        int quantity = int.TryParse(action.GetParam("quantity") ?? "1", out int q) ? q : 1;

        if (creature.Inventory.Items.Count(i => i.Tag == itemTag) < quantity)
            return CommandResult.Fail($"Player needs {quantity} of '{itemTag}'");
        int removed = 0;
        foreach (NwItem item in creature.Inventory.Items.Where(i => i.Tag == itemTag).Take(quantity).ToList())
        {
            item.Destroy();
            removed++;
        }

        if (removed < quantity)
        {
            Log.Warn("Could only remove {Removed}/{Required} of '{ItemTag}'", removed, quantity, itemTag);
        }

        return CommandResult.OkWith("removed", removed);
    }

    private CommandResult HandleGiveGold(DialogueAction action, NwCreature creature)
    {
        int amount = int.TryParse(action.GetRequiredParam("amount"), out int a) ? a : 0;
        if (amount <= 0) return CommandResult.Fail("Gold amount must be positive");

        creature.GiveGold(amount);
        Log.Info("Dialogue action: Gave {Amount} gold to {Player}", amount, creature.Name);
        return CommandResult.Ok();
    }

    private CommandResult HandleTakeGold(DialogueAction action, NwCreature creature)
    {
        int amount = int.TryParse(action.GetRequiredParam("amount"), out int a) ? a : 0;
        if (amount <= 0) return CommandResult.Fail("Gold amount must be positive");

        if (creature.Gold < (uint)amount)
            return CommandResult.Fail($"Player only has {creature.Gold} gold, needs {amount}");

        creature.TakeGold(amount);
        Log.Info("Dialogue action: Took {Amount} gold from {Player}", amount, creature.Name);
        return CommandResult.Ok();
    }

    private CommandResult HandleSetLocalVariable(DialogueAction action, NwCreature creature, NwCreature npc)
    {
        string variableName = action.GetRequiredParam("variableName");
        string value = action.GetRequiredParam("value");
        string target = action.GetParam("target") ?? "npc";

        NwCreature targetCreature = target.Equals("player", StringComparison.OrdinalIgnoreCase) ? creature : npc;

        // Try setting as int first, fall back to string
        if (int.TryParse(value, out int intValue))
        {
            targetCreature.GetObjectVariable<LocalVariableInt>(variableName).Value = intValue;
        }
        else
        {
            targetCreature.GetObjectVariable<LocalVariableString>(variableName).Value = value;
        }

        Log.Info("Dialogue action: Set {Target}.{Variable} = {Value}", target, variableName, value);
        return CommandResult.Ok();
    }

    private CommandResult HandleOpenShop(DialogueAction action, NwCreature creature, NwCreature npc)
    {
        string storeResRef = action.GetRequiredParam("storeResRef");
        string storeTag = action.GetParam("storeTag") ?? storeResRef;
        int markUp = int.TryParse(action.GetParam("bonusMarkUp") ?? "0", out int mu) ? mu : 0;
        int markDown = int.TryParse(action.GetParam("bonusMarkDown") ?? "0", out int md) ? md : 0;

        // Check the cache first — if we have a valid, matching store for this NPC, reuse it.
        if (_storeCache.TryGetValue(npc.ObjectId, out CachedStore? cached)
            && cached.ResRef == storeResRef
            && string.Equals(cached.Tag, storeTag, StringComparison.OrdinalIgnoreCase)
            && cached.Store.IsValid)
        {
            NWScript.OpenStore(cached.Store, creature, markUp, markDown);
            Log.Info("Dialogue action: Opened cached store '{StoreTag}' for {Player}", storeTag, creature.Name);
            return CommandResult.Ok();
        }

        // Cache miss or stale — look for an already-spawned store near the NPC.
        NwStore? store = npc.GetNearestObjectsByType<NwStore>()
            .FirstOrDefault(s => s.ResRef == storeResRef && string.Equals(s.Tag, storeTag, StringComparison.OrdinalIgnoreCase));

        if (store == null)
        {
            if (npc.Location == null)
                return CommandResult.Fail("NPC has no valid location to spawn store");

            store = NwStore.Create(storeResRef, npc.Location);
            if (store == null)
                return CommandResult.Fail($"Failed to create store from resref '{storeResRef}'");

            store.Tag = storeTag;
            Log.Info("Dialogue action: Created store '{StoreTag}' from resref '{ResRef}' at {Npc}",
                storeTag, storeResRef, npc.Name);
        }

        // Cache the store for future opens.
        _storeCache[npc.ObjectId] = new CachedStore(storeResRef, storeTag, store);

        NWScript.OpenStore(store, creature, markUp, markDown);
        Log.Info("Dialogue action: Opened store '{StoreTag}' for {Player}", storeTag, creature.Name);
        return CommandResult.Ok();
    }

    /// <summary>
    /// Clears the cached store references so that changed store resrefs/tags are picked up on
    /// next open. Called from the successful update/delete event callbacks only; the cached
    /// dictionary references are dropped, not the underlying NwStore objects.
    /// </summary>
    public void InvalidateStoreCache()
    {
        int count = _storeCache.Count;
        _storeCache.Clear();
        if (count > 0)
        {
            Log.Info("Dialogue store cache invalidated ({Count} entries cleared)", count);
        }
    }

    /// <summary>
    /// Reaction to a successfully updated dialogue tree. The dispatcher only publishes this
    /// event for a successful result, after the handler returns from persistence, so clearing
    /// here reflects the committed change. Runs on the background event-bus thread.
    /// </summary>
    public Task HandleAsync(
        CommandExecutedEvent<UpdateDialogueTreeCommand> @event,
        CancellationToken cancellationToken = default)
    {
        if (@event.Result.Success)
        {
            InvalidateStoreCache();
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Reaction to a successfully deleted dialogue tree. The dispatcher only publishes this
    /// event for a successful result, after the handler returns from persistence, so clearing
    /// here reflects the committed change. Runs on the background event-bus thread.
    /// </summary>
    public Task HandleAsync(
        CommandExecutedEvent<DeleteDialogueTreeCommand> @event,
        CancellationToken cancellationToken = default)
    {
        if (@event.Result.Success)
        {
            InvalidateStoreCache();
        }

        return Task.CompletedTask;
    }

}
