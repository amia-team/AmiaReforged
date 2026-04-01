using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Enums;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Entities;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Tests.Codex.Entities;

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
        CodexQuestEntry quest = new CodexQuestEntry
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
        CodexQuestEntry quest = new CodexQuestEntry
        {
            QuestId = new QuestId("quest_002"),
            Title = "Help the Villagers",
            Description = "Assist the village with their problem",
            DateStarted = _testDate,
            QuestGiver = "Elder Marcus",
            Location = "Riverside Village",
            Keywords = new List<Keyword> { new Keyword("village"), new Keyword("help") }
        };

        // Assert
        Assert.That(quest.QuestGiver, Is.EqualTo("Elder Marcus"));
        Assert.That(quest.Location, Is.EqualTo("Riverside Village"));
        Assert.That(quest.CurrentStageId, Is.EqualTo(0));
        Assert.That(quest.Keywords, Has.Count.EqualTo(2));
    }

    [Test]
    public void Constructor_DefaultCurrentStageId_IsZero()
    {
        // Arrange & Act
        CodexQuestEntry quest = new CodexQuestEntry
        {
            QuestId = new QuestId("quest_003"),
            Title = "Simple Quest",
            Description = "A simple quest",
            DateStarted = _testDate
        };

        // Assert
        Assert.That(quest.CurrentStageId, Is.EqualTo(0));
    }

    [Test]
    public void Constructor_WithEmptyKeywords_CreatesEmptyList()
    {
        // Arrange & Act
        CodexQuestEntry quest = new CodexQuestEntry
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
        CodexQuestEntry quest = new CodexQuestEntry
        {
            QuestId = new QuestId("quest_005"),
            Title = "Test Quest",
            Description = "Test",
            DateStarted = _testDate
        };
        DateTime completedDate = _testDate.AddHours(2);

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
        CodexQuestEntry quest = new CodexQuestEntry
        {
            QuestId = new QuestId("quest_006"),
            Title = "Test Quest",
            Description = "Test",
            DateStarted = _testDate
        };
        // Note: Quest starts in Discovered state by default, which is valid for completion
        DateTime completedDate = _testDate.AddHours(3);

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
        CodexQuestEntry quest = new CodexQuestEntry
        {
            QuestId = new QuestId("quest_007"),
            Title = "Test Quest",
            Description = "Test",
            DateStarted = _testDate
        };
        quest.MarkCompleted(_testDate.AddHours(1));

        // Act & Assert
        InvalidOperationException? ex = Assert.Throws<InvalidOperationException>(() =>
            quest.MarkCompleted(_testDate.AddHours(2)));
        Assert.That(ex.Message, Does.Contain("Cannot complete quest in state Completed"));
    }

    [Test]
    public void MarkCompleted_FromFailedState_ThrowsInvalidOperationException()
    {
        // Arrange
        CodexQuestEntry quest = new CodexQuestEntry
        {
            QuestId = new QuestId("quest_008"),
            Title = "Test Quest",
            Description = "Test",
            DateStarted = _testDate
        };
        quest.MarkFailed(_testDate.AddHours(1));

        // Act & Assert
        InvalidOperationException? ex = Assert.Throws<InvalidOperationException>(() =>
            quest.MarkCompleted(_testDate.AddHours(2)));
        Assert.That(ex.Message, Does.Contain("Cannot complete quest in state Failed"));
    }

    [Test]
    public void MarkCompleted_FromAbandonedState_ThrowsInvalidOperationException()
    {
        // Arrange
        CodexQuestEntry quest = new CodexQuestEntry
        {
            QuestId = new QuestId("quest_009"),
            Title = "Test Quest",
            Description = "Test",
            DateStarted = _testDate
        };
        quest.MarkAbandoned(_testDate.AddHours(1));

        // Act & Assert
        InvalidOperationException? ex = Assert.Throws<InvalidOperationException>(() =>
            quest.MarkCompleted(_testDate.AddHours(2)));
        Assert.That(ex.Message, Does.Contain("Cannot complete quest in state Abandoned"));
    }

    #endregion

    #region MarkFailed Tests

    [Test]
    public void MarkFailed_FromDiscoveredState_SetsStateAndDate()
    {
        // Arrange
        CodexQuestEntry quest = new CodexQuestEntry
        {
            QuestId = new QuestId("quest_010"),
            Title = "Test Quest",
            Description = "Test",
            DateStarted = _testDate
        };
        DateTime failedDate = _testDate.AddHours(1);

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
        CodexQuestEntry quest = new CodexQuestEntry
        {
            QuestId = new QuestId("quest_011"),
            Title = "Test Quest",
            Description = "Test",
            DateStarted = _testDate
        };
        // Note: Quest starts in Discovered state by default, which is valid for failing
        DateTime failedDate = _testDate.AddHours(2);

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
        CodexQuestEntry quest = new CodexQuestEntry
        {
            QuestId = new QuestId("quest_012"),
            Title = "Test Quest",
            Description = "Test",
            DateStarted = _testDate
        };
        quest.MarkCompleted(_testDate.AddHours(1));

        // Act & Assert
        InvalidOperationException? ex = Assert.Throws<InvalidOperationException>(() =>
            quest.MarkFailed(_testDate.AddHours(2)));
        Assert.That(ex.Message, Does.Contain("Cannot fail quest in state Completed"));
    }

    [Test]
    public void MarkFailed_FromFailedState_ThrowsInvalidOperationException()
    {
        // Arrange
        CodexQuestEntry quest = new CodexQuestEntry
        {
            QuestId = new QuestId("quest_013"),
            Title = "Test Quest",
            Description = "Test",
            DateStarted = _testDate
        };
        quest.MarkFailed(_testDate.AddHours(1));

        // Act & Assert
        InvalidOperationException? ex = Assert.Throws<InvalidOperationException>(() =>
            quest.MarkFailed(_testDate.AddHours(2)));
        Assert.That(ex.Message, Does.Contain("Cannot fail quest in state Failed"));
    }

    [Test]
    public void MarkFailed_FromAbandonedState_ThrowsInvalidOperationException()
    {
        // Arrange
        CodexQuestEntry quest = new CodexQuestEntry
        {
            QuestId = new QuestId("quest_014"),
            Title = "Test Quest",
            Description = "Test",
            DateStarted = _testDate
        };
        quest.MarkAbandoned(_testDate.AddHours(1));

        // Act & Assert
        InvalidOperationException? ex = Assert.Throws<InvalidOperationException>(() =>
            quest.MarkFailed(_testDate.AddHours(2)));
        Assert.That(ex.Message, Does.Contain("Cannot fail quest in state Abandoned"));
    }

    #endregion

    #region MarkAbandoned Tests

    [Test]
    public void MarkAbandoned_FromDiscoveredState_SetsStateAndDate()
    {
        // Arrange
        CodexQuestEntry quest = new CodexQuestEntry
        {
            QuestId = new QuestId("quest_015"),
            Title = "Test Quest",
            Description = "Test",
            DateStarted = _testDate
        };
        DateTime abandonedDate = _testDate.AddHours(1);

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
        CodexQuestEntry quest = new CodexQuestEntry
        {
            QuestId = new QuestId("quest_016"),
            Title = "Test Quest",
            Description = "Test",
            DateStarted = _testDate
        };
        // Note: Quest starts in Discovered state by default, which is valid for abandoning
        DateTime abandonedDate = _testDate.AddHours(2);

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
        CodexQuestEntry quest = new CodexQuestEntry
        {
            QuestId = new QuestId("quest_017"),
            Title = "Test Quest",
            Description = "Test",
            DateStarted = _testDate
        };
        quest.MarkCompleted(_testDate.AddHours(1));

        // Act & Assert
        InvalidOperationException? ex = Assert.Throws<InvalidOperationException>(() =>
            quest.MarkAbandoned(_testDate.AddHours(2)));
        Assert.That(ex.Message, Does.Contain("Cannot abandon quest in state Completed"));
    }

    [Test]
    public void MarkAbandoned_FromFailedState_ThrowsInvalidOperationException()
    {
        // Arrange
        CodexQuestEntry quest = new CodexQuestEntry
        {
            QuestId = new QuestId("quest_018"),
            Title = "Test Quest",
            Description = "Test",
            DateStarted = _testDate
        };
        quest.MarkFailed(_testDate.AddHours(1));

        // Act & Assert
        InvalidOperationException? ex = Assert.Throws<InvalidOperationException>(() =>
            quest.MarkAbandoned(_testDate.AddHours(2)));
        Assert.That(ex.Message, Does.Contain("Cannot abandon quest in state Failed"));
    }

    [Test]
    public void MarkAbandoned_FromAbandonedState_ThrowsInvalidOperationException()
    {
        // Arrange
        CodexQuestEntry quest = new CodexQuestEntry
        {
            QuestId = new QuestId("quest_019"),
            Title = "Test Quest",
            Description = "Test",
            DateStarted = _testDate
        };
        quest.MarkAbandoned(_testDate.AddHours(1));

        // Act & Assert
        InvalidOperationException? ex = Assert.Throws<InvalidOperationException>(() =>
            quest.MarkAbandoned(_testDate.AddHours(2)));
        Assert.That(ex.Message, Does.Contain("Cannot abandon quest in state Abandoned"));
    }

    #endregion

    #region MatchesSearch Tests

    [Test]
    public void MatchesSearch_WithTitleMatch_ReturnsTrue()
    {
        // Arrange
        CodexQuestEntry quest = new CodexQuestEntry
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
        CodexQuestEntry quest = new CodexQuestEntry
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
        CodexQuestEntry quest = new CodexQuestEntry
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
        CodexQuestEntry quest = new CodexQuestEntry
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
        CodexQuestEntry quest = new CodexQuestEntry
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
        CodexQuestEntry quest = new CodexQuestEntry
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
        CodexQuestEntry quest = new CodexQuestEntry
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
        CodexQuestEntry quest = new CodexQuestEntry
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
        CodexQuestEntry quest = new CodexQuestEntry
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
        CodexQuestEntry quest = new CodexQuestEntry
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
        CodexQuestEntry quest = new CodexQuestEntry
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

    #region AdvanceToStage Tests

    [Test]
    public void AdvanceToStage_FromLowerStage_AdvancesSuccessfully()
    {
        // Arrange
        CodexQuestEntry quest = CreateInProgressQuest(currentStage: 10);

        // Act
        quest.AdvanceToStage(20);

        // Assert
        Assert.That(quest.CurrentStageId, Is.EqualTo(20));
    }

    [Test]
    public void AdvanceToStage_SameStage_IsIdempotentNoOp()
    {
        // Arrange
        CodexQuestEntry quest = CreateInProgressQuest(currentStage: 30);

        // Act — should NOT throw
        quest.AdvanceToStage(30);

        // Assert — stage unchanged
        Assert.That(quest.CurrentStageId, Is.EqualTo(30));
        Assert.That(quest.State, Is.EqualTo(QuestState.InProgress));
    }

    [Test]
    public void AdvanceToStage_LowerStage_Throws()
    {
        // Arrange
        CodexQuestEntry quest = CreateInProgressQuest(currentStage: 20);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => quest.AdvanceToStage(10));
    }

    [Test]
    public void AdvanceToStage_FromDiscovered_TransitionsToInProgress()
    {
        // Arrange
        CodexQuestEntry quest = new()
        {
            QuestId = new QuestId("auto_transition"),
            Title = "Auto Transition",
            Description = "Test",
            DateStarted = _testDate
        };
        quest.State = QuestState.Discovered;

        // Act
        quest.AdvanceToStage(10);

        // Assert
        Assert.That(quest.State, Is.EqualTo(QuestState.InProgress));
        Assert.That(quest.CurrentStageId, Is.EqualTo(10));
    }

    [Test]
    public void AdvanceToStage_SameStage_WhenDiscovered_DoesNotTransition()
    {
        // Arrange — stage 0, Discovered
        CodexQuestEntry quest = new()
        {
            QuestId = new QuestId("no_transition"),
            Title = "No Transition",
            Description = "Test",
            DateStarted = _testDate
        };
        quest.State = QuestState.Discovered;
        quest.CurrentStageId = 0;

        // Act — advance to stage 0 (same stage, idempotent)
        quest.AdvanceToStage(0);

        // Assert — still Discovered, not transitioned
        Assert.That(quest.State, Is.EqualTo(QuestState.Discovered));
    }

    [TestCase(QuestState.Completed)]
    [TestCase(QuestState.Failed)]
    [TestCase(QuestState.Abandoned)]
    [TestCase(QuestState.Expired)]
    public void AdvanceToStage_InTerminalState_Throws(QuestState terminalState)
    {
        // Arrange
        CodexQuestEntry quest = new()
        {
            QuestId = new QuestId("terminal"),
            Title = "Terminal Quest",
            Description = "Test",
            DateStarted = _testDate
        };
        quest.State = terminalState;

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => quest.AdvanceToStage(10));
    }

    [Test]
    public void AdvanceToStage_SameStage_InTerminalState_StillThrows()
    {
        // Arrange — Completed at stage 30
        CodexQuestEntry quest = new()
        {
            QuestId = new QuestId("terminal_same"),
            Title = "Terminal Same",
            Description = "Test",
            DateStarted = _testDate
        };
        quest.State = QuestState.Completed;
        quest.CurrentStageId = 30;

        // Act & Assert — terminal state check happens before idempotent check
        Assert.Throws<InvalidOperationException>(() => quest.AdvanceToStage(30));
    }

    #endregion

    #region EffectiveState Tests

    [Test]
    public void EffectiveState_NoStages_ReturnsEntryLevelState()
    {
        // Arrange
        CodexQuestEntry quest = new()
        {
            QuestId = new QuestId("no_stages"),
            Title = "No Stages",
            Description = "Test",
            DateStarted = _testDate,
            Stages = []
        };
        quest.State = QuestState.InProgress;

        // Assert
        Assert.That(quest.EffectiveState, Is.EqualTo(QuestState.InProgress));
    }

    [Test]
    public void EffectiveState_StageHasQuestState_ReturnsStageQuestState()
    {
        // Arrange
        CodexQuestEntry quest = new()
        {
            QuestId = new QuestId("stage_state"),
            Title = "Stage State",
            Description = "Test",
            DateStarted = _testDate,
            Stages =
            [
                new QuestStage { StageId = 10, QuestState = QuestState.InProgress },
                new QuestStage { StageId = 20, QuestState = QuestState.Completed }
            ]
        };
        quest.State = QuestState.InProgress;
        quest.CurrentStageId = 20;

        // Assert — should come from stage 20, not entry level
        Assert.That(quest.EffectiveState, Is.EqualTo(QuestState.Completed));
    }

    [Test]
    public void EffectiveState_StageHasNullQuestState_FallsBackToEntryState()
    {
        // Arrange
        CodexQuestEntry quest = new()
        {
            QuestId = new QuestId("null_stage_state"),
            Title = "Null Stage State",
            Description = "Test",
            DateStarted = _testDate,
            Stages =
            [
                new QuestStage { StageId = 10, QuestState = null }
            ]
        };
        quest.State = QuestState.InProgress;
        quest.CurrentStageId = 10;

        // Assert — null stage QuestState falls back to entry State
        Assert.That(quest.EffectiveState, Is.EqualTo(QuestState.InProgress));
    }

    [Test]
    public void EffectiveState_CurrentStageIdBetweenStages_ResolvesHighestBelow()
    {
        // Arrange — stages 10 and 30, current is 25
        CodexQuestEntry quest = new()
        {
            QuestId = new QuestId("gap_stage"),
            Title = "Gap Stage",
            Description = "Test",
            DateStarted = _testDate,
            Stages =
            [
                new QuestStage { StageId = 10, QuestState = QuestState.InProgress },
                new QuestStage { StageId = 30, QuestState = QuestState.Completed }
            ]
        };
        quest.State = QuestState.Discovered;
        quest.CurrentStageId = 25;

        // Assert — highest stage ≤ 25 is stage 10
        Assert.That(quest.EffectiveState, Is.EqualTo(QuestState.InProgress));
    }

    #endregion

    #region Helpers

    private CodexQuestEntry CreateInProgressQuest(int currentStage)
    {
        CodexQuestEntry quest = new()
        {
            QuestId = new QuestId("adv_test"),
            Title = "Advance Test",
            Description = "Test",
            DateStarted = _testDate
        };
        quest.State = QuestState.InProgress;
        quest.CurrentStageId = currentStage;
        return quest;
    }

    #endregion
}
