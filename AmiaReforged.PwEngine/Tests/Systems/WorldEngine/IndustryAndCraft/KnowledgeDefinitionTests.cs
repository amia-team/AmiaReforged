using AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft.Entities;
using AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft.ValueObjects;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.IndustryAndCraft;

[TestFixture]
public class KnowledgeDefinitionTests
{
    [Test]
    public void Create_SetsFields_TrimsInputs()
    {
        KnowledgeKey key = KnowledgeKey.From("BLACKSMITHING");
        KnowledgeDefinition def = KnowledgeDefinition.Create(
            key,
            "  Blacksmithing  ",
            "  The art of working with metal  ",
            "  Crafting  ");

        Assert.That(def.Id, Is.Not.EqualTo(Guid.Empty));
        Assert.That(def.Key, Is.EqualTo(key));
        Assert.That(def.Name, Is.EqualTo("Blacksmithing"));
        Assert.That(def.Description, Is.EqualTo("The art of working with metal"));
        Assert.That(def.Category, Is.EqualTo("Crafting"));
        Assert.That(def.Prerequisites, Is.Empty);
        Assert.That(def.Unlocks, Is.Empty);
        Assert.That(def.Tags, Is.Empty);
        Assert.That(def.LastUpdated, Is.InRange(DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow.AddMinutes(1)));
    }

    [Test]
    public void Create_WithNullCategory_AcceptsNull()
    {
        KnowledgeDefinition def = KnowledgeDefinition.Create(
            KnowledgeKey.From("TEST"),
            "Test Knowledge",
            "Description",
            category: null);

        Assert.That(def.Category, Is.Null);
    }

    [Test]
    public void Create_Throws_OnEmptyName()
    {
        Assert.Throws<ArgumentException>(() =>
            KnowledgeDefinition.Create(
                KnowledgeKey.From("TEST"),
                "",
                "Description"));

        Assert.Throws<ArgumentException>(() =>
            KnowledgeDefinition.Create(
                KnowledgeKey.From("TEST"),
                "   ",
                "Description"));
    }

    [Test]
    public void AddPrerequisite_AddsToCollection_TouchesLastUpdated()
    {
        KnowledgeDefinition def = CreateTestKnowledge();
        DateTime before = def.LastUpdated;
        KnowledgeKey prereq = KnowledgeKey.From("BASIC_CRAFTING");

        def.AddPrerequisite(prereq);

        Assert.That(def.Prerequisites, Contains.Item(prereq));
        Assert.That(def.LastUpdated, Is.GreaterThanOrEqualTo(before));
    }

    [Test]
    public void AddPrerequisite_Throws_WhenSelfReferential()
    {
        KnowledgeDefinition def = CreateTestKnowledge();

        Assert.Throws<InvalidOperationException>(() =>
            def.AddPrerequisite(def.Key));
    }

    [Test]
    public void AddPrerequisite_IsIdempotent()
    {
        KnowledgeDefinition def = CreateTestKnowledge();
        KnowledgeKey prereq = KnowledgeKey.From("BASIC_CRAFTING");

        def.AddPrerequisite(prereq);
        DateTime afterFirst = def.LastUpdated;

        def.AddPrerequisite(prereq); // Second add

        Assert.That(def.Prerequisites.Count, Is.EqualTo(1));
        Assert.That(def.LastUpdated, Is.EqualTo(afterFirst)); // No change on duplicate
    }

    [Test]
    public void RemovePrerequisite_RemovesFromCollection_TouchesLastUpdated()
    {
        KnowledgeDefinition def = CreateTestKnowledge();
        KnowledgeKey prereq = KnowledgeKey.From("BASIC_CRAFTING");
        def.AddPrerequisite(prereq);
        DateTime before = def.LastUpdated;

        def.RemovePrerequisite(prereq);

        Assert.That(def.Prerequisites, Does.Not.Contain(prereq));
        Assert.That(def.LastUpdated, Is.GreaterThanOrEqualTo(before));
    }

    [Test]
    public void RemovePrerequisite_IsIdempotent()
    {
        KnowledgeDefinition def = CreateTestKnowledge();
        KnowledgeKey prereq = KnowledgeKey.From("NONEXISTENT");

        def.RemovePrerequisite(prereq); // Remove non-existent

        Assert.That(def.Prerequisites, Is.Empty);
    }

    [Test]
    public void AddUnlock_AddsToCollection_TouchesLastUpdated()
    {
        KnowledgeDefinition def = CreateTestKnowledge();
        DateTime before = def.LastUpdated;
        KnowledgeKey unlock = KnowledgeKey.From("ADVANCED_SMITHING");

        def.AddUnlock(unlock);

        Assert.That(def.Unlocks, Contains.Item(unlock));
        Assert.That(def.LastUpdated, Is.GreaterThanOrEqualTo(before));
    }

    [Test]
    public void AddUnlock_Throws_WhenSelfReferential()
    {
        KnowledgeDefinition def = CreateTestKnowledge();

        Assert.Throws<InvalidOperationException>(() =>
            def.AddUnlock(def.Key));
    }

    [Test]
    public void AddTag_AddsToCollection_TouchesLastUpdated()
    {
        KnowledgeDefinition def = CreateTestKnowledge();
        DateTime before = def.LastUpdated;

        def.AddTag("  COMBAT  ");

        Assert.That(def.Tags, Contains.Item("COMBAT"));
        Assert.That(def.LastUpdated, Is.GreaterThanOrEqualTo(before));
    }

    [Test]
    public void AddTag_IgnoresEmptyOrWhitespace()
    {
        KnowledgeDefinition def = CreateTestKnowledge();
        DateTime before = def.LastUpdated;

        def.AddTag("");
        def.AddTag("   ");

        Assert.That(def.Tags, Is.Empty);
        Assert.That(def.LastUpdated, Is.EqualTo(before)); // No change
    }

    [Test]
    public void HasTag_IsCaseInsensitive()
    {
        KnowledgeDefinition def = CreateTestKnowledge();
        def.AddTag("Combat");

        Assert.That(def.HasTag("combat"), Is.True);
        Assert.That(def.HasTag("COMBAT"), Is.True);
        Assert.That(def.HasTag("Combat"), Is.True);
        Assert.That(def.HasTag("invalid"), Is.False);
    }

    [Test]
    public void RemoveTag_RemovesFromCollection_TouchesLastUpdated()
    {
        KnowledgeDefinition def = CreateTestKnowledge();
        def.AddTag("COMBAT");
        DateTime before = def.LastUpdated;

        def.RemoveTag("combat"); // Case insensitive

        Assert.That(def.Tags, Does.Not.Contain("COMBAT"));
        Assert.That(def.LastUpdated, Is.GreaterThanOrEqualTo(before));
    }

    private static KnowledgeDefinition CreateTestKnowledge() =>
        KnowledgeDefinition.Create(
            KnowledgeKey.From("TEST_KNOWLEDGE"),
            "Test Knowledge",
            "Test Description",
            "Test Category");
}
