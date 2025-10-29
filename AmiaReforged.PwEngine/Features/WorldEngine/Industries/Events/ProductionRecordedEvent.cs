using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Industries.Events;

/// <summary>
/// Domain event representing production/crafting activity recorded for an industry member.
/// Published after a character successfully crafts an item using a recipe.
/// </summary>
public sealed record ProductionRecordedEvent(
    CharacterId ProducerId,
    IndustryTag IndustryTag,
    RecipeId RecipeId,
    int KnowledgePointsAwarded,
    DateTime OccurredAt) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
}

