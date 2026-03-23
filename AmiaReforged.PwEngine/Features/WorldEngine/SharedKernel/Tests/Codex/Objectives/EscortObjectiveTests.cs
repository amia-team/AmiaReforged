using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Objectives;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Objectives.Evaluators;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.ValueObjects;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Tests.Codex.Objectives;

[TestFixture]
public class EscortObjectiveTests
{
    private EscortObjectiveEvaluator _evaluator = null!;

    [SetUp]
    public void SetUp()
    {
        _evaluator = new EscortObjectiveEvaluator();
    }

    #region Waypoint Tracking

    [Test]
    public void Reaching_intermediate_waypoint_reports_progress()
    {
        // Given an escort objective for "merchant_npc" to "town_gate"
        ObjectiveDefinition definition = CreateEscortDefinition("merchant_npc", "town_gate");
        ObjectiveState state = CreateState(definition);

        // When the NPC reaches a waypoint
        QuestSignal signal = new(SignalType.WaypointReached, "merchant_npc",
            new Dictionary<string, object>
            {
                ["npc_tag"] = "merchant_npc",
                ["waypoint"] = "crossroads"
            });
        EvaluationResult result = _evaluator.Evaluate(signal, definition, state);

        // Then progress is reported
        Assert.That(result.StateChanged, Is.True);
        Assert.That(result.IsCompleted, Is.False);
        Assert.That(state.CurrentCount, Is.EqualTo(1));
    }

    [Test]
    public void Reaching_destination_completes_escort()
    {
        // Given an escort objective for "merchant_npc" to "town_gate"
        ObjectiveDefinition definition = CreateEscortDefinition("merchant_npc", "town_gate");
        ObjectiveState state = CreateState(definition);

        // When the NPC reaches the destination
        QuestSignal signal = new(SignalType.WaypointReached, "merchant_npc",
            new Dictionary<string, object>
            {
                ["npc_tag"] = "merchant_npc",
                ["waypoint"] = "town_gate"
            });
        EvaluationResult result = _evaluator.Evaluate(signal, definition, state);

        // Then the escort completes
        Assert.That(result.IsCompleted, Is.True);
        Assert.That(state.IsCompleted, Is.True);
    }

    #endregion

    #region NPC Death

    [Test]
    public void Npc_death_fails_escort_when_fail_on_death_enabled()
    {
        // Given an escort objective with default fail-on-death
        ObjectiveDefinition definition = CreateEscortDefinition("merchant_npc", "town_gate");
        ObjectiveState state = CreateState(definition);

        // When the NPC is killed
        QuestSignal signal = new(SignalType.CreatureKilled, "merchant_npc");
        EvaluationResult result = _evaluator.Evaluate(signal, definition, state);

        // Then the escort fails
        Assert.That(result.IsFailed, Is.True);
        Assert.That(state.IsFailed, Is.True);
        Assert.That(state.IsActive, Is.False);
    }

    [Test]
    public void Npc_death_does_not_fail_escort_when_fail_on_death_disabled()
    {
        // Given an escort objective where NPC death is tolerated
        ObjectiveDefinition definition = CreateEscortDefinition("merchant_npc", "town_gate", failOnDeath: false);
        ObjectiveState state = CreateState(definition);

        // When the NPC is killed
        QuestSignal signal = new(SignalType.CreatureKilled, "merchant_npc");
        EvaluationResult result = _evaluator.Evaluate(signal, definition, state);

        // Then the escort continues (no state change from this signal)
        Assert.That(result.StateChanged, Is.False);
        Assert.That(state.IsFailed, Is.False);
    }

    [Test]
    public void Npc_status_dead_fails_escort()
    {
        // Given an escort objective
        ObjectiveDefinition definition = CreateEscortDefinition("merchant_npc", "town_gate");
        ObjectiveState state = CreateState(definition);

        // When NPC status changes to dead
        QuestSignal signal = new(SignalType.NpcStatusChanged, "merchant_npc",
            new Dictionary<string, object> { ["status"] = "dead" });
        EvaluationResult result = _evaluator.Evaluate(signal, definition, state);

        // Then the escort fails
        Assert.That(result.IsFailed, Is.True);
    }

    #endregion

    #region Signal Filtering

    [Test]
    public void Waypoint_for_different_npc_is_ignored()
    {
        // Given an escort for "merchant_npc"
        ObjectiveDefinition definition = CreateEscortDefinition("merchant_npc", "town_gate");
        ObjectiveState state = CreateState(definition);

        // When a DIFFERENT NPC reaches a waypoint
        QuestSignal signal = new(SignalType.WaypointReached, "bandit_npc",
            new Dictionary<string, object>
            {
                ["npc_tag"] = "bandit_npc",
                ["waypoint"] = "town_gate"
            });
        EvaluationResult result = _evaluator.Evaluate(signal, definition, state);

        // Then nothing happens
        Assert.That(result.StateChanged, Is.False);
    }

    [Test]
    public void Signals_after_failure_are_ignored()
    {
        // Given a failed escort
        ObjectiveDefinition definition = CreateEscortDefinition("merchant_npc", "town_gate");
        ObjectiveState state = CreateState(definition);
        _evaluator.Evaluate(new QuestSignal(SignalType.CreatureKilled, "merchant_npc"), definition, state);
        Assert.That(state.IsFailed, Is.True);

        // When a waypoint signal arrives
        QuestSignal signal = new(SignalType.WaypointReached, "merchant_npc",
            new Dictionary<string, object>
            {
                ["npc_tag"] = "merchant_npc",
                ["waypoint"] = "town_gate"
            });
        EvaluationResult result = _evaluator.Evaluate(signal, definition, state);

        // Then it's ignored
        Assert.That(result.StateChanged, Is.False);
    }

    #endregion

    #region Helpers

    private static ObjectiveDefinition CreateEscortDefinition(
        string npcTag, string destination, bool failOnDeath = true)
    {
        Dictionary<string, object> config = new()
        {
            [EscortObjectiveEvaluator.NpcTagKey] = npcTag,
            [EscortObjectiveEvaluator.DestinationKey] = destination,
            [EscortObjectiveEvaluator.FailOnDeathKey] = failOnDeath
        };

        return new ObjectiveDefinition
        {
            ObjectiveId = ObjectiveId.NewId(),
            TypeTag = "escort",
            DisplayText = $"Escort {npcTag} to {destination}",
            TargetTag = npcTag,
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
