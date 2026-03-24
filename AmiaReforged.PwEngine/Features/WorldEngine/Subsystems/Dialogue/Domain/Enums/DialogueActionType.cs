namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.Enums;

/// <summary>
/// Types of actions that can be fired when a dialogue node is entered or a choice is selected.
/// Each action dispatches a World Engine command.
/// </summary>
public enum DialogueActionType
{
    /// <summary>
    /// Start a quest for the player. Parameters: questId.
    /// </summary>
    StartQuest,

    /// <summary>
    /// Mark a quest as completed. Parameters: questId.
    /// </summary>
    CompleteQuest,

    /// <summary>
    /// Give an item to the player. Parameters: itemTag, quantity (optional, default 1).
    /// </summary>
    GiveItem,

    /// <summary>
    /// Take an item from the player. Parameters: itemTag, quantity (optional, default 1).
    /// </summary>
    TakeItem,

    /// <summary>
    /// Adjust the player's reputation with a faction. Parameters: factionId, amount (positive or negative).
    /// </summary>
    ChangeReputation,

    /// <summary>
    /// Grant a knowledge/lore entry to the player. Parameters: loreId.
    /// </summary>
    GrantKnowledge,

    /// <summary>
    /// Set a local variable on the NPC or player. Parameters: target (npc/player), variableName, value.
    /// </summary>
    SetLocalVariable,

    /// <summary>
    /// Give gold to the player. Parameters: amount.
    /// </summary>
    GiveGold,

    /// <summary>
    /// Take gold from the player. Parameters: amount.
    /// </summary>
    TakeGold,

    /// <summary>
    /// Open a store for the player. Creates the store from a resref at the NPC's location,
    /// or reuses an already-spawned store nearby.
    /// Parameters: storeResRef (required), storeTag (optional — defaults to storeResRef),
    /// bonusMarkUp (optional, default 0), bonusMarkDown (optional, default 0).
    /// </summary>
    OpenShop,

    /// <summary>
    /// Custom action dispatched to a named command handler. Parameters: commandType, plus additional key-value pairs.
    /// </summary>
    Custom
}
