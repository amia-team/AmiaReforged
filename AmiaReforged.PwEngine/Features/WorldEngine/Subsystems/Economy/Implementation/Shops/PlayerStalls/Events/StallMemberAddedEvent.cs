using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops.PlayerStalls.Events;

/// <summary>
/// Published when a new member is added to a player stall.
/// </summary>
public sealed record StallMemberAddedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;

    public required long StallId { get; init; }
    public required string MemberPersonaId { get; init; }
    public required string MemberDisplayName { get; init; }
    public required string AddedByPersonaId { get; init; }
    public required bool CanManageInventory { get; init; }
    public required bool CanConfigureSettings { get; init; }
    public required bool CanCollectEarnings { get; init; }
}
