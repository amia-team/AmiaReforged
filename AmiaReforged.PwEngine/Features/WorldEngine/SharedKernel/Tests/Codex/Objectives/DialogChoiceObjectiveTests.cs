using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Objectives;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Objectives.Evaluators;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.ValueObjects;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Tests.Codex.Objectives;

[TestFixture]
public class DialogChoiceObjectiveTests
{
    private DialogChoiceObjectiveEvaluator _evaluator = null!;
    private ObjectiveDefinition _definition = null!;
    private ObjectiveState _state = null!;

    [SetUp]
    public void SetUp()
    {
        _evaluator = new DialogChoiceObjectiveEvaluator();

        _definition = new ObjectiveDefinition
        {
            ObjectiveId = ObjectiveId.NewId(),
            TypeTag = "dialog_choice",
            DisplayText = "Speak to Gilbert about the goblins",
            TargetTag = "33220e2f", // truncated node ID
            RequiredCount = 1
        };

        _state = new ObjectiveState
        {
            ObjectiveId = ObjectiveId.NewId(),
            IsActive = true,
            IsCompleted = false
        };
    }

    [Test]
    public void TypeTag_is_dialog_choice()
    {
        Assert.That(_evaluator.TypeTag, Is.EqualTo("dialog_choice"));
    }

    [Test]
    public void Matching_DialogChoice_signal_completes_objective()
    {
        // Given a dialog_choice objective targeting node "33220e2f"
        QuestSignal signal = new(SignalType.DialogChoice, "33220e2f");

        // When evaluated
        EvaluationResult result = _evaluator.Evaluate(signal, _definition, _state);

        // Then the objective completes
        Assert.That(result.IsCompleted, Is.True);
        Assert.That(_state.IsCompleted, Is.True);
        Assert.That(_state.IsActive, Is.False);
    }

    [Test]
    public void Non_matching_tag_produces_no_op()
    {
        // Given a signal with a different node ID
        QuestSignal signal = new(SignalType.DialogChoice, "aaaabbbb");

        // When evaluated
        EvaluationResult result = _evaluator.Evaluate(signal, _definition, _state);

        // Then no change
        Assert.That(result.StateChanged, Is.False);
        Assert.That(_state.IsCompleted, Is.False);
        Assert.That(_state.IsActive, Is.True);
    }

    [Test]
    public void Tag_matching_is_case_insensitive()
    {
        // Given the same node ID in uppercase
        QuestSignal signal = new(SignalType.DialogChoice, "33220E2F");

        // When evaluated
        EvaluationResult result = _evaluator.Evaluate(signal, _definition, _state);

        // Then completes
        Assert.That(result.IsCompleted, Is.True);
    }

    [Test]
    public void Wrong_signal_type_produces_no_op()
    {
        // Given an ItemAcquired signal (not DialogChoice)
        QuestSignal signal = new(SignalType.ItemAcquired, "33220e2f");

        // When evaluated
        EvaluationResult result = _evaluator.Evaluate(signal, _definition, _state);

        // Then no change
        Assert.That(result.StateChanged, Is.False);
    }

    [Test]
    public void Already_completed_objective_is_terminal_no_op()
    {
        // Given an already-completed state
        _state.IsCompleted = true;
        _state.IsActive = false;

        QuestSignal signal = new(SignalType.DialogChoice, "33220e2f");

        // When evaluated
        EvaluationResult result = _evaluator.Evaluate(signal, _definition, _state);

        // Then no-op (terminal)
        Assert.That(result.StateChanged, Is.False);
    }

    [Test]
    public void Inactive_state_is_skipped_by_session()
    {
        // Given an inactive state (e.g. later objective in a Sequence group)
        _state.IsActive = false;

        QuestSignal signal = new(SignalType.DialogChoice, "33220e2f");

        // When evaluated — even with matching tag, state is inactive
        EvaluationResult result = _evaluator.Evaluate(signal, _definition, _state);

        // Then no change — the evaluator itself doesn't check IsActive,
        // but the session does. The evaluator checks IsTerminal.
        // Since IsActive=false but IsCompleted=false, IsTerminal=false, so
        // the evaluator will still evaluate. However, the SESSION skips
        // inactive objectives before calling the evaluator.
        // This test documents evaluator behavior in isolation.
        Assert.That(result.IsCompleted, Is.True);
    }

    [Test]
    public void Expected_choice_config_matching()
    {
        // Given a definition with an expected_choice config
        ObjectiveDefinition defWithChoice = new()
        {
            ObjectiveId = ObjectiveId.NewId(),
            TypeTag = "dialog_choice",
            DisplayText = "Choose wisely",
            TargetTag = "33220e2f",
            RequiredCount = 1,
            Config = new Dictionary<string, object>
            {
                [DialogChoiceObjectiveEvaluator.ExpectedChoiceKey] = "accept_quest"
            }
        };

        ObjectiveState state = new() { ObjectiveId = ObjectiveId.NewId(), IsActive = true };

        // When signal has no matching payload
        QuestSignal signalNoPayload = new(SignalType.DialogChoice, "33220e2f");
        EvaluationResult result1 = _evaluator.Evaluate(signalNoPayload, defWithChoice, state);

        // Then no-op — expected_choice not matched
        Assert.That(result1.StateChanged, Is.False);
    }
}
