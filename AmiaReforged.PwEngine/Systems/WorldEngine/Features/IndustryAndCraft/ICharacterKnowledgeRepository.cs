using AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft.ValueObjects;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft;

public interface ICharacterKnowledgeRepository
{
    Task<IReadOnlySet<KnowledgeKey>> GetSetAsync(Guid characterId, CancellationToken ct = default);
    Task GrantAsync(Guid characterId, KnowledgeKey key, CancellationToken ct = default);
    Task RevokeAsync(Guid characterId, KnowledgeKey key, CancellationToken ct = default);
}