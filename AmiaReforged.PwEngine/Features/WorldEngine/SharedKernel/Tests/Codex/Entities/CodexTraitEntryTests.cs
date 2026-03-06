using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Enums;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Entities;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Tests.Codex.Entities;

[TestFixture]
public class CodexTraitEntryTests
{
    private DateTime _testDate;

    [SetUp]
    public void SetUp()
    {
        _testDate = new DateTime(2025, 10, 22, 12, 0, 0);
    }

    #region Construction Tests

    [Test]
    public void Constructor_WithValidRequiredProperties_CreatesInstance()
    {
        // Arrange & Act
        CodexTraitEntry trait = new CodexTraitEntry
        {
            TraitTag = new TraitTag("brave"),
            Name = "Brave",
            Description = "This character is exceptionally courageous.",
            Category = TraitCategory.Personality,
            AcquisitionMethod = "Character Creation",
            DateAcquired = _testDate
        };

        // Assert
        Assert.That(trait.TraitTag.Value, Is.EqualTo("brave"));
        Assert.That(trait.Name, Is.EqualTo("Brave"));
        Assert.That(trait.Description, Is.EqualTo("This character is exceptionally courageous."));
        Assert.That(trait.Category, Is.EqualTo(TraitCategory.Personality));
        Assert.That(trait.AcquisitionMethod, Is.EqualTo("Character Creation"));
        Assert.That(trait.DateAcquired, Is.EqualTo(_testDate));
    }

    #endregion

    #region MatchesSearch Tests

    [Test]
    public void MatchesSearch_WithMatchingName_ReturnsTrue()
    {
        CodexTraitEntry trait = CreateTestTrait("brave", "Brave", "Courageous in battle");
        Assert.That(trait.MatchesSearch("brave"), Is.True);
    }

    [Test]
    public void MatchesSearch_WithMatchingDescription_ReturnsTrue()
    {
        CodexTraitEntry trait = CreateTestTrait("brave", "Brave", "Courageous in battle");
        Assert.That(trait.MatchesSearch("courageous"), Is.True);
    }

    [Test]
    public void MatchesSearch_WithMatchingCategoryDisplayName_ReturnsTrue()
    {
        CodexTraitEntry trait = CreateTestTrait("brave", "Brave", "Some description", TraitCategory.Personality);
        Assert.That(trait.MatchesSearch("personality"), Is.True);
    }

    [Test]
    public void MatchesSearch_WithMatchingAcquisitionMethod_ReturnsTrue()
    {
        CodexTraitEntry trait = CreateTestTrait("brave", "Brave", "Some description");
        Assert.That(trait.MatchesSearch("character creation"), Is.True);
    }

    [Test]
    public void MatchesSearch_CaseInsensitive_ReturnsTrue()
    {
        CodexTraitEntry trait = CreateTestTrait("brave", "Brave", "Courageous in battle");
        Assert.That(trait.MatchesSearch("BRAVE"), Is.True);
    }

    [Test]
    public void MatchesSearch_WithNonMatchingTerm_ReturnsFalse()
    {
        CodexTraitEntry trait = CreateTestTrait("brave", "Brave", "Courageous in battle");
        Assert.That(trait.MatchesSearch("sneaky"), Is.False);
    }

    [Test]
    public void MatchesSearch_WithNullOrWhitespace_ReturnsFalse()
    {
        CodexTraitEntry trait = CreateTestTrait("brave", "Brave", "Courageous in battle");
        Assert.That(trait.MatchesSearch(""), Is.False);
        Assert.That(trait.MatchesSearch("  "), Is.False);
        Assert.That(trait.MatchesSearch(null!), Is.False);
    }

    #endregion

    #region MatchesCategory Tests

    [Test]
    public void MatchesCategory_WithSameCategory_ReturnsTrue()
    {
        CodexTraitEntry trait = CreateTestTrait("brave", "Brave", "Courageous", TraitCategory.Personality);
        Assert.That(trait.MatchesCategory(TraitCategory.Personality), Is.True);
    }

    [Test]
    public void MatchesCategory_WithDifferentCategory_ReturnsFalse()
    {
        CodexTraitEntry trait = CreateTestTrait("brave", "Brave", "Courageous", TraitCategory.Personality);
        Assert.That(trait.MatchesCategory(TraitCategory.Physical), Is.False);
    }

    [Test]
    [TestCase(TraitCategory.Background)]
    [TestCase(TraitCategory.Personality)]
    [TestCase(TraitCategory.Physical)]
    [TestCase(TraitCategory.Mental)]
    [TestCase(TraitCategory.Social)]
    [TestCase(TraitCategory.Supernatural)]
    [TestCase(TraitCategory.Curse)]
    [TestCase(TraitCategory.Blessing)]
    public void MatchesCategory_EachCategory_MatchesItself(TraitCategory category)
    {
        CodexTraitEntry trait = CreateTestTrait("test", "Test", "Test", category);
        Assert.That(trait.MatchesCategory(category), Is.True);
    }

    #endregion

    #region Helper Methods

    private CodexTraitEntry CreateTestTrait(
        string tag, string name, string description,
        TraitCategory category = TraitCategory.Personality)
    {
        return new CodexTraitEntry
        {
            TraitTag = new TraitTag(tag),
            Name = name,
            Description = description,
            Category = category,
            AcquisitionMethod = "Character Creation",
            DateAcquired = _testDate
        };
    }

    #endregion
}
