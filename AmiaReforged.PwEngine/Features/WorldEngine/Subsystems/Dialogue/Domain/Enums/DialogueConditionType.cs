namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.Enums;

/// <summary>
/// Types of preconditions that control whether a dialogue choice is visible to the player.
/// </summary>
public enum DialogueConditionType
{
    /// <summary>
    /// Player must have a specific item (by tag) in their inventory.
    /// Parameters: itemTag, count (optional, default 1).
    /// </summary>
    HasItem,

    /// <summary>
    /// A quest must be in a specific state for this choice to appear.
    /// Parameters: questId, requiredState (Discovered/InProgress/Completed/Failed).
    /// </summary>
    QuestState,

    /// <summary>
    /// Player's reputation with a faction must be above a threshold.
    /// Parameters: factionId, minScore.
    /// </summary>
    ReputationAbove,

    /// <summary>
    /// Player's reputation with a faction must be below a threshold.
    /// Parameters: factionId, maxScore.
    /// </summary>
    ReputationBelow,

    /// <summary>
    /// Player must have unlocked a specific knowledge/lore entry.
    /// Parameters: loreId.
    /// </summary>
    HasKnowledge,

    /// <summary>
    /// A local variable on the NPC or player must match a value.
    /// Parameters: variableName, expectedValue.
    /// </summary>
    LocalVariable,

    /// <summary>
    /// Custom condition evaluated by a named handler.
    /// Parameters: handlerName, plus any additional key-value pairs.
    /// </summary>
    Custom
}
