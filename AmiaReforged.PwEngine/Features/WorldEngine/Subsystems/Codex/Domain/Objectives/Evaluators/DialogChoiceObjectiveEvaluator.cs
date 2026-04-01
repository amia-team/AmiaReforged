using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Entities;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Objectives.Evaluators;

/// <summary>
/// Evaluates dialog-choice objectives. Completes when the player makes
/// a specific dialog choice identified by the target tag.
/// Supports requiring a specific choice key via the signal payload.
/// </summary>
[ServiceBinding(typeof(IObjectiveEvaluator))]
public sealed class DialogChoiceObjectiveEvaluator : IObjectiveEvaluator
{
    public string TypeTag => "dialog_choice";

    /// <summary>Config key for the expected choice key value.</summary>
    public const string ExpectedChoiceKey = "expected_choice";

    public void Initialize(ObjectiveDefinition definition, ObjectiveState state)
    {
        // No custom state needed.
    }

    public EvaluationResult Evaluate(QuestSignal signal, ObjectiveDefinition definition, ObjectiveState state)
    {
        if (state.IsTerminal)
            return EvaluationResult.NoOp();

        if (signal.SignalType != SignalType.DialogChoice)
            return EvaluationResult.NoOp();

        // Match the dialog/NPC tag
        string targetTag = definition.TargetTag ?? string.Empty;
        if (!string.Equals(signal.TargetTag, targetTag, StringComparison.OrdinalIgnoreCase))
            return EvaluationResult.NoOp();

        // Optionally match a specific choice key
        string? expectedChoice = definition.GetConfig<string>(ExpectedChoiceKey);
        if (!string.IsNullOrEmpty(expectedChoice))
        {
            string? actualChoice = signal.GetPayload<string>("choice_key");
            if (!string.Equals(actualChoice, expectedChoice, StringComparison.OrdinalIgnoreCase))
                return EvaluationResult.NoOp();
        }

        state.IsCompleted = true;
        state.IsActive = false;
        return EvaluationResult.Completed("Dialog choice made");
    }
}
