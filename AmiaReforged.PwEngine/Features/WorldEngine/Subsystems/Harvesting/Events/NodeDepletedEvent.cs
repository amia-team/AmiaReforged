using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting.Events;

/// <summary>
/// Published when a resource node's uses are exhausted
/// </summary>
public sealed record NodeDepletedEvent(
    Guid NodeInstanceId,
    string AreaResRef,
    string ResourceTag,
    Guid LastHarvesterId,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}

