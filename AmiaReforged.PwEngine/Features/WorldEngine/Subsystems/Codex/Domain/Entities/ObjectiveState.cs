using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.ValueObjects;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Entities;

/// <summary>
/// Mutable runtime state for a single objective within an active quest session.
/// Evaluators read and write this state during signal processing.
/// </summary>
public class ObjectiveState
{
    /// <summary>
    /// The objective this state tracks.
    /// </summary>
    public required ObjectiveId ObjectiveId { get; init; }

    /// <summary>
    /// Current count toward completion (for counter-based objectives like kill/collect).
    /// </summary>
    public int CurrentCount { get; set; }

    /// <summary>
    /// Whether this objective has been completed.
    /// </summary>
    public bool IsCompleted { get; set; }

    /// <summary>
    /// Whether this objective has failed.
    /// </summary>
    public bool IsFailed { get; set; }

    /// <summary>
    /// Whether this objective is currently active (eligible to receive signals).
    /// In Sequence mode, only the current objective in the sequence is active.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Evaluator-specific mutable state (discovered clues, current SM node, escort status, etc.).
    /// Each evaluator manages its own keys within this dictionary.
    /// </summary>
    public Dictionary<string, object> CustomState { get; init; } = new();

    /// <summary>
    /// Retrieves a typed value from custom state, or default if not present.
    /// </summary>
    public T? GetCustom<T>(string key)
    {
        if (!CustomState.TryGetValue(key, out object? value))
            return default;

        if (value is T typed)
            return typed;

        try
        {
            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch
        {
            return default;
        }
    }

    /// <summary>
    /// Sets a value in custom state.
    /// </summary>
    public void SetCustom(string key, object value) => CustomState[key] = value;

    /// <summary>
    /// Whether this objective is in a terminal state (completed or failed).
    /// </summary>
    public bool IsTerminal => IsCompleted || IsFailed;
}
