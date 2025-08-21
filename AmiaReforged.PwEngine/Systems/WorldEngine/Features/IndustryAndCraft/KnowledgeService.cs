using AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft.Entities;
using AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft.ValueObjects;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft;

public sealed class CharacterKnowledgeService(
    ICharacterKnowledgeRepository characterKnowledge,
    IKnowledgeDefinitionRepository knowledgeDefinitions)
{
    public async Task<bool> CanLearnKnowledgeAsync(Guid characterId, KnowledgeKey knowledge, CancellationToken ct = default)
    {
        // Get knowledge definition
        KnowledgeDefinition? definition = await knowledgeDefinitions.FindByKeyAsync(knowledge, ct);
        if (definition is null) return false;

        // Get character's current knowledge
        IReadOnlySet<KnowledgeKey> knownKnowledge = await characterKnowledge.GetSetAsync(characterId, ct);

        // Already known?
        if (knownKnowledge.Contains(knowledge)) return false;

        // Check prerequisites
        return definition.Prerequisites.All(prereq => knownKnowledge.Contains(prereq));
    }

    public async Task<IReadOnlyList<KnowledgeDefinition>> GetAvailableKnowledgeAsync(
        Guid characterId,
        string? category = null,
        CancellationToken ct = default)
    {
        IReadOnlySet<KnowledgeKey> knownKnowledge = await characterKnowledge.GetSetAsync(characterId, ct);
        IReadOnlyList<KnowledgeDefinition> allKnowledge = category is null
            ? await knowledgeDefinitions.ListAllAsync(ct)
            : await knowledgeDefinitions.FindByCategoryAsync(category, ct);

        return allKnowledge
            .Where(def => !knownKnowledge.Contains(def.Key)) // Not already known
            .Where(def => def.Prerequisites.All(prereq => knownKnowledge.Contains(prereq))) // Prerequisites met
            .ToList();
    }

    public async Task<IReadOnlyList<KnowledgeDefinition>> GetKnownKnowledgeDefinitionsAsync(
        Guid characterId,
        CancellationToken ct = default)
    {
        IReadOnlySet<KnowledgeKey> knownKeys = await characterKnowledge.GetSetAsync(characterId, ct);
        List<KnowledgeDefinition> definitions = new();

        foreach (KnowledgeKey key in knownKeys)
        {
            KnowledgeDefinition? def = await knowledgeDefinitions.FindByKeyAsync(key, ct);
            if (def is not null) definitions.Add(def);
        }

        return definitions;
    }

    public async Task<bool> TeachKnowledgeAsync(Guid characterId, KnowledgeKey knowledge, CancellationToken ct = default)
    {
        if (!await CanLearnKnowledgeAsync(characterId, knowledge, ct))
            return false;

        await characterKnowledge.GrantAsync(characterId, knowledge, ct);
        return true;
    }

    public async Task<IReadOnlyDictionary<string, IReadOnlyList<KnowledgeDefinition>>> GetKnowledgeByCategoryAsync(
        Guid characterId,
        CancellationToken ct = default)
    {
        IReadOnlyList<KnowledgeDefinition> knownDefs = await GetKnownKnowledgeDefinitionsAsync(characterId, ct);

        return knownDefs
            .Where(def => !string.IsNullOrWhiteSpace(def.Category))
            .GroupBy(def => def.Category!)
            .ToDictionary(
                g => g.Key,
                g => (IReadOnlyList<KnowledgeDefinition>)g.ToList(),
                StringComparer.OrdinalIgnoreCase);
    }
}
