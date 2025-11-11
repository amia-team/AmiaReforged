using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Events;

/// <summary>
/// Event published when a trait's active state changes.
/// </summary>
public sealed record TraitActiveStateChangedEvent(
    CharacterId CharacterId,
    TraitTag TraitTag,
    bool IsActive,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}


/// <summary>
/// Event published when a character selects a trait.
/// </summary>
public sealed record TraitSelectedEvent(
    CharacterId CharacterId,
    TraitTag TraitTag,
    bool IsConfirmed,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}

