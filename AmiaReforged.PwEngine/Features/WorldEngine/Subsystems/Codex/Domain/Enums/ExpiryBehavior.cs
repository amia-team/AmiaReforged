namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Enums;

/// <summary>
/// Determines what happens to a dynamic quest when its time limit elapses.
/// </summary>
public enum ExpiryBehavior
{
    /// <summary>
    /// The quest transitions to <see cref="QuestState.Failed"/> with a timeout reason.
    /// Counts against the character's failure record.
    /// </summary>
    Fail = 0,

    /// <summary>
    /// The quest is silently removed from the character's codex without recording a failure.
    /// </summary>
    Remove = 1,

    /// <summary>
    /// The quest transitions to <see cref="QuestState.Expired"/> and enters a cooldown period
    /// before it can be accepted again.
    /// </summary>
    Cooldown = 2
}
