using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Events;

/// <summary>
/// Event published when a character deselects a trait.
/// </summary>
public sealed record TraitDeselectedEvent(
    CharacterId CharacterId,
    TraitTag TraitTag,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}

