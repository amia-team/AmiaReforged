using AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft.ValueObjects;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft.Entities;

public sealed class KnowledgeDefinition : Entity
{
    public KnowledgeKey Key { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }

    // Optional grouping - could be craft, lore, technique, etc.
    public string? Category { get; private set; }

    // Prerequisites - knowledge that must be known first
    private readonly HashSet<KnowledgeKey> _prerequisites = new();
    public IReadOnlySet<KnowledgeKey> Prerequisites => _prerequisites;

    // Knowledge that this enables learning (unlocks)
    private readonly HashSet<KnowledgeKey> _unlocks = new();
    public IReadOnlySet<KnowledgeKey> Unlocks => _unlocks;

    // Tags for flexible categorization and querying
    private readonly HashSet<string> _tags = new(StringComparer.OrdinalIgnoreCase);
    public IReadOnlySet<string> Tags => _tags;

    private KnowledgeDefinition(KnowledgeKey key, string name, string description, string? category = null)
    {
        Id = Guid.NewGuid();
        Key = key;
        Name = name.Trim();
        Description = description.Trim();
        Category = category?.Trim();
        LastUpdated = DateTime.UtcNow;
    }

    public static KnowledgeDefinition Create(KnowledgeKey key, string name, string description, string? category = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty.", nameof(name));

        return new KnowledgeDefinition(key, name, description, category);
    }

    public void AddPrerequisite(KnowledgeKey prerequisite)
    {
        if (prerequisite.Value == Key.Value)
            throw new InvalidOperationException("Knowledge cannot be prerequisite to itself.");

        if (_prerequisites.Add(prerequisite))
            Touch();
    }

    public void RemovePrerequisite(KnowledgeKey prerequisite)
    {
        if (_prerequisites.Remove(prerequisite))
            Touch();
    }

    public void AddUnlock(KnowledgeKey unlock)
    {
        if (unlock.Value == Key.Value)
            throw new InvalidOperationException("Knowledge cannot unlock itself.");

        if (_unlocks.Add(unlock))
            Touch();
    }

    public void RemoveUnlock(KnowledgeKey unlock)
    {
        if (_unlocks.Remove(unlock))
            Touch();
    }

    public void AddTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag)) return;
        if (_tags.Add(tag.Trim()))
            Touch();
    }

    public void RemoveTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag)) return;
        if (_tags.Remove(tag.Trim()))
            Touch();
    }

    public bool HasTag(string tag) => !string.IsNullOrWhiteSpace(tag) && _tags.Contains(tag.Trim());

    private void Touch() => LastUpdated = DateTime.UtcNow;
}
