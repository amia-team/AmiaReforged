using AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft.Entities;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft.ValueObjects;

public sealed class KnowledgeLookup
{
    private readonly IReadOnlyDictionary<KnowledgeKey, KnowledgeDefinition> _definitions;
    private readonly IReadOnlyDictionary<string, IReadOnlyList<KnowledgeKey>> _byCategory;
    private readonly IReadOnlyDictionary<string, IReadOnlyList<KnowledgeKey>> _byTag;

    public KnowledgeLookup(IEnumerable<KnowledgeDefinition> definitions)
    {
        List<KnowledgeDefinition> defList = definitions.ToList();

        _definitions = defList.ToDictionary(d => d.Key, d => d);

        _byCategory = defList
            .Where(d => !string.IsNullOrWhiteSpace(d.Category))
            .GroupBy(d => d.Category!.ToLowerInvariant(), StringComparer.OrdinalIgnoreCase) // Normalize category key
            .ToDictionary(g => g.Key, g => (IReadOnlyList<KnowledgeKey>)g.Select(d => d.Key).ToList());

        _byTag = defList
            .SelectMany(d => d.Tags.Select(tag => new { Tag = tag, Key = d.Key }))
            .GroupBy(x => x.Tag.ToLowerInvariant(), StringComparer.OrdinalIgnoreCase) // Normalize tag key
            .ToDictionary(g => g.Key, g => (IReadOnlyList<KnowledgeKey>)g.Select(x => x.Key).ToList());
    }


    public KnowledgeDefinition? GetDefinition(KnowledgeKey key) =>
        _definitions.GetValueOrDefault(key);

    public IReadOnlyList<KnowledgeKey> GetByCategory(string category) =>
        _byCategory.GetValueOrDefault(category.ToLowerInvariant(), []);

    public IReadOnlyList<KnowledgeKey> GetByTag(string tag) =>
        _byTag.GetValueOrDefault(tag?.ToLowerInvariant() ?? string.Empty, []);


    public bool HasKnowledgeInCategory(IEnumerable<KnowledgeKey> knownKnowledge, string category)
    {
        IReadOnlyList<KnowledgeKey> categoryKnowledge = GetByCategory(category);
        return knownKnowledge.Any(k => categoryKnowledge.Contains(k));
    }

    public bool HasKnowledgeWithTag(IEnumerable<KnowledgeKey> knownKnowledge, string tag)
    {
        IReadOnlyList<KnowledgeKey> taggedKnowledge = GetByTag(tag);
        return knownKnowledge.Any(k => taggedKnowledge.Contains(k));
    }
}
