using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Regions.Events;

/// <summary>
/// Event published when all regions are cleared (reload operation).
/// </summary>
public sealed record AllRegionsClearedEvent(
    int RegionCount,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}

