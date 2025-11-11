namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;

/// <summary>
/// Value object representing the result of batch command execution.
/// Provides aggregate statistics and individual results.
/// </summary>
public sealed record BatchCommandResult
{
    public required IReadOnlyList<CommandResult> Results { get; init; }
    public required int TotalCount { get; init; }
    public required int SuccessCount { get; init; }
    public required int FailedCount { get; init; }
    public bool Cancelled { get; init; }

    /// <summary>
    /// True if all commands succeeded and none were cancelled.
    /// </summary>
    public bool AllSucceeded => SuccessCount == TotalCount && !Cancelled;

    /// <summary>
    /// True if any command failed.
    /// </summary>
    public bool AnyFailed => FailedCount > 0;

    /// <summary>
    /// Percentage of commands that succeeded (0-100).
    /// </summary>
    public double SuccessRate => TotalCount > 0 ? (SuccessCount * 100.0) / TotalCount : 0;

    /// <summary>
    /// Creates a result for successful batch execution.
    /// </summary>
    public static BatchCommandResult FromResults(IReadOnlyList<CommandResult> results, bool cancelled = false)
    {
        int successCount = results.Count(r => r.Success);
        return new BatchCommandResult
        {
            Results = results,
            TotalCount = results.Count,
            SuccessCount = successCount,
            FailedCount = results.Count - successCount,
            Cancelled = cancelled
        };
    }

    /// <summary>
    /// Creates an empty result (no commands executed).
    /// </summary>
    public static BatchCommandResult Empty => new()
    {
        Results = Array.Empty<CommandResult>(),
        TotalCount = 0,
        SuccessCount = 0,
        FailedCount = 0
    };
}

