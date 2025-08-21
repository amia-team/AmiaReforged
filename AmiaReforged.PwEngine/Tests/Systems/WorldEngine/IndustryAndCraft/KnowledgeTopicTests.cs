using AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft.Entities;
using AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft.ValueObjects;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.IndustryAndCraft;

[TestFixture]
public class KnowledgeTopicTests
{
    [Test]
    public void Create_SetsFields_TrimsInputs()
    {
        TopicKey key = TopicKey.From("CRAFTING");
        TopicKey parent = TopicKey.From("SKILLS");

        KnowledgeTopic topic = KnowledgeTopic.Create(
            key,
            "  Crafting  ",
            "  All about crafting items  ",
            parent);

        Assert.That(topic.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(topic.Key, Is.EqualTo(key));
        Assert.That(topic.Name, Is.EqualTo("Crafting"));
        Assert.That(topic.Description, Is.EqualTo("All about crafting items"));
        Assert.That(topic.ParentTopic, Is.EqualTo(parent));
        Assert.That(topic.Knowledge, Is.Empty);
        Assert.That(topic.LastUpdated, Is.InRange(DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(1)));
    }

    [Test]
    public void Create_WithNullParent_AcceptsNull()
    {
        KnowledgeTopic topic = KnowledgeTopic.Create(
            TopicKey.From("ROOT"),
            "Root Topic",
            "Root level topic",
            parentTopic: null);

        Assert.That(topic.ParentTopic, Is.Null);
    }

    [Test]
    public void Create_Throws_OnEmptyName()
    {
        Assert.Throws<ArgumentException>(() =>
            KnowledgeTopic.Create(
                TopicKey.From("TEST"),
                "",
                "Description"));
    }

    [Test]
    public void AddKnowledge_AddsToCollection_TouchesLastUpdated()
    {
        KnowledgeTopic topic = CreateTestTopic();
        DateTime before = topic.LastUpdated;
        KnowledgeKey knowledge = KnowledgeKey.From("BLACKSMITHING");

        topic.AddKnowledge(knowledge);

        Assert.That(topic.Knowledge, Contains.Item(knowledge));
        Assert.That(topic.LastUpdated, Is.GreaterThanOrEqualTo(before));
    }

    [Test]
    public void AddKnowledge_IsIdempotent()
    {
        KnowledgeTopic topic = CreateTestTopic();
        KnowledgeKey knowledge = KnowledgeKey.From("BLACKSMITHING");

        topic.AddKnowledge(knowledge);
        DateTime afterFirst = topic.LastUpdated;

        topic.AddKnowledge(knowledge); // Second add

        Assert.That(topic.Knowledge.Count, Is.EqualTo(1));
        Assert.That(topic.LastUpdated, Is.EqualTo(afterFirst));
    }

    [Test]
    public void RemoveKnowledge_RemovesFromCollection_TouchesLastUpdated()
    {
        KnowledgeTopic topic = CreateTestTopic();
        KnowledgeKey knowledge = KnowledgeKey.From("BLACKSMITHING");
        topic.AddKnowledge(knowledge);
        DateTime before = topic.LastUpdated;

        topic.RemoveKnowledge(knowledge);

        Assert.That(topic.Knowledge, Does.Not.Contain(knowledge));
        Assert.That(topic.LastUpdated, Is.GreaterThanOrEqualTo(before));
    }

    [Test]
    public void SetParent_UpdatesParent_TouchesLastUpdated()
    {
        KnowledgeTopic topic = CreateTestTopic();
        DateTime before = topic.LastUpdated;
        TopicKey newParent = TopicKey.From("NEW_PARENT");

        topic.SetParent(newParent);

        Assert.That(topic.ParentTopic, Is.EqualTo(newParent));
        Assert.That(topic.LastUpdated, Is.GreaterThanOrEqualTo(before));
    }

    [Test]
    public void SetParent_Throws_WhenSelfReferential()
    {
        KnowledgeTopic topic = CreateTestTopic();

        Assert.Throws<InvalidOperationException>(() =>
            topic.SetParent(topic.Key));
    }

    [Test]
    public void SetParent_AcceptsNull()
    {
        KnowledgeTopic topic = CreateTestTopic();

        topic.SetParent(null);

        Assert.That(topic.ParentTopic, Is.Null);
    }

    private static KnowledgeTopic CreateTestTopic() =>
        KnowledgeTopic.Create(
            TopicKey.From("TEST_TOPIC"),
            "Test Topic",
            "Test Description");
}
