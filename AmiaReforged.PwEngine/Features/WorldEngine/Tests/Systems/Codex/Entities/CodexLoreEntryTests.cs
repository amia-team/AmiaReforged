using AmiaReforged.PwEngine.Features.Codex.Domain.Enums;
using AmiaReforged.PwEngine.Features.Codex.Domain.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Entities;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Tests.Systems.Codex.Entities;

[TestFixture]
public class CodexLoreEntryTests
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
        CodexLoreEntry lore = new CodexLoreEntry
        {
            LoreId = new LoreId("lore_001"),
            Title = "The Ancient Dragon Wars",
            Content = "Long ago, dragons ruled the skies...",
            Category = "History",
            Tier = LoreTier.Common,
            DateDiscovered = _testDate
        };

        // Assert
        Assert.That(lore.LoreId.Value, Is.EqualTo("lore_001"));
        Assert.That(lore.Title, Is.EqualTo("The Ancient Dragon Wars"));
        Assert.That(lore.Content, Is.EqualTo("Long ago, dragons ruled the skies..."));
        Assert.That(lore.Category, Is.EqualTo("History"));
        Assert.That(lore.Tier, Is.EqualTo(LoreTier.Common));
        Assert.That(lore.DateDiscovered, Is.EqualTo(_testDate));
    }

    [Test]
    public void Constructor_WithOptionalProperties_SetsThemCorrectly()
    {
        // Arrange & Act
        CodexLoreEntry lore = new CodexLoreEntry
        {
            LoreId = new LoreId("lore_002"),
            Title = "The Lost City",
            Content = "A magnificent city that vanished...",
            Category = "Geography",
            Tier = LoreTier.Rare,
            DateDiscovered = _testDate,
            DiscoveryLocation = "Ancient Library",
            DiscoverySource = "Dusty Tome",
            Keywords = new List<Keyword> { new Keyword("city"), new Keyword("ancient") }
        };

        // Assert
        Assert.That(lore.DiscoveryLocation, Is.EqualTo("Ancient Library"));
        Assert.That(lore.DiscoverySource, Is.EqualTo("Dusty Tome"));
        Assert.That(lore.Keywords, Has.Count.EqualTo(2));
    }

    [Test]
    public void Constructor_WithEmptyKeywords_CreatesEmptyList()
    {
        // Arrange & Act
        CodexLoreEntry lore = new CodexLoreEntry
        {
            LoreId = new LoreId("lore_003"),
            Title = "Simple Lore",
            Content = "Simple content",
            Category = "General",
            Tier = LoreTier.Common,
            DateDiscovered = _testDate
        };

        // Assert
        Assert.That(lore.Keywords, Is.Not.Null);
        Assert.That(lore.Keywords, Is.Empty);
    }

    [Test]
    public void Constructor_WithAllTiers_CreatesCorrectly()
    {
        // Arrange & Act
        CodexLoreEntry commonLore = new CodexLoreEntry
        {
            LoreId = new LoreId("lore_common"),
            Title = "Common Knowledge",
            Content = "Everyone knows this",
            Category = "General",
            Tier = LoreTier.Common,
            DateDiscovered = _testDate
        };

        CodexLoreEntry uncommonLore = new CodexLoreEntry
        {
            LoreId = new LoreId("lore_uncommon"),
            Title = "Uncommon Knowledge",
            Content = "Some know this",
            Category = "General",
            Tier = LoreTier.Uncommon,
            DateDiscovered = _testDate
        };

        CodexLoreEntry rareLore = new CodexLoreEntry
        {
            LoreId = new LoreId("lore_rare"),
            Title = "Rare Knowledge",
            Content = "Few know this",
            Category = "General",
            Tier = LoreTier.Rare,
            DateDiscovered = _testDate
        };

        CodexLoreEntry legendaryLore = new CodexLoreEntry
        {
            LoreId = new LoreId("lore_legendary"),
            Title = "Legendary Knowledge",
            Content = "Almost no one knows this",
            Category = "General",
            Tier = LoreTier.Legendary,
            DateDiscovered = _testDate
        };

        // Assert
        Assert.That(commonLore.Tier, Is.EqualTo(LoreTier.Common));
        Assert.That(uncommonLore.Tier, Is.EqualTo(LoreTier.Uncommon));
        Assert.That(rareLore.Tier, Is.EqualTo(LoreTier.Rare));
        Assert.That(legendaryLore.Tier, Is.EqualTo(LoreTier.Legendary));
    }

    #endregion

    #region MatchesSearch Tests

    [Test]
    public void MatchesSearch_WithTitleMatch_ReturnsTrue()
    {
        // Arrange
        CodexLoreEntry lore = new CodexLoreEntry
        {
            LoreId = new LoreId("lore_004"),
            Title = "The Ancient Dragon Wars",
            Content = "Content here",
            Category = "History",
            Tier = LoreTier.Common,
            DateDiscovered = _testDate
        };

        // Act & Assert
        Assert.That(lore.MatchesSearch("dragon"), Is.True);
        Assert.That(lore.MatchesSearch("ancient"), Is.True);
        Assert.That(lore.MatchesSearch("wars"), Is.True);
    }

    [Test]
    public void MatchesSearch_WithContentMatch_ReturnsTrue()
    {
        // Arrange
        CodexLoreEntry lore = new CodexLoreEntry
        {
            LoreId = new LoreId("lore_005"),
            Title = "Simple Title",
            Content = "Long ago, the legendary heroes fought against the darkness",
            Category = "History",
            Tier = LoreTier.Common,
            DateDiscovered = _testDate
        };

        // Act & Assert
        Assert.That(lore.MatchesSearch("legendary"), Is.True);
        Assert.That(lore.MatchesSearch("heroes"), Is.True);
        Assert.That(lore.MatchesSearch("darkness"), Is.True);
    }

    [Test]
    public void MatchesSearch_WithCategoryMatch_ReturnsTrue()
    {
        // Arrange
        CodexLoreEntry lore = new CodexLoreEntry
        {
            LoreId = new LoreId("lore_006"),
            Title = "Test",
            Content = "Test content",
            Category = "Geography",
            Tier = LoreTier.Common,
            DateDiscovered = _testDate
        };

        // Act & Assert
        Assert.That(lore.MatchesSearch("geography"), Is.True);
        Assert.That(lore.MatchesSearch("GEOGRAPHY"), Is.True);
    }

    [Test]
    public void MatchesSearch_WithDiscoveryLocationMatch_ReturnsTrue()
    {
        // Arrange
        CodexLoreEntry lore = new CodexLoreEntry
        {
            LoreId = new LoreId("lore_007"),
            Title = "Test",
            Content = "Test content",
            Category = "History",
            Tier = LoreTier.Common,
            DateDiscovered = _testDate,
            DiscoveryLocation = "Ancient Library"
        };

        // Act & Assert
        Assert.That(lore.MatchesSearch("library"), Is.True);
        Assert.That(lore.MatchesSearch("ancient"), Is.True);
    }

    [Test]
    public void MatchesSearch_WithDiscoverySourceMatch_ReturnsTrue()
    {
        // Arrange
        CodexLoreEntry lore = new CodexLoreEntry
        {
            LoreId = new LoreId("lore_008"),
            Title = "Test",
            Content = "Test content",
            Category = "History",
            Tier = LoreTier.Common,
            DateDiscovered = _testDate,
            DiscoverySource = "Dusty Tome"
        };

        // Act & Assert
        Assert.That(lore.MatchesSearch("dusty"), Is.True);
        Assert.That(lore.MatchesSearch("tome"), Is.True);
    }

    [Test]
    public void MatchesSearch_WithKeywordMatch_ReturnsTrue()
    {
        // Arrange
        CodexLoreEntry lore = new CodexLoreEntry
        {
            LoreId = new LoreId("lore_009"),
            Title = "Test",
            Content = "Test content",
            Category = "History",
            Tier = LoreTier.Common,
            DateDiscovered = _testDate,
            Keywords = new List<Keyword> { new Keyword("dragons"), new Keyword("magic") }
        };

        // Act & Assert
        Assert.That(lore.MatchesSearch("dragons"), Is.True);
        Assert.That(lore.MatchesSearch("magic"), Is.True);
    }

    [Test]
    public void MatchesSearch_WithNoMatch_ReturnsFalse()
    {
        // Arrange
        CodexLoreEntry lore = new CodexLoreEntry
        {
            LoreId = new LoreId("lore_010"),
            Title = "Simple Title",
            Content = "Simple content",
            Category = "History",
            Tier = LoreTier.Common,
            DateDiscovered = _testDate
        };

        // Act & Assert
        Assert.That(lore.MatchesSearch("dragon"), Is.False);
        Assert.That(lore.MatchesSearch("treasure"), Is.False);
    }

    [Test]
    public void MatchesSearch_WithNullDiscoveryLocation_ReturnsFalse()
    {
        // Arrange
        CodexLoreEntry lore = new CodexLoreEntry
        {
            LoreId = new LoreId("lore_011"),
            Title = "Test",
            Content = "Test content",
            Category = "History",
            Tier = LoreTier.Common,
            DateDiscovered = _testDate,
            DiscoveryLocation = null
        };

        // Act & Assert
        Assert.That(lore.MatchesSearch("location"), Is.False);
    }

    [Test]
    public void MatchesSearch_WithNullDiscoverySource_ReturnsFalse()
    {
        // Arrange
        CodexLoreEntry lore = new CodexLoreEntry
        {
            LoreId = new LoreId("lore_012"),
            Title = "Test",
            Content = "Test content",
            Category = "History",
            Tier = LoreTier.Common,
            DateDiscovered = _testDate,
            DiscoverySource = null
        };

        // Act & Assert
        Assert.That(lore.MatchesSearch("source"), Is.False);
    }

    [Test]
    public void MatchesSearch_WithEmptySearchTerm_ReturnsFalse()
    {
        // Arrange
        CodexLoreEntry lore = new CodexLoreEntry
        {
            LoreId = new LoreId("lore_013"),
            Title = "Test",
            Content = "Test content",
            Category = "History",
            Tier = LoreTier.Common,
            DateDiscovered = _testDate
        };

        // Act & Assert
        Assert.That(lore.MatchesSearch(""), Is.False);
        Assert.That(lore.MatchesSearch("   "), Is.False);
    }

    [Test]
    public void MatchesSearch_WithNullSearchTerm_ReturnsFalse()
    {
        // Arrange
        CodexLoreEntry lore = new CodexLoreEntry
        {
            LoreId = new LoreId("lore_014"),
            Title = "Test",
            Content = "Test content",
            Category = "History",
            Tier = LoreTier.Common,
            DateDiscovered = _testDate
        };

        // Act & Assert
        Assert.That(lore.MatchesSearch(null!), Is.False);
    }

    [Test]
    public void MatchesSearch_IsCaseInsensitive()
    {
        // Arrange
        CodexLoreEntry lore = new CodexLoreEntry
        {
            LoreId = new LoreId("lore_015"),
            Title = "The ANCIENT Dragon Wars",
            Content = "legendary content here",
            Category = "History",
            Tier = LoreTier.Common,
            DateDiscovered = _testDate,
            DiscoveryLocation = "Ancient LIBRARY",
            DiscoverySource = "DUSTY Tome"
        };

        // Act & Assert
        Assert.That(lore.MatchesSearch("ancient"), Is.True);
        Assert.That(lore.MatchesSearch("LEGENDARY"), Is.True);
        Assert.That(lore.MatchesSearch("library"), Is.True);
        Assert.That(lore.MatchesSearch("dusty"), Is.True);
    }

    #endregion

    #region MatchesTier Tests

    [Test]
    public void MatchesTier_WithMatchingTier_ReturnsTrue()
    {
        // Arrange
        CodexLoreEntry lore = new CodexLoreEntry
        {
            LoreId = new LoreId("lore_016"),
            Title = "Test",
            Content = "Test content",
            Category = "History",
            Tier = LoreTier.Rare,
            DateDiscovered = _testDate
        };

        // Act & Assert
        Assert.That(lore.MatchesTier(LoreTier.Rare), Is.True);
    }

    [Test]
    public void MatchesTier_WithDifferentTier_ReturnsFalse()
    {
        // Arrange
        CodexLoreEntry lore = new CodexLoreEntry
        {
            LoreId = new LoreId("lore_017"),
            Title = "Test",
            Content = "Test content",
            Category = "History",
            Tier = LoreTier.Common,
            DateDiscovered = _testDate
        };

        // Act & Assert
        Assert.That(lore.MatchesTier(LoreTier.Rare), Is.False);
        Assert.That(lore.MatchesTier(LoreTier.Legendary), Is.False);
    }

    [Test]
    public void MatchesTier_WithAllTiers_WorksCorrectly()
    {
        // Arrange
        CodexLoreEntry commonLore = new CodexLoreEntry
        {
            LoreId = new LoreId("lore_018"),
            Title = "Common",
            Content = "Content",
            Category = "History",
            Tier = LoreTier.Common,
            DateDiscovered = _testDate
        };

        CodexLoreEntry uncommonLore = new CodexLoreEntry
        {
            LoreId = new LoreId("lore_019"),
            Title = "Uncommon",
            Content = "Content",
            Category = "History",
            Tier = LoreTier.Uncommon,
            DateDiscovered = _testDate
        };

        CodexLoreEntry rareLore = new CodexLoreEntry
        {
            LoreId = new LoreId("lore_020"),
            Title = "Rare",
            Content = "Content",
            Category = "History",
            Tier = LoreTier.Rare,
            DateDiscovered = _testDate
        };

        CodexLoreEntry legendaryLore = new CodexLoreEntry
        {
            LoreId = new LoreId("lore_021"),
            Title = "Legendary",
            Content = "Content",
            Category = "History",
            Tier = LoreTier.Legendary,
            DateDiscovered = _testDate
        };

        // Act & Assert
        Assert.That(commonLore.MatchesTier(LoreTier.Common), Is.True);
        Assert.That(uncommonLore.MatchesTier(LoreTier.Uncommon), Is.True);
        Assert.That(rareLore.MatchesTier(LoreTier.Rare), Is.True);
        Assert.That(legendaryLore.MatchesTier(LoreTier.Legendary), Is.True);
    }

    #endregion

    #region MatchesCategory Tests

    [Test]
    public void MatchesCategory_WithExactMatch_ReturnsTrue()
    {
        // Arrange
        CodexLoreEntry lore = new CodexLoreEntry
        {
            LoreId = new LoreId("lore_022"),
            Title = "Test",
            Content = "Test content",
            Category = "History",
            Tier = LoreTier.Common,
            DateDiscovered = _testDate
        };

        // Act & Assert
        Assert.That(lore.MatchesCategory("History"), Is.True);
    }

    [Test]
    public void MatchesCategory_IsCaseInsensitive()
    {
        // Arrange
        CodexLoreEntry lore = new CodexLoreEntry
        {
            LoreId = new LoreId("lore_023"),
            Title = "Test",
            Content = "Test content",
            Category = "Geography",
            Tier = LoreTier.Common,
            DateDiscovered = _testDate
        };

        // Act & Assert
        Assert.That(lore.MatchesCategory("geography"), Is.True);
        Assert.That(lore.MatchesCategory("GEOGRAPHY"), Is.True);
        Assert.That(lore.MatchesCategory("Geography"), Is.True);
        Assert.That(lore.MatchesCategory("gEoGrApHy"), Is.True);
    }

    [Test]
    public void MatchesCategory_WithDifferentCategory_ReturnsFalse()
    {
        // Arrange
        CodexLoreEntry lore = new CodexLoreEntry
        {
            LoreId = new LoreId("lore_024"),
            Title = "Test",
            Content = "Test content",
            Category = "History",
            Tier = LoreTier.Common,
            DateDiscovered = _testDate
        };

        // Act & Assert
        Assert.That(lore.MatchesCategory("Geography"), Is.False);
        Assert.That(lore.MatchesCategory("Religion"), Is.False);
    }

    [Test]
    public void MatchesCategory_WithEmptyString_ReturnsFalse()
    {
        // Arrange
        CodexLoreEntry lore = new CodexLoreEntry
        {
            LoreId = new LoreId("lore_025"),
            Title = "Test",
            Content = "Test content",
            Category = "History",
            Tier = LoreTier.Common,
            DateDiscovered = _testDate
        };

        // Act & Assert
        Assert.That(lore.MatchesCategory(""), Is.False);
        Assert.That(lore.MatchesCategory("   "), Is.False);
    }

    [Test]
    public void MatchesCategory_WithNullString_ReturnsFalse()
    {
        // Arrange
        CodexLoreEntry lore = new CodexLoreEntry
        {
            LoreId = new LoreId("lore_026"),
            Title = "Test",
            Content = "Test content",
            Category = "History",
            Tier = LoreTier.Common,
            DateDiscovered = _testDate
        };

        // Act & Assert
        Assert.That(lore.MatchesCategory(null!), Is.False);
    }

    [Test]
    public void MatchesCategory_WithPartialMatch_ReturnsFalse()
    {
        // Arrange
        CodexLoreEntry lore = new CodexLoreEntry
        {
            LoreId = new LoreId("lore_027"),
            Title = "Test",
            Content = "Test content",
            Category = "History",
            Tier = LoreTier.Common,
            DateDiscovered = _testDate
        };

        // Act & Assert
        Assert.That(lore.MatchesCategory("Hist"), Is.False);
        Assert.That(lore.MatchesCategory("story"), Is.False);
    }

    #endregion

    #region Edge Cases Tests

    [Test]
    public void Constructor_WithEmptyOptionalProperties_WorksCorrectly()
    {
        // Arrange & Act
        CodexLoreEntry lore = new CodexLoreEntry
        {
            LoreId = new LoreId("lore_028"),
            Title = "Test",
            Content = "Test content",
            Category = "History",
            Tier = LoreTier.Common,
            DateDiscovered = _testDate,
            DiscoveryLocation = null,
            DiscoverySource = null,
            Keywords = new List<Keyword>()
        };

        // Assert
        Assert.That(lore.DiscoveryLocation, Is.Null);
        Assert.That(lore.DiscoverySource, Is.Null);
        Assert.That(lore.Keywords, Is.Empty);
    }

    [Test]
    public void MatchesSearch_WithEmptyKeywordsList_DoesNotMatchKeywords()
    {
        // Arrange
        CodexLoreEntry lore = new CodexLoreEntry
        {
            LoreId = new LoreId("lore_029"),
            Title = "Test",
            Content = "Test content",
            Category = "History",
            Tier = LoreTier.Common,
            DateDiscovered = _testDate,
            Keywords = new List<Keyword>()
        };

        // Act & Assert
        Assert.That(lore.MatchesSearch("keyword"), Is.False);
    }

    [Test]
    public void MatchesSearch_WithMultipleMatches_ReturnsTrue()
    {
        // Arrange
        CodexLoreEntry lore = new CodexLoreEntry
        {
            LoreId = new LoreId("lore_030"),
            Title = "Dragon",
            Content = "Dragon lore",
            Category = "Dragon History",
            Tier = LoreTier.Common,
            DateDiscovered = _testDate,
            DiscoveryLocation = "Dragon Cave",
            DiscoverySource = "Dragon Scroll",
            Keywords = new List<Keyword> { new Keyword("dragon") }
        };

        // Act & Assert - Should match in multiple places
        Assert.That(lore.MatchesSearch("dragon"), Is.True);
    }

    #endregion
}
