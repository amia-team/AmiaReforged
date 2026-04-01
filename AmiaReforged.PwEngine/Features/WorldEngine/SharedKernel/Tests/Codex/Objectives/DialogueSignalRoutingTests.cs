using System.Threading.Channels;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Aggregates;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Enums;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Objectives;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Objectives.Evaluators;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Tests.Codex.Application;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Tests.Codex.Objectives;

/// <summary>
/// End-to-end tests for dialogue signal routing through the quest session system.
/// Validates that a <c>DialogueNodeEnteredEvent</c> (simulated via the same signal path)
/// correctly completes dialog_choice objectives and triggers stage advancement.
/// </summary>
[TestFixture]
public class DialogueSignalRoutingTests
{
    private ObjectiveEvaluatorRegistry _registry = null!;
    private QuestSessionManager _sessionManager = null!;
    private Channel<CodexDomainEvent> _eventChannel = null!;
    private CharacterId _characterId;
    private QuestId _questId;
    private DateTime _testDate;

    [SetUp]
    public void SetUp()
    {
        _registry = new ObjectiveEvaluatorRegistry(new IObjectiveEvaluator[]
        {
            new DialogChoiceObjectiveEvaluator(),
            new CollectObjectiveEvaluator(),
            new KillObjectiveEvaluator()
        });

        _sessionManager = new QuestSessionManager(_registry);
        _eventChannel = Channel.CreateUnbounded<CodexDomainEvent>();
        _characterId = CharacterId.New();
        _questId = new QuestId("sp_goblins");
        _testDate = new DateTime(2026, 4, 1, 12, 0, 0);
    }

    #region Signal Routing

    [Test]
    public void DialogueNodeEntered_matching_node_completes_objective()
    {
        // Given a quest at stage 20 with a dialog_choice objective targeting a specific node
        Guid nodeGuid = Guid.Parse("33220e2f-31a8-4d66-8e35-9f92363281d6");
        DialogueNodeId nodeId = DialogueNodeId.From(nodeGuid);
        string shortId = nodeId.ToShortString(); // "33220e2f"

        QuestObjectiveGroup group = CreateDialogChoiceGroup(shortId);
        _sessionManager.CreateSession(_characterId, _questId, [group], _testDate);

        // When the dialogue node entered signal is routed (same as ProcessDialogueNodeEntered)
        QuestSignal signal = new(SignalType.DialogChoice, shortId);
        RouteSignal(signal);

        // Then the objective completes
        List<CodexDomainEvent> events = DrainChannel();
        Assert.That(events.Any(e => e is ObjectiveCompletedEvent), Is.True);
    }

    [Test]
    public void DialogueNodeEntered_non_matching_node_produces_no_events()
    {
        // Given a dialog_choice objective targeting "33220e2f"
        QuestObjectiveGroup group = CreateDialogChoiceGroup("33220e2f");
        _sessionManager.CreateSession(_characterId, _questId, [group], _testDate);

        // When a different node is entered
        QuestSignal signal = new(SignalType.DialogChoice, "aaaabbbb");
        RouteSignal(signal);

        // Then no events
        Assert.That(_eventChannel.Reader.TryRead(out _), Is.False);
    }

    [Test]
    public void DialogueNodeEntered_no_session_produces_no_events()
    {
        // Given no active sessions

        // When a dialogue signal arrives
        QuestSignal signal = new(SignalType.DialogChoice, "33220e2f");
        RouteSignal(signal);

        // Then no events
        Assert.That(_eventChannel.Reader.TryRead(out _), Is.False);
    }

    #endregion

    #region Stage Advancement via Dialogue

    [Test]
    public void DialogueNodeEntered_completing_objective_advances_to_next_stage()
    {
        // Given a quest with stage 20 (dialog_choice objective) and stage 30 (next stage)
        Guid nodeGuid = Guid.Parse("33220e2f-31a8-4d66-8e35-9f92363281d6");
        string shortId = new DialogueNodeId(nodeGuid).ToShortString();

        List<QuestStage> stages =
        [
            new QuestStage
            {
                StageId = 20,
                JournalText = "Speak to Gilbert about the goblin problem",
                ObjectiveGroups = [CreateDialogChoiceGroup(shortId)]
            },
            new QuestStage
            {
                StageId = 30,
                JournalText = "Gilbert told you about the goblin camp",
                ObjectiveGroups = []
            }
        ];

        StageContext ctx = new(stages, 20);
        _sessionManager.CreateSession(
            _characterId, _questId,
            stages[0].ObjectiveGroups, _testDate,
            stageContext: ctx);

        // When the dialogue node is entered
        QuestSignal signal = new(SignalType.DialogChoice, shortId);
        RouteSignal(signal);

        // Then we get stage advancement from 20 → 30
        List<CodexDomainEvent> events = DrainChannel();
        QuestStageAdvancedEvent? stageEvent = events.OfType<QuestStageAdvancedEvent>().FirstOrDefault();
        Assert.That(stageEvent, Is.Not.Null);
        Assert.That(stageEvent!.FromStage, Is.EqualTo(20));
        Assert.That(stageEvent.ToStage, Is.EqualTo(30));
    }

    [Test]
    public void DialogueNodeEntered_completing_objective_with_CompletionStageId_branches()
    {
        // Given a quest where the dialog group has CompletionStageId = 50 (branching)
        string shortId = "abcd1234";

        QuestObjectiveGroup branchingGroup = new()
        {
            DisplayName = "Speak to NPC",
            CompletionMode = CompletionMode.All,
            CompletionStageId = 50,
            Objectives =
            [
                new ObjectiveDefinition
                {
                    ObjectiveId = ObjectiveId.NewId(),
                    TypeTag = "dialog_choice",
                    DisplayText = "Speak to the NPC",
                    TargetTag = shortId,
                    RequiredCount = 1
                }
            ]
        };

        List<QuestStage> stages =
        [
            new QuestStage
            {
                StageId = 20,
                JournalText = "Decide what to tell the merchant",
                ObjectiveGroups = [branchingGroup]
            },
            new QuestStage
            {
                StageId = 30,
                JournalText = "You told the truth",
                ObjectiveGroups = []
            },
            new QuestStage
            {
                StageId = 50,
                JournalText = "You lied to the merchant",
                ObjectiveGroups = []
            }
        ];

        StageContext ctx = new(stages, 20);
        _sessionManager.CreateSession(
            _characterId, _questId,
            stages[0].ObjectiveGroups, _testDate,
            stageContext: ctx);

        // When the dialogue node is entered
        QuestSignal signal = new(SignalType.DialogChoice, shortId);
        RouteSignal(signal);

        // Then we branch to stage 50 (not 30)
        List<CodexDomainEvent> events = DrainChannel();
        QuestStageAdvancedEvent? stageEvent = events.OfType<QuestStageAdvancedEvent>().FirstOrDefault();
        Assert.That(stageEvent, Is.Not.Null);
        Assert.That(stageEvent!.ToStage, Is.EqualTo(50));
    }

    #endregion

    #region Full quest scenario: SetQuestStage then dialogue completion

    [Test]
    public void Full_scenario_quest_session_created_at_stage_then_dialogue_completes()
    {
        // Simulates: dialogue action sets quest to stage 20, then a dialogue node completes
        // the objective at stage 20, advancing to stage 30.
        Guid nodeGuid = Guid.Parse("33220e2f-31a8-4d66-8e35-9f92363281d6");
        string shortId = new DialogueNodeId(nodeGuid).ToShortString();

        // Build the full quest entry as it would exist after SetQuestStageAsync
        CodexQuestEntry entry = new()
        {
            QuestId = _questId,
            Title = "Goblin Troubles",
            Description = "The goblins are causing problems",
            DateStarted = _testDate,
            Stages =
            [
                new QuestStage
                {
                    StageId = 10,
                    JournalText = "Find Gilbert in the village",
                    ObjectiveGroups = []
                },
                new QuestStage
                {
                    StageId = 20,
                    JournalText = "Speak to Gilbert about the goblins",
                    ObjectiveGroups = [CreateDialogChoiceGroup(shortId)]
                },
                new QuestStage
                {
                    StageId = 30,
                    JournalText = "Gilbert told you where the goblin camp is",
                    QuestState = QuestState.Completed,
                    ObjectiveGroups = []
                }
            ]
        };
        entry.State = QuestState.InProgress;
        entry.CurrentStageId = 20;

        // Create session (equivalent to what CreateSessionForQuest does)
        QuestObjectiveTestHelpers.CreateSessionForQuest(_sessionManager, _characterId, entry);

        // When the dialogue node is entered
        QuestSignal signal = new(SignalType.DialogChoice, shortId);
        RouteSignal(signal);

        // Then the session advances from 20 → 30
        List<CodexDomainEvent> events = DrainChannel();
        QuestStageAdvancedEvent? stageEvent = events.OfType<QuestStageAdvancedEvent>().FirstOrDefault();
        Assert.That(stageEvent, Is.Not.Null, "Expected QuestStageAdvancedEvent but got none");
        Assert.That(stageEvent!.FromStage, Is.EqualTo(20));
        Assert.That(stageEvent.ToStage, Is.EqualTo(30));

        // Also verify the objective completed
        Assert.That(events.Any(e => e is ObjectiveCompletedEvent), Is.True);
        Assert.That(events.Any(e => e is QuestObjectiveGroupCompletedEvent), Is.True);
    }

    [Test]
    public void Full_scenario_idempotent_SetQuestStage_then_dialogue_still_works()
    {
        // Simulates: SetQuestStage(30) called when already at stage 30 (idempotent),
        // followed by quest entry advancing via AdvanceToStage — no crash.
        Guid nodeGuid = Guid.Parse("33220e2f-31a8-4d66-8e35-9f92363281d6");
        string shortId = new DialogueNodeId(nodeGuid).ToShortString();

        CodexQuestEntry entry = new()
        {
            QuestId = _questId,
            Title = "Goblin Troubles",
            Description = "A quest",
            DateStarted = _testDate,
            Stages =
            [
                new QuestStage
                {
                    StageId = 30,
                    JournalText = "You're at the goblin camp",
                    ObjectiveGroups = [CreateDialogChoiceGroup(shortId)]
                },
                new QuestStage
                {
                    StageId = 40,
                    JournalText = "You completed the mission",
                    ObjectiveGroups = []
                }
            ]
        };
        entry.State = QuestState.InProgress;
        entry.CurrentStageId = 30;

        // Idempotent advance — should not throw
        Assert.DoesNotThrow(() => entry.AdvanceToStage(30));

        // Session is created at stage 30
        QuestObjectiveTestHelpers.CreateSessionForQuest(_sessionManager, _characterId, entry);

        // Dialogue signal arrives
        QuestSignal signal = new(SignalType.DialogChoice, shortId);
        RouteSignal(signal);

        // Then the session advances from 30 → 40
        List<CodexDomainEvent> events = DrainChannel();
        QuestStageAdvancedEvent? stageEvent = events.OfType<QuestStageAdvancedEvent>().FirstOrDefault();
        Assert.That(stageEvent, Is.Not.Null);
        Assert.That(stageEvent!.FromStage, Is.EqualTo(30));
        Assert.That(stageEvent.ToStage, Is.EqualTo(40));
    }

    #endregion

    #region DialogueNodeId.ToShortString

    [Test]
    public void ToShortString_returns_first_8_hex_chars()
    {
        // Given a known GUID
        Guid guid = Guid.Parse("33220e2f-31a8-4d66-8e35-9f92363281d6");
        DialogueNodeId nodeId = DialogueNodeId.From(guid);

        // When
        string shortId = nodeId.ToShortString();

        // Then
        Assert.That(shortId, Is.EqualTo("33220e2f"));
        Assert.That(shortId.Length, Is.EqualTo(8));
    }

    [Test]
    public void ToShortString_is_lowercase()
    {
        DialogueNodeId nodeId = DialogueNodeId.From(Guid.Parse("ABCDEF12-0000-0000-0000-000000000000"));
        Assert.That(nodeId.ToShortString(), Is.EqualTo("abcdef12"));
    }

    #endregion

    #region Helpers

    private static QuestObjectiveGroup CreateDialogChoiceGroup(string targetNodeShortId)
    {
        return new QuestObjectiveGroup
        {
            DisplayName = "Speak to NPC",
            CompletionMode = CompletionMode.All,
            Objectives =
            [
                new ObjectiveDefinition
                {
                    ObjectiveId = ObjectiveId.NewId(),
                    TypeTag = "dialog_choice",
                    DisplayText = "Speak to the NPC",
                    TargetTag = targetNodeShortId,
                    RequiredCount = 1
                }
            ]
        };
    }

    private void RouteSignal(QuestSignal signal)
    {
        IReadOnlyList<CodexDomainEvent> events = _sessionManager.ProcessSignal(_characterId, signal);
        foreach (CodexDomainEvent domainEvent in events)
        {
            _eventChannel.Writer.TryWrite(domainEvent);
        }
    }

    private List<CodexDomainEvent> DrainChannel()
    {
        List<CodexDomainEvent> events = [];
        while (_eventChannel.Reader.TryRead(out CodexDomainEvent? e))
        {
            events.Add(e);
        }
        return events;
    }

    #endregion
}
