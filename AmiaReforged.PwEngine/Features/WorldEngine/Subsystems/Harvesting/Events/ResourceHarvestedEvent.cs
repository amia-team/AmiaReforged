using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting.Events;

/// <summary>
/// Published when a character successfully harvests resources from a node
/// </summary>
public sealed record ResourceHarvestedEvent(
    Guid HarvesterId,
    Guid NodeInstanceId,
    string ResourceTag,
    HarvestedItem[] Items,
    int RemainingUses,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}

public record HarvestedItem(
    string ItemTag,
    int Quantity,
    IPQuality Quality);

