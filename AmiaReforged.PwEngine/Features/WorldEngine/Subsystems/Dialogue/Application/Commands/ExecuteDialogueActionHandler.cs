using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.Enums;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
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
public sealed class ExecuteDialogueActionHandler : ICommandHandler<ExecuteDialogueActionCommand>
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    [Inject] private Lazy<IWorldEngineFacade>? WorldEngine { get; init; }

    public async Task<CommandResult> HandleAsync(ExecuteDialogueActionCommand command,
        CancellationToken cancellationToken = default)
    {
        DialogueAction action = command.Action;
        NwPlayer player = command.Player;
        NwCreature? creature = player.LoginCreature;

        if (creature == null)
            return CommandResult.Fail("Player has no login creature");

        try
        {
            return action.ActionType switch
            {
                DialogueActionType.StartQuest => await HandleStartQuest(action, command.CharacterId),
                DialogueActionType.CompleteQuest => await HandleCompleteQuest(action, command.CharacterId),
                DialogueActionType.GiveItem => HandleGiveItem(action, creature),
                DialogueActionType.TakeItem => HandleTakeItem(action, creature),
                DialogueActionType.GiveGold => HandleGiveGold(action, creature),
                DialogueActionType.TakeGold => HandleTakeGold(action, creature),
                DialogueActionType.GrantKnowledge => await HandleGrantKnowledge(action, command.CharacterId),
                DialogueActionType.SetLocalVariable => HandleSetLocalVariable(action, creature, command.Npc),
                DialogueActionType.ChangeReputation => HandleChangeReputation(action),
                DialogueActionType.OpenShop => HandleOpenShop(action, creature, command.Npc),
                DialogueActionType.Custom => HandleCustom(action),
                _ => CommandResult.Fail($"Unknown dialogue action type: {action.ActionType}")
            };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error executing dialogue action {ActionType}", action.ActionType);
            return CommandResult.Fail($"Action failed: {ex.Message}");
        }
    }

    private async Task<CommandResult> HandleStartQuest(DialogueAction action, Guid characterId)
    {
        string questId = action.GetRequiredParam("questId");
        Log.Info("Dialogue action: Starting quest '{QuestId}' for character {CharacterId}", questId, characterId);

        // TODO: Dispatch to Codex subsystem when quest start command is implemented
        // For now, log the intent
        return CommandResult.OkWith("questId", questId);
    }

    private async Task<CommandResult> HandleCompleteQuest(DialogueAction action, Guid characterId)
    {
        string questId = action.GetRequiredParam("questId");
        Log.Info("Dialogue action: Completing quest '{QuestId}' for character {CharacterId}", questId, characterId);

        // TODO: Dispatch to Codex subsystem when quest completion command is implemented
        return CommandResult.OkWith("questId", questId);
    }

    private CommandResult HandleGiveItem(DialogueAction action, NwCreature creature)
    {
        string itemTag = action.GetRequiredParam("itemTag");
        int quantity = int.TryParse(action.GetParam("quantity") ?? "1", out int q) ? q : 1;

        for (int i = 0; i < quantity; i++)
        {
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

        creature.Gold += (uint)amount;
        Log.Info("Dialogue action: Gave {Amount} gold to {Player}", amount, creature.Name);
        return CommandResult.Ok();
    }

    private CommandResult HandleTakeGold(DialogueAction action, NwCreature creature)
    {
        int amount = int.TryParse(action.GetRequiredParam("amount"), out int a) ? a : 0;
        if (amount <= 0) return CommandResult.Fail("Gold amount must be positive");

        if (creature.Gold < amount)
            return CommandResult.Fail($"Player only has {creature.Gold} gold, needs {amount}");

        creature.Gold -= (uint)amount;
        Log.Info("Dialogue action: Took {Amount} gold from {Player}", amount, creature.Name);
        return CommandResult.Ok();
    }

    private async Task<CommandResult> HandleGrantKnowledge(DialogueAction action, Guid characterId)
    {
        string loreId = action.GetRequiredParam("loreId");

        if (WorldEngine?.Value?.Codex == null)
            return CommandResult.Fail("Codex subsystem not available");

        CommandResult result = await WorldEngine.Value.Codex.GrantKnowledgeAsync(
            SharedKernel.CharacterId.From(characterId), loreId);

        Log.Info("Dialogue action: Granted knowledge '{LoreId}' to character {CharacterId}", loreId, characterId);
        return result;
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

    private CommandResult HandleChangeReputation(DialogueAction action)
    {
        string factionId = action.GetRequiredParam("factionId");
        int amount = int.TryParse(action.GetRequiredParam("amount"), out int a) ? a : 0;

        Log.Info("Dialogue action: Change reputation with '{FactionId}' by {Amount}", factionId, amount);
        // TODO: Dispatch to reputation system when implemented
        return CommandResult.OkWith("factionId", factionId);
    }

    private CommandResult HandleOpenShop(DialogueAction action, NwCreature creature, NwCreature npc)
    {
        string storeResRef = action.GetRequiredParam("storeResRef");
        string storeTag = action.GetParam("storeTag") ?? storeResRef;
        int markUp = int.TryParse(action.GetParam("bonusMarkUp") ?? "0", out int mu) ? mu : 0;
        int markDown = int.TryParse(action.GetParam("bonusMarkDown") ?? "0", out int md) ? md : 0;

        // Reuse an already-spawned store near the NPC if one exists with the matching tag.
        NwStore? store = npc.GetNearestObjectsByType<NwStore>()
            .FirstOrDefault(s => string.Equals(s.Tag, storeTag, StringComparison.OrdinalIgnoreCase));

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

        NWScript.OpenStore(store, creature, markUp, markDown);
        Log.Info("Dialogue action: Opened store '{StoreTag}' for {Player}", storeTag, creature.Name);
        return CommandResult.Ok();
    }

    private CommandResult HandleCustom(DialogueAction action)
    {
        string commandType = action.GetRequiredParam("commandType");
        Log.Info("Dialogue action: Custom command '{CommandType}'", commandType);
        // TODO: Route to named command handler via CommandDispatcher
        return CommandResult.OkWith("commandType", commandType);
    }
}
