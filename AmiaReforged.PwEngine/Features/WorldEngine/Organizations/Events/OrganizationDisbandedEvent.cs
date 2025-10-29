using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Organizations.Events;

/// <summary>
/// Domain event representing an organization being disbanded/dissolved.
/// Published when an organization is removed from the system.
/// </summary>
public sealed record OrganizationDisbandedEvent(
    OrganizationId OrganizationId,
    string Name,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}

