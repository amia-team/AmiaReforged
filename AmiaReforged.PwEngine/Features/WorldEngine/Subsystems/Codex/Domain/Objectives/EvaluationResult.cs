namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Objectives;

/// <summary>
/// Result of evaluating an objective against a signal.
/// </summary>
public sealed record EvaluationResult
{
    /// <summary>Whether the objective state changed as a result of this evaluation.</summary>
    public bool StateChanged { get; init; }

    /// <summary>Whether the objective is now complete.</summary>
    public bool IsCompleted { get; init; }

    /// <summary>Whether the objective has failed.</summary>
    public bool IsFailed { get; init; }

    /// <summary>Optional message describing what happened (for journal/UI feedback).</summary>
    public string? Message { get; init; }

    /// <summary>No state change occurred — the signal was irrelevant to this objective.</summary>
    public static EvaluationResult NoOp() => new() { StateChanged = false };

    /// <summary>Progress advanced but objective is not yet complete.</summary>
    public static EvaluationResult Progressed(string? message = null) =>
        new() { StateChanged = true, Message = message };

    /// <summary>The objective is now satisfied.</summary>
    public static EvaluationResult Completed(string? message = null) =>
        new() { StateChanged = true, IsCompleted = true, Message = message };

    /// <summary>The objective has failed and cannot be completed.</summary>
    public static EvaluationResult Failed(string reason) =>
        new() { StateChanged = true, IsFailed = true, Message = reason };
}
