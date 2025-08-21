using AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft.Entities;
using AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft.ValueObjects;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft;

public interface IKnowledgeDefinitionRepository
{
    Task<KnowledgeDefinition?> FindByKeyAsync(KnowledgeKey key, CancellationToken ct = default);
    Task<IReadOnlyList<KnowledgeDefinition>> FindByCategoryAsync(string category, CancellationToken ct = default);
    Task<IReadOnlyList<KnowledgeDefinition>> FindByTagAsync(string tag, CancellationToken ct = default);
    Task<IReadOnlyList<KnowledgeDefinition>> ListAllAsync(CancellationToken ct = default);

    // Find knowledge that can be learned given current knowledge (prerequisites met)
    Task<IReadOnlyList<KnowledgeDefinition>> FindAvailableAsync(IEnumerable<KnowledgeKey> knownKnowledge, CancellationToken ct = default);

    Task AddOrUpdateAsync(KnowledgeDefinition definition, CancellationToken ct = default);
    Task RemoveAsync(KnowledgeKey key, CancellationToken ct = default);
}
