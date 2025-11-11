using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Organizations.Events;

/// <summary>
/// Domain event representing updates to an organization's properties.
/// Published after organization details are successfully updated.
/// </summary>
public sealed record OrganizationUpdatedEvent(
    OrganizationId OrganizationId,
    string Name,
    string Description,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}

