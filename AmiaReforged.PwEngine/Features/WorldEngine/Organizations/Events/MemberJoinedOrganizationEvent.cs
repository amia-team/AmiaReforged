using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Organizations.Events;

/// <summary>
/// Domain event representing a character joining an organization.
/// Published after membership is successfully added to the repository.
/// </summary>
public sealed record MemberJoinedOrganizationEvent(
    CharacterId MemberId,
    OrganizationId OrganizationId,
    OrganizationRank InitialRank,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}

