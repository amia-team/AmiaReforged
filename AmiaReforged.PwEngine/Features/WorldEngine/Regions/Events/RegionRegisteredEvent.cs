using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Regions.Events;

/// <summary>
/// Event published when a new region is registered.
/// </summary>
public sealed record RegionRegisteredEvent(
    RegionTag Tag,
    string Name,
    int AreaCount,
    int SettlementCount,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}
