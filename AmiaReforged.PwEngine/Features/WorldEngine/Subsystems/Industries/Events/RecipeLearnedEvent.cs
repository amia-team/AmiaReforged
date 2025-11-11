using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.Events;

/// <summary>
/// Domain event representing a character learning knowledge (recipe, technique, etc.) within an industry.
/// Published after knowledge is successfully acquired and points are deducted.
/// </summary>
public sealed record RecipeLearnedEvent(
    CharacterId LearnerId,
    IndustryTag IndustryTag,
    string KnowledgeTag,
    int PointCost,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}

