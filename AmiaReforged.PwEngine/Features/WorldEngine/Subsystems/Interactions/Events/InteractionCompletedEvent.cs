using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions.Events;

/// <summary>
/// Published when an interaction session finishes (successfully or not).
/// </summary>
public sealed record InteractionCompletedEvent(
    Guid SessionId,
    Guid CharacterId,
    string InteractionTag,
    Guid TargetId,
    bool Success,
    string? Message,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}
