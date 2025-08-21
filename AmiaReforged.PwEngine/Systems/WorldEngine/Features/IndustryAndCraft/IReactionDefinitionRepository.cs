using AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft.ValueObjects;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft;

public interface IReactionDefinitionRepository
{
    Task<ReactionDefinition?> FindByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<ReactionDefinition>> ListAllAsync(CancellationToken ct = default);
    Task AddOrUpdateAsync(ReactionDefinition definition, CancellationToken ct = default);
    Task RemoveAsync(Guid id, CancellationToken ct = default);
}
