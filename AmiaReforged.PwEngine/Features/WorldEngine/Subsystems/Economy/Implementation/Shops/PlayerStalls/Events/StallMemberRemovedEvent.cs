using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops.PlayerStalls.Events;

/// <summary>
/// Published when a member is removed from a player stall.
/// </summary>
public sealed record StallMemberRemovedEvent : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt { get; } = DateTime.UtcNow;

    public required long StallId { get; init; }
    public required string MemberPersonaId { get; init; }
    public required string RemovedByPersonaId { get; init; }
}
