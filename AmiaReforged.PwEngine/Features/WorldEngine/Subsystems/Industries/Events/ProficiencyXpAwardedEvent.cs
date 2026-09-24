using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.Events;

/// <summary>
/// Domain event published when a character earns proficiency XP in an industry.
/// Published by <c>AwardProficiencyCommand</c> after the calculator applies the XP,
/// auto-levels within tier boundaries, and enforces the tier-ceiling hard gate.
/// </summary>
public sealed record ProficiencyXpAwardedEvent(
    CharacterId MemberId,
    IndustryTag IndustryTag,
    int NewLevel,
    int XpRemaining,
    int XpRequired,
    int LevelsGained,
    bool IsAtTierCeiling,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}
