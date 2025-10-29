using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;

namespace AmiaReforged.PwEngine.Features.WorldEngine.ResourceNodes.Events;

/// <summary>
/// Event published when resource nodes are successfully provisioned for an area.
/// </summary>
public sealed record AreaNodesProvisionedEvent(
    string AreaResRef,
    string AreaName,
    int NodeCount,
    DateTime ProvisionedAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredAt => ProvisionedAt;
}

