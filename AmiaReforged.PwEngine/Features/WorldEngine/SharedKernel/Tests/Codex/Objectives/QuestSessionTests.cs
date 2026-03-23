using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Aggregates;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Enums;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Objectives;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Objectives.Evaluators;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.ValueObjects;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Tests.Codex.Objectives;

[TestFixture]
public class QuestSessionTests
{
    private ObjectiveEvaluatorRegistry _registry = null!;
    private CharacterId _characterId;
    private QuestId _questId;
    private DateTime _testDate;

    [SetUp]
    public void SetUp()
    {
        _registry = new ObjectiveEvaluatorRegistry(new IObjectiveEvaluator[]
        {
            new KillObjectiveEvaluator(),
            new CollectObjectiveEvaluator(),
            new ReachLocationObjectiveEvaluator()
        });

        _characterId = CharacterId.New();
        _questId = new QuestId("test_quest");
        _testDate = new DateTime(2026, 3, 23, 12, 0, 0);
    }

    #region Signal Routing

    [Test]
    public void ProcessSignal_routes_to_matching_objectives_only()
    {
        // Given a session with a kill objective and a collect objective
        QuestObjectiveGroup group = new()
        {
            DisplayName = "Main Objectives",
            CompletionMode = CompletionMode.All,
            Objectives =
            [
                CreateKillDefinition("goblin", 3),
                CreateCollectDefinition("gem", 2)
            ]
        };

        QuestSession session = new(_questId, _characterId, [group], _registry, _testDate);

        // When a creature kill signal arrives
        IReadOnlyList<CodexDomainEvent> events = session.ProcessSignal(
            new QuestSignal(SignalType.CreatureKilled, "goblin"));

        // Then only the kill objective progresses
        Assert.That(events, Has.Count.EqualTo(1));
        Assert.That(events[0], Is.TypeOf<ObjectiveProgressedEvent>());
    }

    [Test]
    public void ProcessSignal_ignores_non_matching_signals()
    {
        // Given a session with a kill objective
        QuestObjectiveGroup group = new()
        {
            DisplayName = "Kill Goblins",
            Objectives = [CreateKillDefinition("goblin", 3)]
        };

        QuestSession session = new(_questId, _characterId, [group], _registry, _testDate);

        // When an unrelated signal arrives
        IReadOnlyList<CodexDomainEvent> events = session.ProcessSignal(
            new QuestSignal(SignalType.AreaEntered, "some_area"));

        // Then no events are produced
        Assert.That(events, Is.Empty);
    }

    #endregion

    #region Event Publication

    [Test]
    public void Completing_objective_publishes_ObjectiveCompletedEvent()
    {
        // Given a session with a single-kill objective
        ObjectiveDefinition killDef = CreateKillDefinition("boss", 1);
        QuestObjectiveGroup group = new()
        {
            DisplayName = "Slay the Boss",
            Objectives = [killDef]
        };

        QuestSession session = new(_questId, _characterId, [group], _registry, _testDate);

        // When the boss is killed
        IReadOnlyList<CodexDomainEvent> events = session.ProcessSignal(
            new QuestSignal(SignalType.CreatureKilled, "boss"));

        // Then an ObjectiveCompletedEvent is published
        Assert.That(events.OfType<ObjectiveCompletedEvent>().Count(), Is.EqualTo(1));
        ObjectiveCompletedEvent completed = events.OfType<ObjectiveCompletedEvent>().First();
        Assert.That(completed.QuestId, Is.EqualTo(_questId));
        Assert.That(completed.ObjectiveId, Is.EqualTo(killDef.ObjectiveId));
    }

    [Test]
    public void Progressing_objective_publishes_ObjectiveProgressedEvent_with_counts()
    {
        // Given a kill-3 objective
        ObjectiveDefinition killDef = CreateKillDefinition("goblin", 3);
        QuestObjectiveGroup group = new()
        {
            DisplayName = "Hunt Goblins",
            Objectives = [killDef]
        };

        QuestSession session = new(_questId, _characterId, [group], _registry, _testDate);

        // When first goblin killed
        IReadOnlyList<CodexDomainEvent> events = session.ProcessSignal(
            new QuestSignal(SignalType.CreatureKilled, "goblin"));

        // Then a progress event with correct counts is published
        ObjectiveProgressedEvent progress = events.OfType<ObjectiveProgressedEvent>().First();
        Assert.That(progress.OldCount, Is.EqualTo(0));
        Assert.That(progress.NewCount, Is.EqualTo(1));
        Assert.That(progress.RequiredCount, Is.EqualTo(3));
    }

    #endregion

    #region Group Completion

    [Test]
    public void Completing_all_objectives_in_group_publishes_GroupCompletedEvent()
    {
        // Given a group with 2 objectives (All mode)
        ObjectiveDefinition killDef = CreateKillDefinition("goblin", 1);
        ObjectiveDefinition collectDef = CreateCollectDefinition("gem", 1);
        QuestObjectiveGroup group = new()
        {
            DisplayName = "Retrieve the Gems",
            CompletionMode = CompletionMode.All,
            Objectives = [killDef, collectDef]
        };

        QuestSession session = new(_questId, _characterId, [group], _registry, _testDate);

        // When both objectives complete
        session.ProcessSignal(new QuestSignal(SignalType.CreatureKilled, "goblin"));
        IReadOnlyList<CodexDomainEvent> events = session.ProcessSignal(
            new QuestSignal(SignalType.ItemAcquired, "gem"));

        // Then a GroupCompletedEvent is published
        Assert.That(events.OfType<QuestObjectiveGroupCompletedEvent>().Count(), Is.EqualTo(1));
        Assert.That(session.IsGroupCompleted(0), Is.True);
    }

    [Test]
    public void Any_mode_group_completes_on_first_objective()
    {
        // Given a group with 2 objectives in Any mode
        QuestObjectiveGroup group = new()
        {
            DisplayName = "Choose Your Path",
            CompletionMode = CompletionMode.Any,
            Objectives =
            [
                CreateKillDefinition("goblin", 5),
                CreateCollectDefinition("gem", 1)
            ]
        };

        QuestSession session = new(_questId, _characterId, [group], _registry, _testDate);

        // When the easy objective completes
        IReadOnlyList<CodexDomainEvent> events = session.ProcessSignal(
            new QuestSignal(SignalType.ItemAcquired, "gem"));

        // Then the group completes
        Assert.That(events.OfType<QuestObjectiveGroupCompletedEvent>().Count(), Is.EqualTo(1));
        Assert.That(session.IsGroupCompleted(0), Is.True);
    }

    [Test]
    public void IsFullyCompleted_true_when_all_groups_complete()
    {
        // Given 2 groups
        QuestObjectiveGroup group1 = new()
        {
            DisplayName = "Phase 1",
            Objectives = [CreateKillDefinition("goblin", 1)]
        };
        QuestObjectiveGroup group2 = new()
        {
            DisplayName = "Phase 2",
            Objectives = [CreateCollectDefinition("gem", 1)]
        };

        QuestSession session = new(_questId, _characterId, [group1, group2], _registry, _testDate);

        // When both groups complete
        session.ProcessSignal(new QuestSignal(SignalType.CreatureKilled, "goblin"));
        session.ProcessSignal(new QuestSignal(SignalType.ItemAcquired, "gem"));

        // Then the quest is fully completed
        Assert.That(session.IsFullyCompleted, Is.True);
    }

    [Test]
    public void Completed_groups_do_not_process_further_signals()
    {
        // Given a completed group
        QuestObjectiveGroup group = new()
        {
            DisplayName = "Kill Boss",
            Objectives = [CreateKillDefinition("boss", 1)]
        };

        QuestSession session = new(_questId, _characterId, [group], _registry, _testDate);
        session.ProcessSignal(new QuestSignal(SignalType.CreatureKilled, "boss"));
        Assert.That(session.IsGroupCompleted(0), Is.True);

        // When another signal arrives for the same group
        IReadOnlyList<CodexDomainEvent> events = session.ProcessSignal(
            new QuestSignal(SignalType.CreatureKilled, "boss"));

        // Then no events are produced
        Assert.That(events, Is.Empty);
    }

    #endregion

    #region Sequence Mode in Groups

    [Test]
    public void Sequence_group_only_activates_first_objective()
    {
        // Given a sequence group: kill goblin → collect gem
        ObjectiveDefinition killDef = CreateKillDefinition("goblin", 1);
        ObjectiveDefinition collectDef = CreateCollectDefinition("gem", 1);
        QuestObjectiveGroup group = new()
        {
            DisplayName = "Sequential Tasks",
            CompletionMode = CompletionMode.Sequence,
            Objectives = [killDef, collectDef]
        };

        QuestSession session = new(_questId, _characterId, [group], _registry, _testDate);

        // Then the collect objective should not be active initially
        ObjectiveState? collectState = session.GetObjectiveState(collectDef.ObjectiveId);
        Assert.That(collectState!.IsActive, Is.False);

        // When a collect signal arrives before kill is done
        IReadOnlyList<CodexDomainEvent> events = session.ProcessSignal(
            new QuestSignal(SignalType.ItemAcquired, "gem"));

        // Then nothing happens
        Assert.That(events, Is.Empty);
    }

    [Test]
    public void Sequence_group_activates_next_after_current_completes()
    {
        // Given a sequence group
        ObjectiveDefinition killDef = CreateKillDefinition("goblin", 1);
        ObjectiveDefinition collectDef = CreateCollectDefinition("gem", 1);
        QuestObjectiveGroup group = new()
        {
            DisplayName = "Sequential Tasks",
            CompletionMode = CompletionMode.Sequence,
            Objectives = [killDef, collectDef]
        };

        QuestSession session = new(_questId, _characterId, [group], _registry, _testDate);

        // When the kill completes
        session.ProcessSignal(new QuestSignal(SignalType.CreatureKilled, "goblin"));

        // Then the collect becomes active
        ObjectiveState? collectState = session.GetObjectiveState(collectDef.ObjectiveId);
        Assert.That(collectState!.IsActive, Is.True);

        // And a collect signal now works
        IReadOnlyList<CodexDomainEvent> events = session.ProcessSignal(
            new QuestSignal(SignalType.ItemAcquired, "gem"));
        Assert.That(events.OfType<ObjectiveCompletedEvent>().Count(), Is.EqualTo(1));
    }

    #endregion

    #region Helpers

    private static ObjectiveDefinition CreateKillDefinition(string targetTag, int count)
    {
        return new ObjectiveDefinition
        {
            ObjectiveId = ObjectiveId.NewId(),
            TypeTag = "kill",
            DisplayText = $"Kill {count} {targetTag}",
            TargetTag = targetTag,
            RequiredCount = count
        };
    }

    private static ObjectiveDefinition CreateCollectDefinition(string targetTag, int count)
    {
        return new ObjectiveDefinition
        {
            ObjectiveId = ObjectiveId.NewId(),
            TypeTag = "collect",
            DisplayText = $"Collect {count} {targetTag}",
            TargetTag = targetTag,
            RequiredCount = count
        };
    }

    #endregion
}
