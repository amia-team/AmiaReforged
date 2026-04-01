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
    Abandoned = 4,

    /// <summary>
    /// Quest expired because its time limit elapsed.
    /// Distinct from <see cref="Failed"/> to allow separate handling of time-based termination.
    /// </summary>
    Expired = 5
}

/// <summary>
/// Extension methods for <see cref="QuestState"/>.
/// </summary>
public static class QuestStateExtensions
{
    public static string DisplayName(this QuestState state) => state switch
    {
        QuestState.Discovered => "Discovered",
        QuestState.InProgress => "In Progress",
        QuestState.Completed => "Completed",
        QuestState.Failed => "Failed",
        QuestState.Abandoned => "Abandoned",
        QuestState.Expired => "Expired",
        _ => state.ToString()
    };
}
