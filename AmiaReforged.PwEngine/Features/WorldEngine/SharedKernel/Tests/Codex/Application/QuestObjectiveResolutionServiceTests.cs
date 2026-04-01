using System.Threading.Channels;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Application;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Aggregates;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Enums;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Objectives;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Objectives.Evaluators;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Infrastructure;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.ValueObjects;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Tests.Codex.Application;

[TestFixture]
public class QuestObjectiveResolutionServiceTests
{
    private InMemoryPlayerCodexRepository _repository = null!;
    private QuestSessionManager _sessionManager = null!;
    private CodexEventProcessor _eventProcessor = null!;
    private Channel<CodexDomainEvent> _eventChannel = null!;
    private CharacterId _characterId;
    private QuestId _questId;
    private DateTime _testDate;

    [SetUp]
    public void SetUp()
    {
        _repository = new InMemoryPlayerCodexRepository();

        ObjectiveEvaluatorRegistry registry = new(new IObjectiveEvaluator[]
        {
            new CollectObjectiveEvaluator(),
            new KillObjectiveEvaluator(),
            new ReachLocationObjectiveEvaluator(),
            new DialogChoiceObjectiveEvaluator()
        });

        _sessionManager = new QuestSessionManager(registry);

        _eventChannel = Channel.CreateUnbounded<CodexDomainEvent>();
        _eventProcessor = new CodexEventProcessor(_repository, _eventChannel);

        _characterId = CharacterId.New();
        _questId = new QuestId("collect_herbs");
        _testDate = new DateTime(2026, 4, 1, 12, 0, 0);
    }

    [Test]
    public void ProcessItemAcquired_routes_signal_and_produces_events()
    {
        // Given an active session tracking collection of "herb" ×3
        QuestObjectiveGroup group = CreateCollectGroup("herb", 3);
        _sessionManager.CreateSession(_characterId, _questId, [group], _testDate);

        // When a matching item acquired signal arrives
        QuestObjectiveTestHelpers.ProcessItemAcquired(_sessionManager, _eventChannel, _characterId, "herb");

        // Then the event channel contains a progress event
        Assert.That(_eventChannel.Reader.TryRead(out CodexDomainEvent? domainEvent), Is.True);
        Assert.That(domainEvent, Is.TypeOf<ObjectiveProgressedEvent>());
    }

    [Test]
    public void ProcessItemAcquired_no_session_produces_no_events()
    {
        // Given no active sessions for the character

        // When an item acquired signal arrives
        QuestObjectiveTestHelpers.ProcessItemAcquired(_sessionManager, _eventChannel, _characterId, "herb");

        // Then no events are produced
        Assert.That(_eventChannel.Reader.TryRead(out _), Is.False);
    }

    [Test]
    public void ProcessItemAcquired_non_matching_item_produces_no_events()
    {
        // Given a session tracking "herb" collection
        QuestObjectiveGroup group = CreateCollectGroup("herb", 3);
        _sessionManager.CreateSession(_characterId, _questId, [group], _testDate);

        // When a non-matching item is acquired
        QuestObjectiveTestHelpers.ProcessItemAcquired(_sessionManager, _eventChannel, _characterId, "sword");

        // Then no events are produced
        Assert.That(_eventChannel.Reader.TryRead(out _), Is.False);
    }

    [Test]
    public void ProcessItemAcquired_completing_objective_produces_completed_event()
    {
        // Given a session tracking "rune" ×1
        QuestObjectiveGroup group = CreateCollectGroup("rune", 1);
        _sessionManager.CreateSession(_characterId, _questId, [group], _testDate);

        // When the required item is acquired
        QuestObjectiveTestHelpers.ProcessItemAcquired(_sessionManager, _eventChannel, _characterId, "rune");

        // Then we get both a progress event and a completed event
        List<CodexDomainEvent> events = DrainChannel();
        Assert.That(events.Any(e => e is ObjectiveCompletedEvent), Is.True);
    }

    [Test]
    public void ProcessItemLost_decrements_count_when_loss_tracking_enabled()
    {
        // Given a collect objective with loss tracking, already at count 2
        QuestObjectiveGroup group = CreateCollectGroupWithLossTracking("artifact", 3);
        _sessionManager.CreateSession(_characterId, _questId, [group], _testDate);

        // Acquire 2 first
        QuestObjectiveTestHelpers.ProcessItemAcquired(_sessionManager, _eventChannel, _characterId, "artifact");
        QuestObjectiveTestHelpers.ProcessItemAcquired(_sessionManager, _eventChannel, _characterId, "artifact");
        DrainChannel(); // clear progress events

        // When an artifact is lost
        QuestObjectiveTestHelpers.ProcessItemLost(_sessionManager, _eventChannel, _characterId, "artifact");

        // Then a progress event with decremented count is produced
        Assert.That(_eventChannel.Reader.TryRead(out CodexDomainEvent? evt), Is.True);
        Assert.That(evt, Is.TypeOf<ObjectiveProgressedEvent>());
    }

    [Test]
    public async Task InitializeSessionsForPlayer_creates_sessions_for_InProgress_quests()
    {
        // Given a codex with one InProgress quest that has objectives
        PlayerCodex codex = CreateCodexWithInProgressQuest();
        await _repository.SaveAsync(codex);

        // When sessions are initialized
        await InitializeSessionsAsync();

        // Then a session exists for the quest
        Assert.That(_sessionManager.HasSession(_characterId, _questId), Is.True);
    }

    [Test]
    public async Task InitializeSessionsForPlayer_skips_completed_quests()
    {
        // Given a codex with a completed quest
        PlayerCodex codex = CreateCodexWithCompletedQuest();
        await _repository.SaveAsync(codex);

        // When sessions are initialized
        await InitializeSessionsAsync();

        // Then no session is created
        Assert.That(_sessionManager.HasSession(_characterId, _questId), Is.False);
    }

    [Test]
    public async Task InitializeSessionsForPlayer_skips_quests_without_objectives()
    {
        // Given a codex with an InProgress quest but no objectives in its current stage
        PlayerCodex codex = CreateCodexWithQuestNoObjectives();
        await _repository.SaveAsync(codex);

        // When sessions are initialized
        await InitializeSessionsAsync();

        // Then no session is created (no objective groups to track)
        Assert.That(_sessionManager.HasSession(_characterId, _questId), Is.False);
    }

    [Test]
    public async Task InitializeSessionsForPlayer_no_codex_is_noop()
    {
        // Given no codex exists for character

        // When sessions are initialized
        await InitializeSessionsAsync();

        // Then no sessions
        Assert.That(_sessionManager.GetAllSessions(_characterId), Is.Empty);
    }

    [Test]
    public void TeardownSessionsForPlayer_removes_all_sessions()
    {
        // Given sessions exist for the character
        QuestObjectiveGroup group = CreateCollectGroup("herb", 3);
        _sessionManager.CreateSession(_characterId, _questId, [group], _testDate);
        _sessionManager.CreateSession(_characterId, new QuestId("second_quest"), [group], _testDate);

        Assert.That(_sessionManager.GetAllSessions(_characterId), Has.Count.EqualTo(2));

        // When sessions are torn down
        TeardownSessions();

        // Then no sessions remain
        Assert.That(_sessionManager.GetAllSessions(_characterId), Is.Empty);
    }

    [Test]
    public void TeardownSessionsForPlayer_no_sessions_is_noop()
    {
        // Given no sessions exist for the character

        // When sessions are torn down — no exception
        Assert.DoesNotThrow(() => TeardownSessions());
    }

    [Test]
    public void CreateSessionForQuest_creates_session_from_quest_entry()
    {
        // Given an InProgress quest entry with objectives
        CodexQuestEntry entry = CreateQuestEntryWithObjectives(QuestState.InProgress);

        // When a session is created
        QuestObjectiveTestHelpers.CreateSessionForQuest(_sessionManager, _characterId, entry);

        // Then the session exists
        Assert.That(_sessionManager.HasSession(_characterId, entry.QuestId), Is.True);
    }

    [Test]
    public void CreateSessionForQuest_replaces_existing_session()
    {
        // Given an existing session
        QuestObjectiveGroup oldGroup = CreateCollectGroup("old_item", 5);
        _sessionManager.CreateSession(_characterId, _questId, [oldGroup], _testDate);

        // When CreateSessionForQuest is called again for the same quest
        CodexQuestEntry entry = CreateQuestEntryWithObjectives(QuestState.InProgress);
        QuestObjectiveTestHelpers.CreateSessionForQuest(_sessionManager, _characterId, entry);

        // Then old session is replaced (still has 1 session for this quest)
        Assert.That(_sessionManager.HasSession(_characterId, _questId), Is.True);
    }

    [Test]
    public void CreateSessionForQuest_skips_quest_without_objectives()
    {
        // Given a quest entry with no objectives
        CodexQuestEntry entry = CreateQuestEntryNoObjectives(QuestState.InProgress);

        // When a session is created
        QuestObjectiveTestHelpers.CreateSessionForQuest(_sessionManager, _characterId, entry);

        // Then no session is created
        Assert.That(_sessionManager.HasSession(_characterId, entry.QuestId), Is.False);
    }

    [Test]
    public void GetCurrentStageObjectiveGroups_returns_correct_stage_objectives()
    {
        // Given a quest at stage 20 with stages 10 and 20
        CodexQuestEntry entry = new()
        {
            QuestId = _questId,
            Title = "Herb Quest",
            Description = "Collect herbs",
            DateStarted = _testDate,
            Stages =
            [
                new QuestStage
                {
                    StageId = 10,
                    JournalText = "Stage 10",
                    ObjectiveGroups = [CreateCollectGroup("herb_a", 2)]
                },
                new QuestStage
                {
                    StageId = 20,
                    JournalText = "Stage 20",
                    ObjectiveGroups = [CreateCollectGroup("herb_b", 5)]
                }
            ]
        };
        entry.State = QuestState.InProgress;
        entry.CurrentStageId = 20;

        // When
        List<QuestObjectiveGroup> result = QuestObjectiveTestHelpers.GetCurrentStageObjectiveGroups(entry);

        // Then stage 20's objectives are returned
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Objectives[0].TargetTag, Is.EqualTo("herb_b"));
    }

    [Test]
    public void GetCurrentStageObjectiveGroups_returns_empty_when_no_stages()
    {
        // Given a quest with no stages
        CodexQuestEntry entry = new()
        {
            QuestId = _questId,
            Title = "Empty Quest",
            Description = "No stages",
            DateStarted = _testDate,
            Stages = []
        };

        // When
        List<QuestObjectiveGroup> result = QuestObjectiveTestHelpers.GetCurrentStageObjectiveGroups(entry);

        // Then empty
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void GetCurrentStageObjectiveGroups_falls_back_to_highest_stage_leq_current()
    {
        // Given a quest at stage 25, with stages at 10 and 20 (no stage 25 defined)
        CodexQuestEntry entry = new()
        {
            QuestId = _questId,
            Title = "Gap Quest",
            Description = "Stage gap",
            DateStarted = _testDate,
            Stages =
            [
                new QuestStage
                {
                    StageId = 10,
                    JournalText = "Stage 10",
                    ObjectiveGroups = [CreateCollectGroup("item_a", 1)]
                },
                new QuestStage
                {
                    StageId = 20,
                    JournalText = "Stage 20",
                    ObjectiveGroups = [CreateCollectGroup("item_b", 1)]
                }
            ]
        };
        entry.State = QuestState.InProgress;
        entry.CurrentStageId = 25;

        // When
        List<QuestObjectiveGroup> result = QuestObjectiveTestHelpers.GetCurrentStageObjectiveGroups(entry);

        // Then stage 20's objectives are returned (highest ≤ 25)
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Objectives[0].TargetTag, Is.EqualTo("item_b"));
    }

    [Test]
    public async Task Full_flow_login_acquire_items_complete_objective()
    {
        // Given a codex with an InProgress quest to collect 2 mushrooms
        PlayerCodex codex = CreateCodexWithCollectQuest("mushroom", 2);
        await _repository.SaveAsync(codex);

        // When player logs in (sessions initialized)
        await InitializeSessionsAsync();
        Assert.That(_sessionManager.HasSession(_characterId, _questId), Is.True);

        // And acquires 2 mushrooms
        QuestObjectiveTestHelpers.ProcessItemAcquired(_sessionManager, _eventChannel, _characterId, "mushroom");
        QuestObjectiveTestHelpers.ProcessItemAcquired(_sessionManager, _eventChannel, _characterId, "mushroom");

        // Then we get progress + completion events
        List<CodexDomainEvent> events = DrainChannel();
        Assert.That(events.Count, Is.GreaterThanOrEqualTo(2));
        Assert.That(events.Any(e => e is ObjectiveCompletedEvent), Is.True);
    }

    [Test]
    public async Task ProcessDialogueNodeEntered_routes_signal_and_completes_dialog_objective()
    {
        // Given a quest at stage 20 with a dialog_choice objective
        Guid nodeGuid = Guid.Parse("33220e2f-31a8-4d66-8e35-9f92363281d6");
        DialogueNodeId nodeId = DialogueNodeId.From(nodeGuid);
        string shortId = nodeId.ToShortString();

        CodexQuestEntry entry = new()
        {
            QuestId = _questId,
            Title = "Talk Quest",
            Description = "Speak to someone",
            DateStarted = _testDate,
            Stages =
            [
                new QuestStage
                {
                    StageId = 20,
                    JournalText = "Speak to Gilbert",
                    ObjectiveGroups =
                    [
                        new QuestObjectiveGroup
                        {
                            DisplayName = "Talk",
                            CompletionMode = CompletionMode.All,
                            Objectives =
                            [
                                new ObjectiveDefinition
                                {
                                    ObjectiveId = ObjectiveId.NewId(),
                                    TypeTag = "dialog_choice",
                                    DisplayText = "Speak to Gilbert",
                                    TargetTag = shortId,
                                    RequiredCount = 1
                                }
                            ]
                        }
                    ]
                },
                new QuestStage
                {
                    StageId = 30,
                    JournalText = "You spoke to Gilbert"
                }
            ]
        };
        entry.State = QuestState.InProgress;
        entry.CurrentStageId = 20;

        PlayerCodex codex = new(_characterId, _testDate);
        codex.RecordQuestStarted(entry, _testDate);
        entry.State = QuestState.InProgress;
        entry.CurrentStageId = 20;
        await _repository.SaveAsync(codex);

        // When player logs in and the session is initialized
        await InitializeSessionsAsync();
        Assert.That(_sessionManager.HasSession(_characterId, _questId), Is.True);

        // And the dialogue node is entered
        QuestObjectiveTestHelpers.ProcessDialogueNodeEntered(
            _sessionManager, _eventChannel, _characterId, nodeId);

        // Then the objective completes and stage advances 20 → 30
        List<CodexDomainEvent> events = DrainChannel();
        Assert.That(events.Any(e => e is ObjectiveCompletedEvent), Is.True,
            "Expected ObjectiveCompletedEvent");
        Assert.That(events.Any(e => e is QuestObjectiveGroupCompletedEvent), Is.True,
            "Expected QuestObjectiveGroupCompletedEvent");

        QuestStageAdvancedEvent? stageEvent = events.OfType<QuestStageAdvancedEvent>().FirstOrDefault();
        Assert.That(stageEvent, Is.Not.Null, "Expected QuestStageAdvancedEvent");
        Assert.That(stageEvent!.FromStage, Is.EqualTo(20));
        Assert.That(stageEvent.ToStage, Is.EqualTo(30));
    }

    [Test]
    public async Task ProcessDialogueNodeEntered_non_matching_node_produces_no_events()
    {
        // Given a dialog_choice quest session
        Guid nodeGuid = Guid.Parse("33220e2f-31a8-4d66-8e35-9f92363281d6");
        string shortId = new DialogueNodeId(nodeGuid).ToShortString();

        CodexQuestEntry entry = new()
        {
            QuestId = _questId,
            Title = "Talk Quest",
            Description = "Speak to someone",
            DateStarted = _testDate,
            Stages =
            [
                new QuestStage
                {
                    StageId = 20,
                    JournalText = "Speak to Gilbert",
                    ObjectiveGroups =
                    [
                        new QuestObjectiveGroup
                        {
                            DisplayName = "Talk",
                            CompletionMode = CompletionMode.All,
                            Objectives =
                            [
                                new ObjectiveDefinition
                                {
                                    ObjectiveId = ObjectiveId.NewId(),
                                    TypeTag = "dialog_choice",
                                    DisplayText = "Speak to Gilbert",
                                    TargetTag = shortId,
                                    RequiredCount = 1
                                }
                            ]
                        }
                    ]
                }
            ]
        };
        entry.State = QuestState.InProgress;
        entry.CurrentStageId = 20;

        PlayerCodex codex = new(_characterId, _testDate);
        codex.RecordQuestStarted(entry, _testDate);
        entry.State = QuestState.InProgress;
        entry.CurrentStageId = 20;
        await _repository.SaveAsync(codex);

        await InitializeSessionsAsync();

        // When a DIFFERENT dialogue node is entered
        DialogueNodeId wrongNode = DialogueNodeId.From(Guid.Parse("aaaabbbb-0000-0000-0000-000000000000"));
        QuestObjectiveTestHelpers.ProcessDialogueNodeEntered(
            _sessionManager, _eventChannel, _characterId, wrongNode);

        // Then no events produced
        Assert.That(_eventChannel.Reader.TryRead(out _), Is.False);
    }

    /// <summary>
    /// Simulates InitializeSessionsForPlayerAsync without NWN dependencies,
    /// using the same logic as QuestObjectiveTestHelpers.
    /// </summary>
    private async Task InitializeSessionsAsync()
    {
        PlayerCodex? codex = await _repository.LoadAsync(_characterId);
        if (codex == null) return;

        foreach (CodexQuestEntry quest in codex.Quests)
        {
            if (quest.State != QuestState.InProgress) continue;
            QuestObjectiveTestHelpers.CreateSessionForQuest(_sessionManager, _characterId, quest);
        }
    }

    /// <summary>
    /// Simulates TeardownSessionsForPlayer without NWN dependencies.
    /// </summary>
    private void TeardownSessions()
    {
        IReadOnlyCollection<QuestSession> sessions = _sessionManager.GetAllSessions(_characterId);
        List<QuestId> questIds = sessions.Select(s => s.QuestId).ToList();

        foreach (QuestId questId in questIds)
        {
            _sessionManager.RemoveSession(_characterId, questId);
        }
    }

    private PlayerCodex CreateCodexWithInProgressQuest()
    {
        PlayerCodex codex = new(_characterId, _testDate);
        CodexQuestEntry entry = CreateQuestEntryWithObjectives(QuestState.InProgress);
        codex.RecordQuestStarted(entry, _testDate);
        return codex;
    }

    private PlayerCodex CreateCodexWithCompletedQuest()
    {
        PlayerCodex codex = new(_characterId, _testDate);
        CodexQuestEntry entry = CreateQuestEntryWithObjectives(QuestState.InProgress);
        codex.RecordQuestStarted(entry, _testDate);
        codex.RecordQuestCompleted(_questId, _testDate);
        return codex;
    }

    private PlayerCodex CreateCodexWithQuestNoObjectives()
    {
        PlayerCodex codex = new(_characterId, _testDate);
        CodexQuestEntry entry = CreateQuestEntryNoObjectives(QuestState.InProgress);
        codex.RecordQuestStarted(entry, _testDate);
        return codex;
    }

    private PlayerCodex CreateCodexWithCollectQuest(string targetTag, int count)
    {
        PlayerCodex codex = new(_characterId, _testDate);
        CodexQuestEntry entry = new()
        {
            QuestId = _questId,
            Title = "Collect Quest",
            Description = "Collect items",
            DateStarted = _testDate,
            Stages =
            [
                new QuestStage
                {
                    StageId = 10,
                    JournalText = "Collect the items",
                    ObjectiveGroups = [CreateCollectGroup(targetTag, count)]
                }
            ]
        };
        entry.State = QuestState.InProgress;
        entry.CurrentStageId = 10;
        codex.RecordQuestStarted(entry, _testDate);
        return codex;
    }

    private CodexQuestEntry CreateQuestEntryWithObjectives(QuestState state)
    {
        CodexQuestEntry entry = new()
        {
            QuestId = _questId,
            Title = "Herb Collection",
            Description = "Collect herbs for the potion",
            DateStarted = _testDate,
            Stages =
            [
                new QuestStage
                {
                    StageId = 10,
                    JournalText = "Gather 3 healing herbs",
                    ObjectiveGroups = [CreateCollectGroup("healing_herb", 3)]
                }
            ]
        };
        entry.State = state;
        entry.CurrentStageId = 10;
        return entry;
    }

    private CodexQuestEntry CreateQuestEntryNoObjectives(QuestState state)
    {
        CodexQuestEntry entry = new()
        {
            QuestId = _questId,
            Title = "Chat Quest",
            Description = "A narrative-only quest",
            DateStarted = _testDate,
            Stages =
            [
                new QuestStage
                {
                    StageId = 10,
                    JournalText = "Talk to the elder",
                    ObjectiveGroups = [] // no objectives
                }
            ]
        };
        entry.State = state;
        entry.CurrentStageId = 10;
        return entry;
    }

    private static QuestObjectiveGroup CreateCollectGroup(string targetTag, int requiredCount)
    {
        return new QuestObjectiveGroup
        {
            DisplayName = $"Collect {requiredCount} {targetTag}",
            CompletionMode = CompletionMode.All,
            Objectives =
            [
                new ObjectiveDefinition
                {
                    ObjectiveId = ObjectiveId.NewId(),
                    TypeTag = "collect",
                    DisplayText = $"Collect {requiredCount} {targetTag}",
                    TargetTag = targetTag,
                    RequiredCount = requiredCount
                }
            ]
        };
    }

    private static QuestObjectiveGroup CreateCollectGroupWithLossTracking(string targetTag, int requiredCount)
    {
        return new QuestObjectiveGroup
        {
            DisplayName = $"Collect {requiredCount} {targetTag}",
            CompletionMode = CompletionMode.All,
            Objectives =
            [
                new ObjectiveDefinition
                {
                    ObjectiveId = ObjectiveId.NewId(),
                    TypeTag = "collect",
                    DisplayText = $"Collect {requiredCount} {targetTag}",
                    TargetTag = targetTag,
                    RequiredCount = requiredCount,
                    Config = new Dictionary<string, object>
                    {
                        [CollectObjectiveEvaluator.TrackLossKey] = true
                    }
                }
            ]
        };
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
}
