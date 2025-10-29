namespace WorldSimulator.Domain.ValueObjects;

/// <summary>
/// Represents the lifecycle state of a simulation work item
/// </summary>
public enum WorkItemStatus
{
    /// <summary>Work item is queued and awaiting processing</summary>
    Pending = 0,

    /// <summary>Work item is currently being processed</summary>
    Processing = 1,

    /// <summary>Work item completed successfully</summary>
    Completed = 2,

    /// <summary>Work item failed during processing</summary>
    Failed = 3,

    /// <summary>Work item was cancelled</summary>
    Cancelled = 4
}

