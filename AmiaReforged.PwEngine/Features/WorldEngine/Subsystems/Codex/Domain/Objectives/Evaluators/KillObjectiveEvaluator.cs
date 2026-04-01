using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Entities;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Objectives.Evaluators;

/// <summary>
/// Evaluates kill-count objectives. Listens for <see cref="SignalType.CreatureKilled"/>
/// signals matching the definition's <see cref="ObjectiveDefinition.TargetTag"/>
/// and increments the counter until <see cref="ObjectiveDefinition.RequiredCount"/> is reached.
/// </summary>
[ServiceBinding(typeof(IObjectiveEvaluator))]
public sealed class KillObjectiveEvaluator : IObjectiveEvaluator
{
    public string TypeTag => "kill";

    public void Initialize(ObjectiveDefinition definition, ObjectiveState state)
    {
        // No custom state needed — uses CurrentCount on ObjectiveState directly.
    }

    public EvaluationResult Evaluate(QuestSignal signal, ObjectiveDefinition definition, ObjectiveState state)
    {
        if (state.IsTerminal)
            return EvaluationResult.NoOp();

        if (!signal.Matches(SignalType.CreatureKilled, definition.TargetTag ?? string.Empty))
            return EvaluationResult.NoOp();

        state.CurrentCount++;

        if (state.CurrentCount >= definition.RequiredCount)
        {
            state.IsCompleted = true;
            state.IsActive = false;
            return EvaluationResult.Completed(
                $"Killed {state.CurrentCount}/{definition.RequiredCount} {definition.TargetTag}");
        }

        return EvaluationResult.Progressed(
            $"Killed {state.CurrentCount}/{definition.RequiredCount} {definition.TargetTag}");
    }
}
