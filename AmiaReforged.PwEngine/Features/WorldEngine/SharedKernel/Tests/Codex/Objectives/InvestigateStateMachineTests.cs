using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Objectives;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Objectives.Evaluators;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Objectives.Models;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.ValueObjects;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Tests.Codex.Objectives;

[TestFixture]
public class InvestigateStateMachineTests
{
    private InvestigateObjectiveEvaluator _evaluator = null!;

    [SetUp]
    public void SetUp()
    {
        _evaluator = new InvestigateObjectiveEvaluator();
    }

    #region State Transitions

    [Test]
    public void Matching_signal_transitions_to_next_state()
    {
        // Given a state machine investigation at the initial state
        (ObjectiveDefinition definition, ObjectiveState state) = CreateWhodunnitStateMachine();

        // When the player makes a dialog choice that triggers a transition
        QuestSignal signal = new(SignalType.DialogChoice, "accuse_butler");
        EvaluationResult result = _evaluator.Evaluate(signal, definition, state);

        // Then the state transitions
        Assert.That(result.StateChanged, Is.True);
        Assert.That(result.IsCompleted, Is.False);
        Assert.That(state.GetCustom<string>("current_sm_state"), Is.EqualTo("accused_butler"));
    }

    [Test]
    public void Non_matching_signal_is_ignored()
    {
        // Given a state machine investigation
        (ObjectiveDefinition definition, ObjectiveState state) = CreateWhodunnitStateMachine();

        // When a signal that doesn't match any transition arrives
        QuestSignal signal = new(SignalType.DialogChoice, "talk_about_weather");
        EvaluationResult result = _evaluator.Evaluate(signal, definition, state);

        // Then nothing happens
        Assert.That(result.StateChanged, Is.False);
    }

    [Test]
    public void Transition_from_wrong_state_is_ignored()
    {
        // Given a state machine investigation at the initial state
        (ObjectiveDefinition definition, ObjectiveState state) = CreateWhodunnitStateMachine();

        // When a signal matching a transition from a DIFFERENT state arrives
        QuestSignal signal = new(SignalType.DialogChoice, "present_evidence");
        EvaluationResult result = _evaluator.Evaluate(signal, definition, state);

        // Then nothing happens (that transition is from "accused_butler", not "investigating")
        Assert.That(result.StateChanged, Is.False);
    }

    #endregion

    #region Terminal States

    [Test]
    public void Reaching_terminal_success_state_completes_objective()
    {
        // Given a state machine investigation
        (ObjectiveDefinition definition, ObjectiveState state) = CreateWhodunnitStateMachine();

        // When following the success path
        _evaluator.Evaluate(new QuestSignal(SignalType.DialogChoice, "accuse_butler"), definition, state);
        EvaluationResult result = _evaluator.Evaluate(
            new QuestSignal(SignalType.DialogChoice, "present_evidence"), definition, state);

        // Then the objective completes
        Assert.That(result.IsCompleted, Is.True);
        Assert.That(state.IsCompleted, Is.True);
        Assert.That(state.IsActive, Is.False);
    }

    [Test]
    public void Reaching_terminal_failure_state_fails_objective()
    {
        // Given a state machine investigation
        (ObjectiveDefinition definition, ObjectiveState state) = CreateWhodunnitStateMachine();

        // When following the failure path
        _evaluator.Evaluate(new QuestSignal(SignalType.DialogChoice, "accuse_butler"), definition, state);
        EvaluationResult result = _evaluator.Evaluate(
            new QuestSignal(SignalType.DialogChoice, "accuse_wrong_person"), definition, state);

        // Then the objective fails
        Assert.That(result.IsFailed, Is.True);
        Assert.That(state.IsFailed, Is.True);
        Assert.That(state.IsActive, Is.False);
    }

    [Test]
    public void Signals_after_terminal_state_are_ignored()
    {
        // Given a completed state machine investigation
        (ObjectiveDefinition definition, ObjectiveState state) = CreateWhodunnitStateMachine();
        _evaluator.Evaluate(new QuestSignal(SignalType.DialogChoice, "accuse_butler"), definition, state);
        _evaluator.Evaluate(new QuestSignal(SignalType.DialogChoice, "present_evidence"), definition, state);
        Assert.That(state.IsCompleted, Is.True);

        // When another signal arrives
        EvaluationResult result = _evaluator.Evaluate(
            new QuestSignal(SignalType.DialogChoice, "accuse_butler"), definition, state);

        // Then it's ignored
        Assert.That(result.StateChanged, Is.False);
    }

    #endregion

    #region Helpers

    /// <summary>
    /// Creates a whodunnit state machine:
    ///   investigating → (accuse_butler) → accused_butler → (present_evidence) → solved [SUCCESS]
    ///                                   → (accuse_wrong_person) → wrong_accusation [FAILURE]
    /// </summary>
    private (ObjectiveDefinition, ObjectiveState) CreateWhodunnitStateMachine()
    {
        StateMachineDefinition sm = new()
        {
            States =
            [
                new NarrativeState { StateId = "investigating", Description = "Investigating the murder" },
                new NarrativeState { StateId = "accused_butler", Description = "Accused the butler" },
                new NarrativeState
                {
                    StateId = "solved", Description = "Case solved!", IsTerminalSuccess = true
                },
                new NarrativeState
                {
                    StateId = "wrong_accusation", Description = "Wrong person accused",
                    IsTerminalFailure = true
                }
            ],
            Transitions =
            [
                new NarrativeTransition
                {
                    FromStateId = "investigating", ToStateId = "accused_butler",
                    SignalType = SignalType.DialogChoice, TargetTag = "accuse_butler",
                    TransitionText = "You confront the butler"
                },
                new NarrativeTransition
                {
                    FromStateId = "accused_butler", ToStateId = "solved",
                    SignalType = SignalType.DialogChoice, TargetTag = "present_evidence",
                    TransitionText = "The evidence is conclusive!"
                },
                new NarrativeTransition
                {
                    FromStateId = "accused_butler", ToStateId = "wrong_accusation",
                    SignalType = SignalType.DialogChoice, TargetTag = "accuse_wrong_person",
                    TransitionText = "The real culprit escapes"
                }
            ],
            InitialStateId = "investigating"
        };

        ObjectiveDefinition definition = new()
        {
            ObjectiveId = ObjectiveId.NewId(),
            TypeTag = "investigate",
            DisplayText = "Solve the murder mystery",
            Config = new Dictionary<string, object>
            {
                [InvestigateObjectiveEvaluator.ModeKey] = "state_machine",
                [InvestigateObjectiveEvaluator.StateMachineKey] = sm
            }
        };

        ObjectiveState state = new() { ObjectiveId = definition.ObjectiveId };
        _evaluator.Initialize(definition, state);
        return (definition, state);
    }

    #endregion
}
