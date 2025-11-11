using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Events;

/// <summary>
/// Event published when a trait is unlocked for a character.
/// </summary>
public sealed record TraitUnlockedEvent(
    CharacterId CharacterId,
    TraitTag TraitTag,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}

