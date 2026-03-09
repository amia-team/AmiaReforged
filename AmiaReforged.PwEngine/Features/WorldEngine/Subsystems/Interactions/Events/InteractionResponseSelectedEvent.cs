using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions.Events;

/// <summary>
/// Published when a data-driven interaction completes and a weighted response is selected.
/// Runtime subscribers listen to this event to apply the actual effects
/// (floating text, VFX, resource node spawns, directional hints, etc.).
/// </summary>
public sealed record InteractionResponseSelectedEvent(
    Guid SessionId,
    CharacterId CharacterId,
    string InteractionTag,
    string ResponseTag,
    List<InteractionResponseEffect> Effects,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}
