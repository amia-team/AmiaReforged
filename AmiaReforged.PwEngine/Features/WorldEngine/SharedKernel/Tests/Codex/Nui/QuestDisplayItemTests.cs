using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Enums;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Nui.Player;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Tests.Codex.Nui;

[TestFixture]
public class QuestDisplayItemTests
{
    private DateTime _testDate;

    [SetUp]
    public void SetUp()
    {
        _testDate = new DateTime(2025, 10, 22, 12, 0, 0);
    }

    /// <summary>
    /// Regression: codex was showing "In Progress" when the current stage defined
    /// QuestState = Completed, because Subtitle read entry.State instead of EffectiveState.
    /// </summary>
    [Test]
    public void Subtitle_StageOverridesQuestState_ShowsEffectiveState()
    {
        // Arrange — entry State is InProgress, but stage 30 has QuestState = Completed
        CodexQuestEntry entry = new()
        {
            QuestId = new QuestId("farmers_favor"),
            Title = "A Farmer's Favor",
            Description = "Help the farmer.",
            DateStarted = _testDate,
            Stages =
            [
                new QuestStage { StageId = 10, QuestState = QuestState.InProgress },
                new QuestStage { StageId = 30, QuestState = QuestState.Completed }
            ]
        };
        entry.State = QuestState.InProgress;
        entry.CurrentStageId = 30;

        QuestDisplayItem display = new(entry);

        // Act
        string subtitle = display.Subtitle;

        // Assert — should show "Completed", not "In Progress"
        Assert.That(subtitle, Is.EqualTo("Completed"));
    }

    [Test]
    public void Subtitle_NoStageOverride_ShowsEntryState()
    {
        // Arrange — stages have no QuestState, entry is InProgress
        CodexQuestEntry entry = new()
        {
            QuestId = new QuestId("plain_quest"),
            Title = "Plain Quest",
            Description = "A plain quest.",
            DateStarted = _testDate,
            Stages =
            [
                new QuestStage { StageId = 10, QuestState = null }
            ]
        };
        entry.State = QuestState.InProgress;
        entry.CurrentStageId = 10;

        QuestDisplayItem display = new(entry);

        // Assert
        Assert.That(display.Subtitle, Is.EqualTo("In Progress"));
    }

    [Test]
    public void Subtitle_AfterMarkCompleted_ShowsCompleted()
    {
        // Arrange — simulate the full flow: stage 30 defines Completed,
        // and MarkCompleted has been called (as ApplyStageQuestState does)
        CodexQuestEntry entry = new()
        {
            QuestId = new QuestId("marked_complete"),
            Title = "Marked Complete",
            Description = "Test",
            DateStarted = _testDate,
            Stages =
            [
                new QuestStage { StageId = 10, QuestState = QuestState.InProgress },
                new QuestStage { StageId = 30, QuestState = QuestState.Completed }
            ]
        };
        entry.State = QuestState.InProgress;
        entry.CurrentStageId = 30;
        entry.MarkCompleted(_testDate.AddHours(1));

        QuestDisplayItem display = new(entry);

        // Assert — both State and EffectiveState agree: Completed
        Assert.That(display.Subtitle, Is.EqualTo("Completed"));
    }

    [Test]
    public void Subtitle_StageFailed_ShowsFailed()
    {
        // Arrange
        CodexQuestEntry entry = new()
        {
            QuestId = new QuestId("failed_quest"),
            Title = "Failed Quest",
            Description = "Test",
            DateStarted = _testDate,
            Stages =
            [
                new QuestStage { StageId = 10, QuestState = QuestState.InProgress },
                new QuestStage { StageId = 20, QuestState = QuestState.Failed }
            ]
        };
        entry.State = QuestState.InProgress;
        entry.CurrentStageId = 20;
        QuestDisplayItem display = new(entry);

        // Assert — EffectiveState from stage overrides entry State
        Assert.That(display.Subtitle, Is.EqualTo("Failed"));
    }
}
