using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Entities;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Objectives.Evaluators;

/// <summary>
/// Evaluates item-collection objectives. Listens for <see cref="SignalType.ItemAcquired"/>
/// signals matching the definition's target tag and increments the counter.
/// Also listens for <see cref="SignalType.ItemLost"/> to decrement if configured.
/// </summary>
public sealed class CollectObjectiveEvaluator : IObjectiveEvaluator
{
    public string TypeTag => "collect";

    /// <summary>Config key: when true, losing the item decrements progress.</summary>
    public const string TrackLossKey = "track_loss";

    public void Initialize(ObjectiveDefinition definition, ObjectiveState state)
    {
        // No custom state needed.
    }

    public EvaluationResult Evaluate(QuestSignal signal, ObjectiveDefinition definition, ObjectiveState state)
    {
        if (state.IsTerminal)
            return EvaluationResult.NoOp();

        string targetTag = definition.TargetTag ?? string.Empty;

        // Handle item acquisition
        if (signal.Matches(SignalType.ItemAcquired, targetTag))
        {
            state.CurrentCount++;

            if (state.CurrentCount >= definition.RequiredCount)
            {
                state.IsCompleted = true;
                state.IsActive = false;
                return EvaluationResult.Completed(
                    $"Collected {state.CurrentCount}/{definition.RequiredCount} {targetTag}");
            }

            return EvaluationResult.Progressed(
                $"Collected {state.CurrentCount}/{definition.RequiredCount} {targetTag}");
        }

        // Handle item loss (if tracking is enabled)
        bool trackLoss = definition.GetConfig<bool>(TrackLossKey);
        if (trackLoss && signal.Matches(SignalType.ItemLost, targetTag))
        {
            if (state.CurrentCount > 0)
            {
                state.CurrentCount--;
                return EvaluationResult.Progressed(
                    $"Lost item — now {state.CurrentCount}/{definition.RequiredCount} {targetTag}");
            }
        }

        return EvaluationResult.NoOp();
    }
}
