using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Organizations.Events;

/// <summary>
/// Domain event representing a character leaving an organization.
/// Published after membership is successfully removed from the repository.
/// </summary>
public sealed record MemberLeftOrganizationEvent(
    CharacterId MemberId,
    OrganizationId OrganizationId,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}

