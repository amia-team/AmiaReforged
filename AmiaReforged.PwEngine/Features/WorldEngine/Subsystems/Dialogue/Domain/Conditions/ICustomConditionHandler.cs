using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.Entities;
using Anvil.API;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.Conditions;

/// <summary>
/// Interface for named custom condition handlers that can be referenced
/// by the "handlerName" parameter on Custom-type dialogue conditions.
/// <para>
/// Implementations should be decorated with
/// <c>[ServiceBinding(typeof(ICustomConditionHandler))]</c> for auto-registration.
/// </para>
/// </summary>
public interface ICustomConditionHandler
{
    /// <summary>
    /// The unique name of this handler, matched against the "handlerName" parameter.
    /// Matching is case-insensitive.
    /// </summary>
    string HandlerName { get; }

    /// <summary>
    /// Evaluates the custom condition against the current player state.
    /// All parameters from the condition's <see cref="DialogueCondition.Parameters"/>
    /// dictionary are available for the handler to consume.
    /// </summary>
    Task<bool> EvaluateAsync(DialogueCondition condition, NwPlayer player, Guid characterId);
}
