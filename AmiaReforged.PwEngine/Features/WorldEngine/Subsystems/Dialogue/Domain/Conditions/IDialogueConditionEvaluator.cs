using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.Enums;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.Conditions;

/// <summary>
/// Evaluates a single dialogue condition against the current player state.
/// Implementations are stateless singletons registered by condition type.
/// </summary>
public interface IDialogueConditionEvaluator
{
    /// <summary>
    /// The condition type this evaluator handles.
    /// </summary>
    DialogueConditionType Type { get; }

    /// <summary>
    /// Evaluates whether the condition is satisfied for the given player.
    /// </summary>
    /// <param name="condition">The condition definition to evaluate.</param>
    /// <param name="player">The NWN player in the dialogue.</param>
    /// <param name="characterId">The player's character ID.</param>
    /// <returns>True if the condition is met.</returns>
    Task<bool> EvaluateAsync(DialogueCondition condition, NwPlayer player, Guid characterId);
}
