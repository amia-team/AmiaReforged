using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops.PlayerStalls.Events;

/// <summary>
/// Published when stall ownership is released due to non-payment.
/// </summary>
public sealed record StallOwnershipReleasedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;

    public required long StallId { get; init; }
    public required Guid? FormerOwnerId { get; init; }
    public required string? FormerPersonaId { get; init; }
    public required string Reason { get; init; }
}

