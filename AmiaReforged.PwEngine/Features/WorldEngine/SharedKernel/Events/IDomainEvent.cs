namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;

/// <summary>
/// Base interface for all domain events in WorldEngine.
/// Domain events represent something that happened in the past (use past tense names).
/// </summary>
public interface IDomainEvent
{
    /// <summary>
    /// Unique identifier for this event instance.
    /// </summary>
    Guid EventId { get; }

    /// <summary>
    /// When this event occurred (UTC).
    /// </summary>
    DateTime OccurredAt { get; }
}

