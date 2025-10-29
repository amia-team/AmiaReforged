using System.Text.Json;
using WorldSimulator.Domain.WorkPayloads;
using WorldSimulator.Domain.ValueObjects;

namespace WorldSimulator.Domain.Aggregates;

/// <summary>
/// Represents a unit of work to be processed by the simulation service.
/// This is the aggregate root for simulation work orchestration.
/// REFACTORED: Now supports strongly-typed SimulationWorkType instead of string-based types.
/// </summary>
public class SimulationWorkItem
{
    /// <summary>Unique identifier for this work item</summary>
    public Guid Id { get; private set; }

    /// <summary>Serialized work type for EF Core persistence</summary>
    private string _serializedWorkType = null!;

    /// <summary>Strongly-typed work type (Parse, Don't Validate!)</summary>
    public SimulationWorkType WorkType { get; private set; } = null!;

    /// <summary>DEPRECATED: String-based work type for backward compatibility</summary>
    [Obsolete("Use strongly-typed WorkType property instead")]
    public string LegacyWorkType => _serializedWorkType;

    /// <summary>DEPRECATED: JSON payload for backward compatibility</summary>
    [Obsolete("Use strongly-typed WorkType property instead")]
    public string Payload => _serializedWorkType;

    /// <summary>Current status of the work item</summary>
    public WorkItemStatus Status { get; private set; }

    /// <summary>When this work item was created</summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>When processing started (null if not yet started)</summary>
    public DateTime? StartedAt { get; private set; }

    /// <summary>When processing completed (null if not yet completed)</summary>
    public DateTime? CompletedAt { get; private set; }

    /// <summary>Error message if processing failed</summary>
    public string? Error { get; private set; }

    /// <summary>Number of retry attempts made</summary>
    public int RetryCount { get; private set; }

    /// <summary>Concurrency token for optimistic locking</summary>
    public uint Version { get; private set; }

    private SimulationWorkItem() { } // EF Core

    /// <summary>
    /// DEPRECATED: String-based constructor for backward compatibility
    /// </summary>
    [Obsolete("Use SimulationWorkItem.Create(SimulationWorkType) instead")]
    public SimulationWorkItem(string workType, string payload)
    {
        Id = Guid.NewGuid();
        _serializedWorkType = payload ?? throw new ArgumentNullException(nameof(payload));
        Status = WorkItemStatus.Pending;
        CreatedAt = DateTime.UtcNow;
        RetryCount = 0;
        Version = 0;

        // Try to deserialize to typed WorkType
        try
        {
            WorkType = JsonSerializer.Deserialize<SimulationWorkType>(_serializedWorkType)!;
        }
        catch
        {
            // Fallback for legacy data - will be removed in future
            throw new ArgumentException($"Cannot deserialize legacy work type: {workType}");
        }
    }

    /// <summary>
    /// Creates a new work item with strongly-typed payload.
    /// Parse, Don't Validate! The WorkType is already validated at construction.
    /// </summary>
    public static SimulationWorkItem Create(SimulationWorkType workType)
    {
        return new SimulationWorkItem
        {
            Id = Guid.NewGuid(),
            WorkType = workType ?? throw new ArgumentNullException(nameof(workType)),
            _serializedWorkType = JsonSerializer.Serialize(workType),
            Status = WorkItemStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            RetryCount = 0,
            Version = 0
        };
    }
    /// <summary>
    /// Marks the work item as processing
    /// </summary>
    public void Start()
    {
        if (Status != WorkItemStatus.Pending && Status != WorkItemStatus.Failed)
        {
            throw new InvalidOperationException($"Cannot start work item in status {Status}");
        }
        Status = WorkItemStatus.Processing;
        StartedAt = DateTime.UtcNow;
        Version++;
    }
    /// <summary>
    /// Marks the work item as completed
    /// </summary>
    public void Complete()
    {
        if (Status != WorkItemStatus.Processing)
        {
            throw new InvalidOperationException($"Cannot complete work item in status {Status}");
        }
        Status = WorkItemStatus.Completed;
        CompletedAt = DateTime.UtcNow;
        Version++;
    }
    /// <summary>
    /// Marks the work item as failed with an error message
    /// </summary>
    public void Fail(string errorMessage)
    {
        if (Status != WorkItemStatus.Processing)
        {
            throw new InvalidOperationException($"Cannot fail work item in status {Status}");
        }
        Status = WorkItemStatus.Failed;
        Error = errorMessage;
        CompletedAt = DateTime.UtcNow;
        RetryCount++;
        Version++;
    }
    /// <summary>
    /// Cancels the work item
    /// </summary>
    public void Cancel()
    {
        if (Status == WorkItemStatus.Completed)
        {
            throw new InvalidOperationException("Cannot cancel completed work item");
        }
        Status = WorkItemStatus.Cancelled;
        CompletedAt = DateTime.UtcNow;
        Version++;
    }
    /// <summary>
    /// Determines if the work item can be retried
    /// </summary>
    public bool CanRetry(int maxRetries = 3)
    {
        return Status == WorkItemStatus.Failed && RetryCount < maxRetries;
    }
    /// <summary>
    /// Factory method to create a work item from a typed payload
    /// </summary>
    public static SimulationWorkItem Create<TPayload>(TPayload payload) where TPayload : IWorkPayload
    {
        var validation = payload.Validate();
        if (!validation.IsValid)
        {
            throw new ArgumentException(
                $"Invalid payload: {string.Join(", ", validation.Errors)}",
                nameof(payload));
        }
        var workType = typeof(TPayload).Name.Replace("Payload", "");
        var json = JsonSerializer.Serialize(payload);
        return new SimulationWorkItem(workType, json);
    }
    /// <summary>
    /// Deserializes the payload to the specified type
    /// </summary>
    public TPayload GetPayload<TPayload>() where TPayload : IWorkPayload
    {
        var payload = JsonSerializer.Deserialize<TPayload>(Payload);
        if (payload == null)
        {
            throw new InvalidOperationException($"Failed to deserialize payload to {typeof(TPayload).Name}");
        }
        return payload;
    }
    /// <summary>
    /// Attempts to deserialize the payload to the specified type
    /// </summary>
    public bool TryGetPayload<TPayload>(out TPayload? payload) where TPayload : IWorkPayload
    {
        try
        {
            payload = JsonSerializer.Deserialize<TPayload>(Payload);
            return payload != null;
        }
        catch
        {
            payload = default;
            return false;
        }
    }
}
