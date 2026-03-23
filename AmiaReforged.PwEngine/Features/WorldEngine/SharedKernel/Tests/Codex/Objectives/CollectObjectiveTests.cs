using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Objectives;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Objectives.Evaluators;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.ValueObjects;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Tests.Codex.Objectives;

[TestFixture]
public class CollectObjectiveTests
{
    private CollectObjectiveEvaluator _evaluator = null!;

    [SetUp]
    public void SetUp()
    {
        _evaluator = new CollectObjectiveEvaluator();
    }

    #region Collection

    [Test]
    public void Acquiring_matching_item_increments_count()
    {
        // Given a collect objective for 3 red gems
        ObjectiveDefinition definition = CreateCollectDefinition("red_gem", 3);
        ObjectiveState state = CreateState(definition);

        // When a red gem is acquired
        QuestSignal signal = new(SignalType.ItemAcquired, "red_gem");
        EvaluationResult result = _evaluator.Evaluate(signal, definition, state);

        // Then progress advances
        Assert.That(result.StateChanged, Is.True);
        Assert.That(state.CurrentCount, Is.EqualTo(1));
    }

    [Test]
    public void Acquiring_non_matching_item_is_ignored()
    {
        // Given a collect objective for red gems
        ObjectiveDefinition definition = CreateCollectDefinition("red_gem", 3);
        ObjectiveState state = CreateState(definition);

        // When a blue gem is acquired
        QuestSignal signal = new(SignalType.ItemAcquired, "blue_gem");
        EvaluationResult result = _evaluator.Evaluate(signal, definition, state);

        // Then nothing happens
        Assert.That(result.StateChanged, Is.False);
        Assert.That(state.CurrentCount, Is.EqualTo(0));
    }

    [Test]
    public void Reaching_required_count_completes_objective()
    {
        // Given a collect objective for 2 artifacts
        ObjectiveDefinition definition = CreateCollectDefinition("artifact", 2);
        ObjectiveState state = CreateState(definition);
        QuestSignal signal = new(SignalType.ItemAcquired, "artifact");

        // When 2 are collected
        _evaluator.Evaluate(signal, definition, state);
        EvaluationResult result = _evaluator.Evaluate(signal, definition, state);

        // Then it completes
        Assert.That(result.IsCompleted, Is.True);
        Assert.That(state.IsCompleted, Is.True);
    }

    #endregion

    #region Item Loss Tracking

    [Test]
    public void Losing_item_decrements_count_when_tracking_enabled()
    {
        // Given a collect objective with loss tracking
        ObjectiveDefinition definition = CreateCollectDefinition("letter", 3, trackLoss: true);
        ObjectiveState state = CreateState(definition);

        // And 2 letters collected
        _evaluator.Evaluate(new QuestSignal(SignalType.ItemAcquired, "letter"), definition, state);
        _evaluator.Evaluate(new QuestSignal(SignalType.ItemAcquired, "letter"), definition, state);
        Assert.That(state.CurrentCount, Is.EqualTo(2));

        // When a letter is lost
        QuestSignal lossSignal = new(SignalType.ItemLost, "letter");
        EvaluationResult result = _evaluator.Evaluate(lossSignal, definition, state);

        // Then count decrements
        Assert.That(result.StateChanged, Is.True);
        Assert.That(state.CurrentCount, Is.EqualTo(1));
    }

    [Test]
    public void Losing_item_does_not_decrement_below_zero()
    {
        // Given a collect objective with loss tracking and 0 items
        ObjectiveDefinition definition = CreateCollectDefinition("letter", 3, trackLoss: true);
        ObjectiveState state = CreateState(definition);

        // When a letter is lost
        QuestSignal lossSignal = new(SignalType.ItemLost, "letter");
        EvaluationResult result = _evaluator.Evaluate(lossSignal, definition, state);

        // Then count stays at 0
        Assert.That(result.StateChanged, Is.False);
        Assert.That(state.CurrentCount, Is.EqualTo(0));
    }

    [Test]
    public void Losing_item_is_ignored_when_tracking_disabled()
    {
        // Given a collect objective WITHOUT loss tracking
        ObjectiveDefinition definition = CreateCollectDefinition("letter", 3, trackLoss: false);
        ObjectiveState state = CreateState(definition);

        _evaluator.Evaluate(new QuestSignal(SignalType.ItemAcquired, "letter"), definition, state);
        Assert.That(state.CurrentCount, Is.EqualTo(1));

        // When a letter is lost
        QuestSignal lossSignal = new(SignalType.ItemLost, "letter");
        EvaluationResult result = _evaluator.Evaluate(lossSignal, definition, state);

        // Then count is unchanged
        Assert.That(result.StateChanged, Is.False);
        Assert.That(state.CurrentCount, Is.EqualTo(1));
    }

    #endregion

    #region Terminal State

    [Test]
    public void Signals_after_completion_are_ignored()
    {
        // Given a completed objective
        ObjectiveDefinition definition = CreateCollectDefinition("scroll", 1);
        ObjectiveState state = CreateState(definition);
        _evaluator.Evaluate(new QuestSignal(SignalType.ItemAcquired, "scroll"), definition, state);
        Assert.That(state.IsCompleted, Is.True);

        // When another is acquired
        EvaluationResult result = _evaluator.Evaluate(
            new QuestSignal(SignalType.ItemAcquired, "scroll"), definition, state);

        // Then it's ignored
        Assert.That(result.StateChanged, Is.False);
    }

    #endregion

    #region Helpers

    private static ObjectiveDefinition CreateCollectDefinition(
        string targetTag, int requiredCount, bool trackLoss = false)
    {
        Dictionary<string, object> config = new();
        if (trackLoss)
            config[CollectObjectiveEvaluator.TrackLossKey] = true;

        return new ObjectiveDefinition
        {
            ObjectiveId = ObjectiveId.NewId(),
            TypeTag = "collect",
            DisplayText = $"Collect {requiredCount} {targetTag}",
            TargetTag = targetTag,
            RequiredCount = requiredCount,
            Config = config
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
