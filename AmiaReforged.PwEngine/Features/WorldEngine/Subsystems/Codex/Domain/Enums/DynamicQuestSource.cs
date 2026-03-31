namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Enums;

/// <summary>
/// Identifies the origin of a dynamic quest template — how it enters the world.
/// </summary>
public enum DynamicQuestSource
{
    /// <summary>
    /// Posted to an in-world bounty board placeable that adventurers interact with.
    /// </summary>
    BountyBoard = 0,

    /// <summary>
    /// Offered dynamically by an NPC quest giver from a template pool.
    /// </summary>
    NpcQuestGiver = 1,

    /// <summary>
    /// Generated automatically by the world simulation (e.g., monster surges, trade disruptions).
    /// </summary>
    WorldEvent = 2,

    /// <summary>
    /// Custom source defined by external systems or DM tools.
    /// </summary>
    Custom = 3
}
