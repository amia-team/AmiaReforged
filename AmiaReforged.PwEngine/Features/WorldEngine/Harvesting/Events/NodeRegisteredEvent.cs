using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Harvesting.Events;

/// <summary>
/// Published when a resource node is registered and available for harvesting
/// </summary>
public sealed record NodeRegisteredEvent(
    Guid NodeInstanceId,
    string AreaResRef,
    string ResourceTag,
    IPQuality Quality,
    int InitialUses,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}

