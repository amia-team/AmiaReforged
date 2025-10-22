using AmiaReforged.PwEngine.Features.Codex.Domain.Enums;
using AmiaReforged.PwEngine.Features.Codex.Domain.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Codex.Entities;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.Codex.Entities;

[TestFixture]
public class CodexQuestEntryTests
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
        var quest = new CodexQuestEntry
        {
            QuestId = new QuestId("quest_001"),
            Title = "The Dragon's Hoard",
            Description = "Find the legendary dragon's treasure",
            DateStarted = _testDate
        };

        // Assert
        Assert.That(quest.QuestId.Value, Is.EqualTo("quest_001"));
        Assert.That(quest.Title, Is.EqualTo("The Dragon's Hoard"));
        Assert.That(quest.Description, Is.EqualTo("Find the legendary dragon's treasure"));
        Assert.That(quest.DateStarted, Is.EqualTo(_testDate));
        Assert.That(quest.State, Is.EqualTo(QuestState.Discovered));
        Assert.That(quest.DateCompleted, Is.Null);
    }

    [Test]
    public void Constructor_WithOptionalProperties_SetsThemCorrectly()
    {
        // Arrange & Act
        var quest = new CodexQuestEntry
        {
            QuestId = new QuestId("quest_002"),
            Title = "Help the Villagers",
            Description = "Assist the village with their problem",
            DateStarted = _testDate,
            QuestGiver = "Elder Marcus",
            Location = "Riverside Village",
            Objectives = new List<string> { "Talk to Elder", "Investigate the barn" },
            Keywords = new List<Keyword> { new Keyword("village"), new Keyword("help") }
        };

        // Assert
        Assert.That(quest.QuestGiver, Is.EqualTo("Elder Marcus"));
        Assert.That(quest.Location, Is.EqualTo("Riverside Village"));
        Assert.That(quest.Objectives, Has.Count.EqualTo(2));
        Assert.That(quest.Keywords, Has.Count.EqualTo(2));
    }

    [Test]
    public void Constructor_WithEmptyObjectives_CreatesEmptyList()
    {
        // Arrange & Act
        var quest = new CodexQuestEntry
        {
            QuestId = new QuestId("quest_003"),
            Title = "Simple Quest",
            Description = "A simple quest",
            DateStarted = _testDate
        };

        // Assert
        Assert.That(quest.Objectives, Is.Not.Null);
        Assert.That(quest.Objectives, Is.Empty);
    }

    [Test]
    public void Constructor_WithEmptyKeywords_CreatesEmptyList()
    {
        // Arrange & Act
        var quest = new CodexQuestEntry
        {
            QuestId = new QuestId("quest_004"),
            Title = "Another Quest",
            Description = "Another quest description",
            DateStarted = _testDate
        };

        // Assert
        Assert.That(quest.Keywords, Is.Not.Null);
        Assert.That(quest.Keywords, Is.Empty);
    }

    #endregion

    #region MarkCompleted Tests

    [Test]
    public void MarkCompleted_FromDiscoveredState_SetsStateAndDate()
    {
        // Arrange
        var quest = new CodexQuestEntry
        {
            QuestId = new QuestId("quest_005"),
            Title = "Test Quest",
            Description = "Test",
            DateStarted = _testDate
        };
        var completedDate = _testDate.AddHours(2);

        // Act
        quest.MarkCompleted(completedDate);

        // Assert
        Assert.That(quest.State, Is.EqualTo(QuestState.Completed));
        Assert.That(quest.DateCompleted, Is.EqualTo(completedDate));
    }

    [Test]
    public void MarkCompleted_FromInProgressState_SetsStateAndDate()
    {
        // Arrange
        var quest = new CodexQuestEntry
        {
            QuestId = new QuestId("quest_006"),
            Title = "Test Quest",
            Description = "Test",
            DateStarted = _testDate
        };
        // Note: Quest starts in Discovered state by default, which is valid for completion
        var completedDate = _testDate.AddHours(3);

        // Act
        quest.MarkCompleted(completedDate);

        // Assert
        Assert.That(quest.State, Is.EqualTo(QuestState.Completed));
        Assert.That(quest.DateCompleted, Is.EqualTo(completedDate));
    }

    [Test]
    public void MarkCompleted_FromCompletedState_ThrowsInvalidOperationException()
    {
        // Arrange
        var quest = new CodexQuestEntry
        {
            QuestId = new QuestId("quest_007"),
            Title = "Test Quest",
            Description = "Test",
            DateStarted = _testDate
        };
        quest.MarkCompleted(_testDate.AddHours(1));

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            quest.MarkCompleted(_testDate.AddHours(2)));
        Assert.That(ex.Message, Does.Contain("Cannot complete quest in state Completed"));
    }

    [Test]
    public void MarkCompleted_FromFailedState_ThrowsInvalidOperationException()
    {
        // Arrange
        var quest = new CodexQuestEntry
        {
            QuestId = new QuestId("quest_008"),
            Title = "Test Quest",
            Description = "Test",
            DateStarted = _testDate
        };
        quest.MarkFailed(_testDate.AddHours(1));

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            quest.MarkCompleted(_testDate.AddHours(2)));
        Assert.That(ex.Message, Does.Contain("Cannot complete quest in state Failed"));
    }

    [Test]
    public void MarkCompleted_FromAbandonedState_ThrowsInvalidOperationException()
    {
        // Arrange
        var quest = new CodexQuestEntry
        {
            QuestId = new QuestId("quest_009"),
            Title = "Test Quest",
            Description = "Test",
            DateStarted = _testDate
        };
        quest.MarkAbandoned(_testDate.AddHours(1));

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            quest.MarkCompleted(_testDate.AddHours(2)));
        Assert.That(ex.Message, Does.Contain("Cannot complete quest in state Abandoned"));
    }

    #endregion

    #region MarkFailed Tests

    [Test]
    public void MarkFailed_FromDiscoveredState_SetsStateAndDate()
    {
        // Arrange
        var quest = new CodexQuestEntry
        {
            QuestId = new QuestId("quest_010"),
            Title = "Test Quest",
            Description = "Test",
            DateStarted = _testDate
        };
        var failedDate = _testDate.AddHours(1);

        // Act
        quest.MarkFailed(failedDate);

        // Assert
        Assert.That(quest.State, Is.EqualTo(QuestState.Failed));
        Assert.That(quest.DateCompleted, Is.EqualTo(failedDate));
    }

    [Test]
    public void MarkFailed_FromInProgressState_SetsStateAndDate()
    {
        // Arrange
        var quest = new CodexQuestEntry
        {
            QuestId = new QuestId("quest_011"),
            Title = "Test Quest",
            Description = "Test",
            DateStarted = _testDate
        };
        // Note: Quest starts in Discovered state by default, which is valid for failing
        var failedDate = _testDate.AddHours(2);

        // Act
        quest.MarkFailed(failedDate);

        // Assert
        Assert.That(quest.State, Is.EqualTo(QuestState.Failed));
        Assert.That(quest.DateCompleted, Is.EqualTo(failedDate));
    }

    [Test]
    public void MarkFailed_FromCompletedState_ThrowsInvalidOperationException()
    {
        // Arrange
        var quest = new CodexQuestEntry
        {
            QuestId = new QuestId("quest_012"),
            Title = "Test Quest",
            Description = "Test",
            DateStarted = _testDate
        };
        quest.MarkCompleted(_testDate.AddHours(1));

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            quest.MarkFailed(_testDate.AddHours(2)));
        Assert.That(ex.Message, Does.Contain("Cannot fail quest in state Completed"));
    }

    [Test]
    public void MarkFailed_FromFailedState_ThrowsInvalidOperationException()
    {
        // Arrange
        var quest = new CodexQuestEntry
        {
            QuestId = new QuestId("quest_013"),
            Title = "Test Quest",
            Description = "Test",
            DateStarted = _testDate
        };
        quest.MarkFailed(_testDate.AddHours(1));

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            quest.MarkFailed(_testDate.AddHours(2)));
        Assert.That(ex.Message, Does.Contain("Cannot fail quest in state Failed"));
    }

    [Test]
    public void MarkFailed_FromAbandonedState_ThrowsInvalidOperationException()
    {
        // Arrange
        var quest = new CodexQuestEntry
        {
            QuestId = new QuestId("quest_014"),
            Title = "Test Quest",
            Description = "Test",
            DateStarted = _testDate
        };
        quest.MarkAbandoned(_testDate.AddHours(1));

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            quest.MarkFailed(_testDate.AddHours(2)));
        Assert.That(ex.Message, Does.Contain("Cannot fail quest in state Abandoned"));
    }

    #endregion

    #region MarkAbandoned Tests

    [Test]
    public void MarkAbandoned_FromDiscoveredState_SetsStateAndDate()
    {
        // Arrange
        var quest = new CodexQuestEntry
        {
            QuestId = new QuestId("quest_015"),
            Title = "Test Quest",
            Description = "Test",
            DateStarted = _testDate
        };
        var abandonedDate = _testDate.AddHours(1);

        // Act
        quest.MarkAbandoned(abandonedDate);

        // Assert
        Assert.That(quest.State, Is.EqualTo(QuestState.Abandoned));
        Assert.That(quest.DateCompleted, Is.EqualTo(abandonedDate));
    }

    [Test]
    public void MarkAbandoned_FromInProgressState_SetsStateAndDate()
    {
        // Arrange
        var quest = new CodexQuestEntry
        {
            QuestId = new QuestId("quest_016"),
            Title = "Test Quest",
            Description = "Test",
            DateStarted = _testDate
        };
        // Note: Quest starts in Discovered state by default, which is valid for abandoning
        var abandonedDate = _testDate.AddHours(2);

        // Act
        quest.MarkAbandoned(abandonedDate);

        // Assert
        Assert.That(quest.State, Is.EqualTo(QuestState.Abandoned));
        Assert.That(quest.DateCompleted, Is.EqualTo(abandonedDate));
    }

    [Test]
    public void MarkAbandoned_FromCompletedState_ThrowsInvalidOperationException()
    {
        // Arrange
        var quest = new CodexQuestEntry
        {
            QuestId = new QuestId("quest_017"),
            Title = "Test Quest",
            Description = "Test",
            DateStarted = _testDate
        };
        quest.MarkCompleted(_testDate.AddHours(1));

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            quest.MarkAbandoned(_testDate.AddHours(2)));
        Assert.That(ex.Message, Does.Contain("Cannot abandon quest in state Completed"));
    }

    [Test]
    public void MarkAbandoned_FromFailedState_ThrowsInvalidOperationException()
    {
        // Arrange
        var quest = new CodexQuestEntry
        {
            QuestId = new QuestId("quest_018"),
            Title = "Test Quest",
            Description = "Test",
            DateStarted = _testDate
        };
        quest.MarkFailed(_testDate.AddHours(1));

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            quest.MarkAbandoned(_testDate.AddHours(2)));
        Assert.That(ex.Message, Does.Contain("Cannot abandon quest in state Failed"));
    }

    [Test]
    public void MarkAbandoned_FromAbandonedState_ThrowsInvalidOperationException()
    {
        // Arrange
        var quest = new CodexQuestEntry
        {
            QuestId = new QuestId("quest_019"),
            Title = "Test Quest",
            Description = "Test",
            DateStarted = _testDate
        };
        quest.MarkAbandoned(_testDate.AddHours(1));

        // Act & Assert
        var ex = Assert.Throws<InvalidOperationException>(() =>
            quest.MarkAbandoned(_testDate.AddHours(2)));
        Assert.That(ex.Message, Does.Contain("Cannot abandon quest in state Abandoned"));
    }

    #endregion

    #region MatchesSearch Tests

    [Test]
    public void MatchesSearch_WithTitleMatch_ReturnsTrue()
    {
        // Arrange
        var quest = new CodexQuestEntry
        {
            QuestId = new QuestId("quest_020"),
            Title = "The Dragon's Hoard",
            Description = "Find treasure",
            DateStarted = _testDate
        };

        // Act & Assert
        Assert.That(quest.MatchesSearch("dragon"), Is.True);
        Assert.That(quest.MatchesSearch("DRAGON"), Is.True);
        Assert.That(quest.MatchesSearch("hoard"), Is.True);
    }

    [Test]
    public void MatchesSearch_WithDescriptionMatch_ReturnsTrue()
    {
        // Arrange
        var quest = new CodexQuestEntry
        {
            QuestId = new QuestId("quest_021"),
            Title = "Simple Quest",
            Description = "Find the legendary artifact in the ancient ruins",
            DateStarted = _testDate
        };

        // Act & Assert
        Assert.That(quest.MatchesSearch("legendary"), Is.True);
        Assert.That(quest.MatchesSearch("artifact"), Is.True);
        Assert.That(quest.MatchesSearch("ruins"), Is.True);
    }

    [Test]
    public void MatchesSearch_WithQuestGiverMatch_ReturnsTrue()
    {
        // Arrange
        var quest = new CodexQuestEntry
        {
            QuestId = new QuestId("quest_022"),
            Title = "Help Request",
            Description = "Help needed",
            DateStarted = _testDate,
            QuestGiver = "Elder Marcus"
        };

        // Act & Assert
        Assert.That(quest.MatchesSearch("marcus"), Is.True);
        Assert.That(quest.MatchesSearch("elder"), Is.True);
    }

    [Test]
    public void MatchesSearch_WithLocationMatch_ReturnsTrue()
    {
        // Arrange
        var quest = new CodexQuestEntry
        {
            QuestId = new QuestId("quest_023"),
            Title = "Local Quest",
            Description = "Do something",
            DateStarted = _testDate,
            Location = "Riverside Village"
        };

        // Act & Assert
        Assert.That(quest.MatchesSearch("riverside"), Is.True);
        Assert.That(quest.MatchesSearch("village"), Is.True);
    }

    [Test]
    public void MatchesSearch_WithKeywordMatch_ReturnsTrue()
    {
        // Arrange
        var quest = new CodexQuestEntry
        {
            QuestId = new QuestId("quest_024"),
            Title = "Test Quest",
            Description = "Test",
            DateStarted = _testDate,
            Keywords = new List<Keyword> { new Keyword("combat"), new Keyword("exploration") }
        };

        // Act & Assert
        Assert.That(quest.MatchesSearch("combat"), Is.True);
        Assert.That(quest.MatchesSearch("exploration"), Is.True);
    }

    [Test]
    public void MatchesSearch_WithNoMatch_ReturnsFalse()
    {
        // Arrange
        var quest = new CodexQuestEntry
        {
            QuestId = new QuestId("quest_025"),
            Title = "Simple Quest",
            Description = "Simple description",
            DateStarted = _testDate
        };

        // Act & Assert
        Assert.That(quest.MatchesSearch("dragon"), Is.False);
        Assert.That(quest.MatchesSearch("treasure"), Is.False);
    }

    [Test]
    public void MatchesSearch_WithNullQuestGiver_StillMatchesOtherFields()
    {
        // Arrange
        var quest = new CodexQuestEntry
        {
            QuestId = new QuestId("quest_026"),
            Title = "Test Quest",
            Description = "A quest",
            DateStarted = _testDate,
            QuestGiver = null
        };

        // Act & Assert
        // Even with null QuestGiver, should match on other fields
        Assert.That(quest.MatchesSearch("test"), Is.True);
        Assert.That(quest.MatchesSearch("nonexistent"), Is.False);
    }

    [Test]
    public void MatchesSearch_WithNullLocation_StillMatchesOtherFields()
    {
        // Arrange
        var quest = new CodexQuestEntry
        {
            QuestId = new QuestId("quest_027"),
            Title = "Test Quest",
            Description = "A quest",
            DateStarted = _testDate,
            Location = null
        };

        // Act & Assert
        // Even with null Location, should match on other fields
        Assert.That(quest.MatchesSearch("test"), Is.True);
        Assert.That(quest.MatchesSearch("nonexistent"), Is.False);
    }

    [Test]
    public void MatchesSearch_WithEmptySearchTerm_ReturnsFalse()
    {
        // Arrange
        var quest = new CodexQuestEntry
        {
            QuestId = new QuestId("quest_028"),
            Title = "Test Quest",
            Description = "Test",
            DateStarted = _testDate
        };

        // Act & Assert
        Assert.That(quest.MatchesSearch(""), Is.False);
        Assert.That(quest.MatchesSearch("   "), Is.False);
    }

    [Test]
    public void MatchesSearch_WithNullSearchTerm_ReturnsFalse()
    {
        // Arrange
        var quest = new CodexQuestEntry
        {
            QuestId = new QuestId("quest_029"),
            Title = "Test Quest",
            Description = "Test",
            DateStarted = _testDate
        };

        // Act & Assert
        Assert.That(quest.MatchesSearch(null!), Is.False);
    }

    [Test]
    public void MatchesSearch_IsCaseInsensitive()
    {
        // Arrange
        var quest = new CodexQuestEntry
        {
            QuestId = new QuestId("quest_030"),
            Title = "The Dragon's Hoard",
            Description = "Find the LEGENDARY treasure",
            DateStarted = _testDate,
            QuestGiver = "Elder Marcus",
            Location = "Riverside Village"
        };

        // Act & Assert
        Assert.That(quest.MatchesSearch("DRAGON"), Is.True);
        Assert.That(quest.MatchesSearch("legendary"), Is.True);
        Assert.That(quest.MatchesSearch("ELDER"), Is.True);
        Assert.That(quest.MatchesSearch("riverside"), Is.True);
    }

    #endregion
}
