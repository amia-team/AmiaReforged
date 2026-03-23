using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.Enums;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.Entities;

/// <summary>
/// A precondition that must be satisfied for a dialogue choice to be visible.
/// Evaluated at runtime against the player's state.
/// </summary>
public sealed class DialogueCondition
{
    /// <summary>
    /// The type of condition to evaluate.
    /// </summary>
    public DialogueConditionType Type { get; init; }

    /// <summary>
    /// Key-value parameters specific to the condition type.
    /// For example, HasItem: { "itemTag": "quest_gem", "count": "1" }.
    /// </summary>
    public Dictionary<string, string> Parameters { get; init; } = new();

    /// <summary>
    /// Gets a parameter value or returns null if not present.
    /// </summary>
    public string? GetParam(string key) =>
        Parameters.TryGetValue(key, out string? value) ? value : null;

    /// <summary>
    /// Gets a parameter value or returns a default.
    /// </summary>
    public string GetParamOrDefault(string key, string defaultValue) =>
        Parameters.TryGetValue(key, out string? value) ? value : defaultValue;
}
