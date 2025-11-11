namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;

/// <summary>
/// Value object representing options for batch command execution.
/// Encapsulates the strategy for how multiple commands should be executed.
/// </summary>
public sealed record BatchExecutionOptions
{
    /// <summary>
    /// If true, stops batch execution on the first command failure.
    /// </summary>
    public bool StopOnFirstFailure { get; init; } = true;

    /// <summary>
    /// If true, wraps all commands in a transaction (if supported).
    /// </summary>
    public bool UseTransaction { get; init; } = false;

    /// <summary>
    /// Maximum degree of parallelism. 1 = sequential execution.
    /// </summary>
    public int MaxDegreeOfParallelism { get; init; } = 1;

    /// <summary>
    /// Creates options for sequential execution that continues on failure.
    /// </summary>
    public static BatchExecutionOptions ContinueOnFailure() =>
        new() { StopOnFirstFailure = false };

    /// <summary>
    /// Creates options for transactional batch execution.
    /// </summary>
    public static BatchExecutionOptions Transactional() =>
        new() { UseTransaction = true, StopOnFirstFailure = true };

    /// <summary>
    /// Creates options for parallel execution.
    /// </summary>
    public static BatchExecutionOptions Parallel(int maxDegreeOfParallelism) =>
        new() { MaxDegreeOfParallelism = maxDegreeOfParallelism };

    /// <summary>
    /// Default options: sequential, stop on first failure.
    /// </summary>
    public static BatchExecutionOptions Default => new();
}

