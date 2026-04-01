using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Entities;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Objectives.Evaluators;

/// <summary>
/// Evaluates reach-location objectives. Completes when the player enters
/// an area whose resref matches the definition's target tag.
/// </summary>
[ServiceBinding(typeof(IObjectiveEvaluator))]
public sealed class ReachLocationObjectiveEvaluator : IObjectiveEvaluator
{
    public string TypeTag => "reach_location";

    public void Initialize(ObjectiveDefinition definition, ObjectiveState state)
    {
        // No custom state needed.
    }

    public EvaluationResult Evaluate(QuestSignal signal, ObjectiveDefinition definition, ObjectiveState state)
    {
        if (state.IsTerminal)
            return EvaluationResult.NoOp();

        if (!signal.Matches(SignalType.AreaEntered, definition.TargetTag ?? string.Empty))
            return EvaluationResult.NoOp();

        state.IsCompleted = true;
        state.IsActive = false;
        return EvaluationResult.Completed($"Reached {definition.TargetTag}");
    }
}
