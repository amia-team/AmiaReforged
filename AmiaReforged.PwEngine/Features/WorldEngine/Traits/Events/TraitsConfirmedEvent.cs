using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Traits.Events;

/// <summary>
/// Event published when a character confirms their trait selections.
/// </summary>
public sealed record TraitsConfirmedEvent(
    CharacterId CharacterId,
    int TraitCount,
    int FinalBudgetAvailable,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}

