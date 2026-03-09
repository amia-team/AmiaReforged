using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions.Events;

/// <summary>
/// Published when a prospecting interaction finishes and new resource nodes have been spawned.
/// </summary>
public sealed record ProspectingCompletedEvent(
    Guid CharacterId,
    string AreaResRef,
    ProspectedNodeInfo[] NodesSpawned,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}

/// <summary>
/// Info about a node spawned by prospecting.
/// </summary>
public record ProspectedNodeInfo(
    Guid NodeInstanceId,
    string DefinitionTag,
    string NodeName);
