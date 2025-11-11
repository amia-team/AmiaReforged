using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops.PlayerStalls.Commands;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops.PlayerStalls.Events;

/// <summary>
/// Published when stall rent is successfully paid.
/// </summary>
public sealed record StallRentPaidEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;

    public required long StallId { get; init; }
    public required int RentAmount { get; init; }
    public required RentChargeSource Source { get; init; }
    public required DateTime NextDueDate { get; init; }
    public required DateTime PaidAt { get; init; }
}

