using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.Enums;
using Anvil.API;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.Conditions;

/// <summary>
/// Registry that maps <see cref="DialogueConditionType"/> to its evaluator.
/// Evaluates a list of conditions (all must pass).
/// </summary>
[ServiceBinding(typeof(DialogueConditionRegistry))]
public sealed class DialogueConditionRegistry
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly Dictionary<DialogueConditionType, IDialogueConditionEvaluator> _evaluators = new();

    public DialogueConditionRegistry(IEnumerable<IDialogueConditionEvaluator> evaluators)
    {
        foreach (IDialogueConditionEvaluator evaluator in evaluators)
        {
            _evaluators[evaluator.Type] = evaluator;
            Log.Debug("Registered dialogue condition evaluator: {Type}", evaluator.Type);
        }
    }

    /// <summary>
    /// Evaluates all conditions. Returns true only if every condition passes.
    /// Unknown condition types fail closed (return false).
    /// </summary>
    public async Task<bool> EvaluateAllAsync(
        IReadOnlyList<DialogueCondition> conditions,
        NwPlayer player,
        Guid characterId)
    {
        if (conditions.Count == 0) return true;

        foreach (DialogueCondition condition in conditions)
        {
            if (!_evaluators.TryGetValue(condition.Type, out IDialogueConditionEvaluator? evaluator))
            {
                Log.Warn("No evaluator registered for dialogue condition type: {Type}", condition.Type);
                return false; // Fail closed
            }

            try
            {
                bool result = await evaluator.EvaluateAsync(condition, player, characterId);
                if (!result) return false;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error evaluating dialogue condition {Type}", condition.Type);
                return false; // Fail closed
            }
        }

        return true;
    }
}
