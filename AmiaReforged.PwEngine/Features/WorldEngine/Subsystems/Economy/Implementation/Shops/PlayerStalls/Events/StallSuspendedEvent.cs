using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops.PlayerStalls.Events;

/// <summary>
/// Published when stall is suspended for non-payment.
/// </summary>
public sealed record StallSuspendedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;

    public required long StallId { get; init; }
    public required string Reason { get; init; }
    public required DateTime GracePeriodEnds { get; init; }
    public required bool IsFirstSuspension { get; init; }
}

