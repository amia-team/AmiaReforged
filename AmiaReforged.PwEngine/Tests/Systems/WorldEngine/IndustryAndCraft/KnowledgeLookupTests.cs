using AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft.Entities;
using AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft.ValueObjects;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.IndustryAndCraft;

[TestFixture]
public class KnowledgeLookupTests
{
    private List<KnowledgeDefinition> _definitions;
    private KnowledgeLookup _lookup;

    [SetUp]
    public void SetUp()
    {
        _definitions = new List<KnowledgeDefinition>
        {
            CreateKnowledgeWithTags("SWORD_FIGHTING", "COMBAT", new[] { "weapon", "melee" }),
            CreateKnowledgeWithTags("ARCHERY", "COMBAT", new[] { "weapon", "ranged" }),
            CreateKnowledgeWithTags("BLACKSMITHING", "CRAFTING", new[] { "forge", "metal" }),
            CreateKnowledgeWithTags("ALCHEMY", "CRAFTING", new[] { "brew", "magic" }),
            CreateKnowledgeWithTags("HISTORY", null, new[] { "academic", "lore" }) // No category
        };

        _lookup = new KnowledgeLookup(_definitions);
    }

    [Test]
    public void GetDefinition_ReturnsCorrectDefinition()
    {
        // Arrange
        KnowledgeKey key = KnowledgeKey.From("SWORD_FIGHTING");

        // Act
        KnowledgeDefinition? result = _lookup.GetDefinition(key);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Key, Is.EqualTo(key));
        Assert.That(result.Category, Is.EqualTo("COMBAT"));
    }

    [Test]
    public void GetDefinition_ReturnsNull_WhenNotFound()
    {
        // Arrange
        KnowledgeKey key = KnowledgeKey.From("NONEXISTENT");

        // Act
        KnowledgeDefinition? result = _lookup.GetDefinition(key);

        // Assert
        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetByCategory_ReturnsCorrectKnowledge()
    {
        // Act
        IReadOnlyList<KnowledgeKey> combatSkills = _lookup.GetByCategory("COMBAT");
        IReadOnlyList<KnowledgeKey> craftingSkills = _lookup.GetByCategory("CRAFTING");

        // Assert
        Assert.That(combatSkills.Count, Is.EqualTo(2));
        Assert.That(combatSkills, Contains.Item(KnowledgeKey.From("SWORD_FIGHTING")));
        Assert.That(combatSkills, Contains.Item(KnowledgeKey.From("ARCHERY")));

        Assert.That(craftingSkills.Count, Is.EqualTo(2));
        Assert.That(craftingSkills, Contains.Item(KnowledgeKey.From("BLACKSMITHING")));
        Assert.That(craftingSkills, Contains.Item(KnowledgeKey.From("ALCHEMY")));
    }

    [Test]
    public void GetByCategory_IsCaseInsensitive()
    {
        // Act
        IReadOnlyList<KnowledgeKey> result1 = _lookup.GetByCategory("combat");
        IReadOnlyList<KnowledgeKey> result2 = _lookup.GetByCategory("COMBAT");

        // Assert
        Assert.That(result1, Is.EquivalentTo(result2));
    }

    [Test]
    public void GetByCategory_ReturnsEmpty_WhenCategoryNotFound()
    {
        // Act
        IReadOnlyList<KnowledgeKey> result = _lookup.GetByCategory("NONEXISTENT");

        // Assert
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void GetByTag_ReturnsCorrectKnowledge()
    {
        // Act
        IReadOnlyList<KnowledgeKey> weaponSkills = _lookup.GetByTag("weapon");
        IReadOnlyList<KnowledgeKey> academicSkills = _lookup.GetByTag("academic");

        // Assert
        Assert.That(weaponSkills.Count, Is.EqualTo(2));
        Assert.That(weaponSkills, Contains.Item(KnowledgeKey.From("SWORD_FIGHTING")));
        Assert.That(weaponSkills, Contains.Item(KnowledgeKey.From("ARCHERY")));

        Assert.That(academicSkills.Count, Is.EqualTo(1));
        Assert.That(academicSkills, Contains.Item(KnowledgeKey.From("HISTORY")));
    }

    [Test]
    public void HasKnowledgeInCategory_ReturnsTrue_WhenCharacterHasAny()
    {
        // Arrange
        KnowledgeKey[] knownKnowledge = { KnowledgeKey.From("SWORD_FIGHTING"), KnowledgeKey.From("HISTORY") };

        // Act
        bool hasCombat = _lookup.HasKnowledgeInCategory(knownKnowledge, "COMBAT");
        bool hasCrafting = _lookup.HasKnowledgeInCategory(knownKnowledge, "CRAFTING");

        // Assert
        Assert.That(hasCombat, Is.True);
        Assert.That(hasCrafting, Is.False);
    }

    [Test]
    public void HasKnowledgeWithTag_ReturnsTrue_WhenCharacterHasAny()
    {
        // Arrange
        KnowledgeKey[] knownKnowledge = { KnowledgeKey.From("BLACKSMITHING"), KnowledgeKey.From("HISTORY") };

        // Act
        bool hasWeapon = _lookup.HasKnowledgeWithTag(knownKnowledge, "weapon");
        bool hasMetal = _lookup.HasKnowledgeWithTag(knownKnowledge, "metal");
        bool hasAcademic = _lookup.HasKnowledgeWithTag(knownKnowledge, "academic");

        // Assert
        Assert.That(hasWeapon, Is.False);
        Assert.That(hasMetal, Is.True);
        Assert.That(hasAcademic, Is.True);
    }

    private static KnowledgeDefinition CreateKnowledgeWithTags(string key, string? category, string[] tags)
    {
        KnowledgeDefinition def = KnowledgeDefinition.Create(
            KnowledgeKey.From(key),
            key.Replace("_", " "),
            $"Description for {key}",
            category);

        foreach (string tag in tags)
            def.AddTag(tag);

        return def;
    }
}
