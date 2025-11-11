using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting.Events;

/// <summary>
/// Published when all nodes in an area are cleared (typically on area cleanup)
/// </summary>
public sealed record NodesClearedEvent(
    string AreaResRef,
    int NodesCleared,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}


