using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Organizations.Events;

/// <summary>
/// Domain event representing a member's rank/role being changed within an organization.
/// Published after member rank is successfully updated.
/// </summary>
public sealed record MemberRoleChangedEvent(
    CharacterId MemberId,
    OrganizationId OrganizationId,
    OrganizationRank NewRank,
    OrganizationRank PreviousRank,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}

