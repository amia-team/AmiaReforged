using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.Events;

/// <summary>
/// Domain event representing a character joining an industry.
/// Published after membership is successfully added.
/// </summary>
public sealed record MemberJoinedIndustryEvent(
    CharacterId MemberId,
    IndustryTag IndustryTag,
    ProficiencyLevel InitialLevel,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}

