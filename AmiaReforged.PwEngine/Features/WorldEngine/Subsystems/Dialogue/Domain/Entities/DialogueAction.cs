using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.Enums;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.Entities;

/// <summary>
/// An action fired when a dialogue node is entered or a choice is selected.
/// Dispatched as a World Engine command at runtime.
/// </summary>
public sealed class DialogueAction
{
    /// <summary>
    /// The type of action to execute.
    /// </summary>
    public DialogueActionType ActionType { get; init; }

    /// <summary>
    /// Key-value parameters specific to the action type.
    /// For example, StartQuest: { "questId": "q_lost_artifact" }.
    /// </summary>
    public Dictionary<string, string> Parameters { get; init; } = new();

    /// <summary>
    /// Execution order when multiple actions are on the same node.
    /// Lower values execute first.
    /// </summary>
    public int ExecutionOrder { get; init; }

    /// <summary>
    /// Gets a parameter value or returns null if not present.
    /// </summary>
    public string? GetParam(string key) =>
        Parameters.TryGetValue(key, out string? value) ? value : null;

    /// <summary>
    /// Gets a required parameter, throwing if not present.
    /// </summary>
    public string GetRequiredParam(string key) =>
        Parameters.TryGetValue(key, out string? value)
            ? value
            : throw new InvalidOperationException(
                $"Dialogue action of type {ActionType} is missing required parameter '{key}'");
}
