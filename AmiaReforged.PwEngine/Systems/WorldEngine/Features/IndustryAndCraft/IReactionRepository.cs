using AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft.ValueObjects;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft;

public interface IReactionRepository
{
    Task<IReadOnlyList<ReactionDefinition>> LoadAllReactionsAsync(CancellationToken cancellationToken = default);
    Task<ReactionDefinition?> GetReactionByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ReactionDefinition>> GetReactionsByNameAsync(string name, CancellationToken cancellationToken = default);
}
