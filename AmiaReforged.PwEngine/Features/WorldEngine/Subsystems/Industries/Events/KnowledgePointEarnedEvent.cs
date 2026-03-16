using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.Events;

/// <summary>
/// Domain event published when a character earns a new economy knowledge point
/// through the progression system (progression points rolled over into a KP).
/// </summary>
public sealed record KnowledgePointEarnedEvent(
    CharacterId CharacterId,
    int NewEconomyKnowledgePointTotal,
    int TotalKnowledgePoints,
    bool IsAtSoftCap,
    bool IsAtHardCap,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}
