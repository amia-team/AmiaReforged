using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Entities;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Objectives.Evaluators;

/// <summary>
/// Evaluates "speak to NPC" / dialog-choice objectives.
/// Completes when the player's conversation enters a dialogue node whose
/// truncated ID (first 8 hex chars) matches the definition's <see cref="ObjectiveDefinition.TargetTag"/>.
///
/// <para>
/// <b>How it works:</b> When the dialogue system enters a node, it publishes a
/// <c>DialogueNodeEnteredEvent</c>. The resolution service translates this into a
/// <c>DialogChoice</c> signal whose <c>TargetTag</c> is the node's short ID (e.g. "33220e2f").
/// An objective definition with <c>TypeTag = "dialog_choice"</c> and <c>TargetTag = "33220e2f"</c>
/// will complete when that node is entered.
/// </para>
///
/// Optionally supports an <c>expected_choice</c> config key for additional payload matching.
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
