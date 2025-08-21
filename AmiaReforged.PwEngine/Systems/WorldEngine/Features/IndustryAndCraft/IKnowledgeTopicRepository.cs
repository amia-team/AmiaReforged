using AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft.Entities;
using AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft.ValueObjects;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft;

public interface IKnowledgeTopicRepository
{
    Task<KnowledgeTopic?> FindByKeyAsync(TopicKey key, CancellationToken ct = default);
    Task<IReadOnlyList<KnowledgeTopic>> FindByParentAsync(TopicKey? parentKey, CancellationToken ct = default);
    Task<IReadOnlyList<KnowledgeTopic>> ListAllAsync(CancellationToken ct = default);
    Task AddOrUpdateAsync(KnowledgeTopic topic, CancellationToken ct = default);
    Task RemoveAsync(TopicKey key, CancellationToken ct = default);
}
