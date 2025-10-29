using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Regions.Events;

/// <summary>
/// Event published when a region's definition is updated.
/// </summary>
public sealed record RegionUpdatedEvent(
    RegionTag Tag,
    string Name,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}

