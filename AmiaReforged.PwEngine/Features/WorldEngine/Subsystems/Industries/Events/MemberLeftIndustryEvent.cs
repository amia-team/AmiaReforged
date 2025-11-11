using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.Events;

/// <summary>
/// Domain event representing a character leaving an industry.
/// Published after membership is successfully removed.
/// </summary>
public sealed record MemberLeftIndustryEvent(
    CharacterId MemberId,
    IndustryTag IndustryTag,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}

