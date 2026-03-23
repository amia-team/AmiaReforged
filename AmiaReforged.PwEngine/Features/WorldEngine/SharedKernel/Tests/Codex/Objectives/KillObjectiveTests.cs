using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Objectives;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Objectives.Evaluators;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.ValueObjects;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Tests.Codex.Objectives;

[TestFixture]
public class KillObjectiveTests
{
    private KillObjectiveEvaluator _evaluator = null!;

    [SetUp]
    public void SetUp()
    {
        _evaluator = new KillObjectiveEvaluator();
    }

    #region Signal Matching

    [Test]
    public void Killing_matching_creature_increments_count()
    {
        // Given a kill objective requiring 3 goblins
        ObjectiveDefinition definition = CreateKillDefinition("goblin", 3);
        ObjectiveState state = CreateState(definition);

        // When a goblin is killed
        QuestSignal signal = new(SignalType.CreatureKilled, "goblin");
        EvaluationResult result = _evaluator.Evaluate(signal, definition, state);

        // Then progress advances
        Assert.That(result.StateChanged, Is.True);
        Assert.That(result.IsCompleted, Is.False);
        Assert.That(state.CurrentCount, Is.EqualTo(1));
    }

    [Test]
    public void Killing_non_matching_creature_is_ignored()
    {
        // Given a kill objective for goblins
        ObjectiveDefinition definition = CreateKillDefinition("goblin", 3);
        ObjectiveState state = CreateState(definition);

        // When an orc is killed
        QuestSignal signal = new(SignalType.CreatureKilled, "orc");
        EvaluationResult result = _evaluator.Evaluate(signal, definition, state);

        // Then nothing happens
        Assert.That(result.StateChanged, Is.False);
        Assert.That(state.CurrentCount, Is.EqualTo(0));
    }

    [Test]
    public void Non_kill_signal_type_is_ignored()
    {
        // Given a kill objective
        ObjectiveDefinition definition = CreateKillDefinition("goblin", 3);
        ObjectiveState state = CreateState(definition);

        // When an item acquired signal with the same tag arrives
        QuestSignal signal = new(SignalType.ItemAcquired, "goblin");
        EvaluationResult result = _evaluator.Evaluate(signal, definition, state);

        // Then nothing happens
        Assert.That(result.StateChanged, Is.False);
    }

    [Test]
    public void Tag_matching_is_case_insensitive()
    {
        // Given a kill objective for "Goblin_Chief"
        ObjectiveDefinition definition = CreateKillDefinition("Goblin_Chief", 1);
        ObjectiveState state = CreateState(definition);

        // When "goblin_chief" (lowercase) is killed
        QuestSignal signal = new(SignalType.CreatureKilled, "goblin_chief");
        EvaluationResult result = _evaluator.Evaluate(signal, definition, state);

        // Then it matches and completes
        Assert.That(result.IsCompleted, Is.True);
    }

    #endregion

    #region Completion

    [Test]
    public void Reaching_required_count_completes_objective()
    {
        // Given a kill objective requiring 2 goblins
        ObjectiveDefinition definition = CreateKillDefinition("goblin", 2);
        ObjectiveState state = CreateState(definition);
        QuestSignal signal = new(SignalType.CreatureKilled, "goblin");

        // When 2 goblins are killed
        _evaluator.Evaluate(signal, definition, state);
        EvaluationResult result = _evaluator.Evaluate(signal, definition, state);

        // Then the objective completes
        Assert.That(result.IsCompleted, Is.True);
        Assert.That(state.IsCompleted, Is.True);
        Assert.That(state.IsActive, Is.False);
        Assert.That(state.CurrentCount, Is.EqualTo(2));
    }

    [Test]
    public void Single_kill_objective_completes_on_first_kill()
    {
        // Given a kill objective requiring 1
        ObjectiveDefinition definition = CreateKillDefinition("boss_dragon", 1);
        ObjectiveState state = CreateState(definition);

        // When the dragon is killed
        QuestSignal signal = new(SignalType.CreatureKilled, "boss_dragon");
        EvaluationResult result = _evaluator.Evaluate(signal, definition, state);

        // Then it completes immediately
        Assert.That(result.IsCompleted, Is.True);
        Assert.That(state.IsCompleted, Is.True);
    }

    #endregion

    #region Terminal State

    [Test]
    public void Signals_after_completion_are_ignored()
    {
        // Given a completed kill objective
        ObjectiveDefinition definition = CreateKillDefinition("goblin", 1);
        ObjectiveState state = CreateState(definition);
        QuestSignal signal = new(SignalType.CreatureKilled, "goblin");

        _evaluator.Evaluate(signal, definition, state);
        Assert.That(state.IsCompleted, Is.True);

        // When another goblin is killed
        EvaluationResult result = _evaluator.Evaluate(signal, definition, state);

        // Then it's ignored
        Assert.That(result.StateChanged, Is.False);
        Assert.That(state.CurrentCount, Is.EqualTo(1));
    }

    [Test]
    public void Signals_after_failure_are_ignored()
    {
        // Given a failed objective
        ObjectiveDefinition definition = CreateKillDefinition("goblin", 3);
        ObjectiveState state = CreateState(definition);
        state.IsFailed = true;

        // When a matching signal arrives
        QuestSignal signal = new(SignalType.CreatureKilled, "goblin");
        EvaluationResult result = _evaluator.Evaluate(signal, definition, state);

        // Then it's ignored
        Assert.That(result.StateChanged, Is.False);
    }

    #endregion

    #region Helpers

    private static ObjectiveDefinition CreateKillDefinition(string targetTag, int requiredCount)
    {
        return new ObjectiveDefinition
        {
            ObjectiveId = ObjectiveId.NewId(),
            TypeTag = "kill",
            DisplayText = $"Kill {requiredCount} {targetTag}",
            TargetTag = targetTag,
            RequiredCount = requiredCount
        };
    }

    private ObjectiveState CreateState(ObjectiveDefinition definition)
    {
        ObjectiveState state = new() { ObjectiveId = definition.ObjectiveId };
        _evaluator.Initialize(definition, state);
        return state;
    }

    #endregion
}
