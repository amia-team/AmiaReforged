using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Enums;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Objectives;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Objectives.Evaluators;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.ValueObjects;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Tests.Codex.Objectives;

[TestFixture]
public class CompositeObjectiveTests
{
    private ObjectiveEvaluatorRegistry _registry = null!;
    private CompositeObjectiveEvaluator _evaluator = null!;

    [SetUp]
    public void SetUp()
    {
        KillObjectiveEvaluator killEvaluator = new();
        CollectObjectiveEvaluator collectEvaluator = new();

        _registry = new ObjectiveEvaluatorRegistry(new IObjectiveEvaluator[]
        {
            killEvaluator, collectEvaluator
        });

        // Use Lazy to break the circular dependency (same as runtime DI)
        Lazy<IObjectiveEvaluatorRegistry> lazyRegistry = null!;
        _evaluator = new CompositeObjectiveEvaluator(new Lazy<IObjectiveEvaluatorRegistry>(() => lazyRegistry.Value));

        // Register composite itself so nested composites would work
        _registry = new ObjectiveEvaluatorRegistry(new IObjectiveEvaluator[]
        {
            killEvaluator, collectEvaluator, _evaluator
        });
        lazyRegistry = new Lazy<IObjectiveEvaluatorRegistry>(() => _registry);
    }

    #region All Mode

    [Test]
    public void All_mode_requires_all_children_to_complete()
    {
        // Given a composite with kill + collect in All mode
        ObjectiveDefinition killChild = CreateKillChild("goblin", 1);
        ObjectiveDefinition collectChild = CreateCollectChild("gem", 1);
        (ObjectiveDefinition composite, ObjectiveState state) =
            CreateComposite(CompletionMode.All, killChild, collectChild);

        // When only the kill objective completes
        _evaluator.Evaluate(new QuestSignal(SignalType.CreatureKilled, "goblin"), composite, state);

        // Then the composite is not yet complete
        Assert.That(state.IsCompleted, Is.False);

        // When the collect also completes
        EvaluationResult result = _evaluator.Evaluate(
            new QuestSignal(SignalType.ItemAcquired, "gem"), composite, state);

        // Then the composite completes
        Assert.That(result.IsCompleted, Is.True);
        Assert.That(state.IsCompleted, Is.True);
    }

    [Test]
    public void All_mode_fails_if_any_child_fails()
    {
        // Given a composite in All mode
        ObjectiveDefinition killChild = CreateKillChild("goblin", 2);
        ObjectiveDefinition collectChild = CreateCollectChild("gem", 1);
        (ObjectiveDefinition composite, ObjectiveState state) =
            CreateComposite(CompletionMode.All, killChild, collectChild);

        // When a child is manually failed
        Dictionary<string, ObjectiveState>? childStates =
            state.GetCustom<Dictionary<string, ObjectiveState>>("child_states");
        ObjectiveState killState = childStates![killChild.ObjectiveId.Value];
        killState.IsFailed = true;

        // And we process a signal that detects the failure state
        EvaluationResult result = _evaluator.Evaluate(
            new QuestSignal(SignalType.ItemAcquired, "gem"), composite, state);

        // Collect child completes, but the overall composite completes since collect is done
        // (the kill one is failed, so All mode won't complete)
        Assert.That(state.IsCompleted, Is.False);
    }

    #endregion

    #region Any Mode

    [Test]
    public void Any_mode_completes_when_first_child_completes()
    {
        // Given a composite with kill + collect in Any mode
        ObjectiveDefinition killChild = CreateKillChild("goblin", 1);
        ObjectiveDefinition collectChild = CreateCollectChild("gem", 1);
        (ObjectiveDefinition composite, ObjectiveState state) =
            CreateComposite(CompletionMode.Any, killChild, collectChild);

        // When only the kill objective completes
        EvaluationResult result = _evaluator.Evaluate(
            new QuestSignal(SignalType.CreatureKilled, "goblin"), composite, state);

        // Then the composite completes
        Assert.That(result.IsCompleted, Is.True);
        Assert.That(state.IsCompleted, Is.True);
    }

    [Test]
    public void Any_mode_does_not_require_all_children()
    {
        // Given a composite with 3 children in Any mode
        ObjectiveDefinition kill1 = CreateKillChild("goblin", 5);
        ObjectiveDefinition kill2 = CreateKillChild("orc", 5);
        ObjectiveDefinition collectChild = CreateCollectChild("gem", 1);
        (ObjectiveDefinition composite, ObjectiveState state) =
            CreateComposite(CompletionMode.Any, kill1, kill2, collectChild);

        // When only the collect completes (the easy one)
        EvaluationResult result = _evaluator.Evaluate(
            new QuestSignal(SignalType.ItemAcquired, "gem"), composite, state);

        // Then the composite completes
        Assert.That(result.IsCompleted, Is.True);
    }

    #endregion

    #region Sequence Mode

    [Test]
    public void Sequence_mode_only_activates_first_child_initially()
    {
        // Given a composite with kill then collect in Sequence mode
        ObjectiveDefinition killChild = CreateKillChild("goblin", 1);
        ObjectiveDefinition collectChild = CreateCollectChild("gem", 1);
        (ObjectiveDefinition composite, ObjectiveState state) =
            CreateComposite(CompletionMode.Sequence, killChild, collectChild);

        // When a collect signal arrives (second in sequence)
        EvaluationResult result = _evaluator.Evaluate(
            new QuestSignal(SignalType.ItemAcquired, "gem"), composite, state);

        // Then nothing happens — collect is not active yet
        Assert.That(result.StateChanged, Is.False);
    }

    [Test]
    public void Sequence_mode_activates_next_child_after_current_completes()
    {
        // Given a composite with kill then collect in Sequence mode
        ObjectiveDefinition killChild = CreateKillChild("goblin", 1);
        ObjectiveDefinition collectChild = CreateCollectChild("gem", 1);
        (ObjectiveDefinition composite, ObjectiveState state) =
            CreateComposite(CompletionMode.Sequence, killChild, collectChild);

        // When the kill completes (first in sequence)
        _evaluator.Evaluate(new QuestSignal(SignalType.CreatureKilled, "goblin"), composite, state);

        // Then the collect becomes active and a matching signal works
        EvaluationResult result = _evaluator.Evaluate(
            new QuestSignal(SignalType.ItemAcquired, "gem"), composite, state);

        // Then the composite completes
        Assert.That(result.IsCompleted, Is.True);
        Assert.That(state.IsCompleted, Is.True);
    }

    [Test]
    public void Sequence_mode_requires_all_children_in_order()
    {
        // Given a 3-step sequence
        ObjectiveDefinition step1 = CreateKillChild("goblin", 1);
        ObjectiveDefinition step2 = CreateCollectChild("gem", 1);
        ObjectiveDefinition step3 = CreateKillChild("boss", 1);
        (ObjectiveDefinition composite, ObjectiveState state) =
            CreateComposite(CompletionMode.Sequence, step1, step2, step3);

        // Complete step 1
        _evaluator.Evaluate(new QuestSignal(SignalType.CreatureKilled, "goblin"), composite, state);
        Assert.That(state.IsCompleted, Is.False);

        // Complete step 2
        _evaluator.Evaluate(new QuestSignal(SignalType.ItemAcquired, "gem"), composite, state);
        Assert.That(state.IsCompleted, Is.False);

        // Complete step 3
        EvaluationResult result = _evaluator.Evaluate(
            new QuestSignal(SignalType.CreatureKilled, "boss"), composite, state);

        Assert.That(result.IsCompleted, Is.True);
        Assert.That(state.IsCompleted, Is.True);
    }

    #endregion

    #region Terminal State

    [Test]
    public void Signals_after_composite_completion_are_ignored()
    {
        // Given a completed composite
        ObjectiveDefinition killChild = CreateKillChild("goblin", 1);
        (ObjectiveDefinition composite, ObjectiveState state) =
            CreateComposite(CompletionMode.All, killChild);

        _evaluator.Evaluate(new QuestSignal(SignalType.CreatureKilled, "goblin"), composite, state);
        Assert.That(state.IsCompleted, Is.True);

        // When another signal arrives
        EvaluationResult result = _evaluator.Evaluate(
            new QuestSignal(SignalType.CreatureKilled, "goblin"), composite, state);

        // Then it's ignored
        Assert.That(result.StateChanged, Is.False);
    }

    #endregion

    #region Helpers

    private static ObjectiveDefinition CreateKillChild(string tag, int count)
    {
        return new ObjectiveDefinition
        {
            ObjectiveId = ObjectiveId.NewId(),
            TypeTag = "kill",
            DisplayText = $"Kill {count} {tag}",
            TargetTag = tag,
            RequiredCount = count
        };
    }

    private static ObjectiveDefinition CreateCollectChild(string tag, int count)
    {
        return new ObjectiveDefinition
        {
            ObjectiveId = ObjectiveId.NewId(),
            TypeTag = "collect",
            DisplayText = $"Collect {count} {tag}",
            TargetTag = tag,
            RequiredCount = count
        };
    }

    private (ObjectiveDefinition, ObjectiveState) CreateComposite(
        CompletionMode mode, params ObjectiveDefinition[] children)
    {
        ObjectiveDefinition composite = new()
        {
            ObjectiveId = ObjectiveId.NewId(),
            TypeTag = "composite",
            DisplayText = "Complete all sub-objectives",
            Config = new Dictionary<string, object>
            {
                [CompositeObjectiveEvaluator.ChildrenKey] = children.ToList(),
                [CompositeObjectiveEvaluator.ModeKey] = mode
            }
        };

        ObjectiveState state = new() { ObjectiveId = composite.ObjectiveId };
        _evaluator.Initialize(composite, state);
        return (composite, state);
    }

    #endregion
}
