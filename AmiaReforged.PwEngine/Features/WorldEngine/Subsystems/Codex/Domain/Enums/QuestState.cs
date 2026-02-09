namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Enums;

/// <summary>
/// Represents the current state of a quest in the player's codex.
/// </summary>
public enum QuestState
{
    /// <summary>
    /// Quest has been discovered but not yet started.
    /// </summary>
    Discovered = 0,

    /// <summary>
    /// Quest is currently in progress.
    /// </summary>
    InProgress = 1,

    /// <summary>
    /// Quest has been successfully completed.
    /// </summary>
    Completed = 2,

    /// <summary>
    /// Quest has failed and cannot be completed.
    /// </summary>
    Failed = 3,

    /// <summary>
    /// Quest was abandoned by the player.
    /// </summary>
    Abandoned = 4
}
