using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops.PlayerStalls.Events;

/// <summary>
/// Published when gold is deposited into a stall's escrow balance for rent payment.
/// </summary>
public sealed record StallEscrowDepositedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;

    public required long StallId { get; init; }
    public required int DepositAmount { get; init; }
    public required string DepositorPersonaId { get; init; }
    public required string DepositorDisplayName { get; init; }
    public required int NewEscrowBalance { get; init; }
    public required DateTime DepositedAt { get; init; }
}

