using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions.Events;

/// <summary>
/// Published when a character begins a new interaction session.
/// </summary>
public sealed record InteractionStartedEvent(
    Guid SessionId,
    Guid CharacterId,
    string InteractionTag,
    Guid TargetId,
    int RequiredRounds,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}
