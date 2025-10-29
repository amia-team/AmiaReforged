using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Industries.Events;

/// <summary>
/// Domain event representing a character advancing to a higher proficiency level in an industry.
/// Published after the member successfully ranks up.
/// </summary>
public sealed record ProficiencyGainedEvent(
    CharacterId MemberId,
    IndustryTag IndustryTag,
    ProficiencyLevel NewLevel,
    ProficiencyLevel PreviousLevel,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}

