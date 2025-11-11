using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Organizations.Events;

/// <summary>
/// Domain event representing an organization being created/registered in the world.
/// Published after organization is successfully added to the repository.
/// </summary>
public sealed record OrganizationCreatedEvent(
    OrganizationId OrganizationId,
    string Name,
    OrganizationType Type,
    OrganizationId? ParentOrganizationId,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}

