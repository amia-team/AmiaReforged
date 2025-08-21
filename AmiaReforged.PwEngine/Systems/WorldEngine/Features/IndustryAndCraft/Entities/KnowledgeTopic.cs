using AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft.ValueObjects;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft.Entities;

public sealed class KnowledgeTopic : Entity
{
    public TopicKey Key { get; private set; }
    public string Name { get; private set; }
    public string Description { get; private set; }

    // Optional parent topic for hierarchical organization
    public TopicKey? ParentTopic { get; private set; }

    // Knowledge entries that belong to this topic
    private readonly HashSet<KnowledgeKey> _knowledge = new();
    public IReadOnlySet<KnowledgeKey> Knowledge => _knowledge;

    private KnowledgeTopic(TopicKey key, string name, string description, TopicKey? parentTopic = null)
    {
        Id = Guid.NewGuid();
        Key = key;
        Name = name.Trim();
        Description = description.Trim();
        ParentTopic = parentTopic;
        LastUpdated = DateTime.UtcNow;
    }

    public static KnowledgeTopic Create(TopicKey key, string name, string description, TopicKey? parentTopic = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Name cannot be empty.", nameof(name));

        return new KnowledgeTopic(key, name, description, parentTopic);
    }

    public void AddKnowledge(KnowledgeKey knowledge)
    {
        if (_knowledge.Add(knowledge))
            Touch();
    }

    public void RemoveKnowledge(KnowledgeKey knowledge)
    {
        if (_knowledge.Remove(knowledge))
            Touch();
    }

    public void SetParent(TopicKey? parentTopic)
    {
        if (parentTopic?.Value == Key.Value)
            throw new InvalidOperationException("Topic cannot be its own parent.");

        ParentTopic = parentTopic;
        Touch();
    }

    private void Touch() => LastUpdated = DateTime.UtcNow;
}
