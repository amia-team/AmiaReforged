namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Objectives;

/// <summary>
/// A lightweight, NWN-agnostic record representing a game event relevant to quest objectives.
/// NWN event hooks translate engine events (OnCreatureDeath, OnItemAcquired, etc.) into
/// QuestSignal instances that the domain layer can process without NWN dependencies.
/// </summary>
/// <param name="SignalType">The type of signal (see <see cref="Objectives.SignalType"/> constants).</param>
/// <param name="TargetTag">The tag of the entity involved (creature tag, item tag, area resref, etc.).</param>
/// <param name="Payload">Optional evaluator-specific data (e.g., dialog choice key, NPC health, waypoint id).</param>
public sealed record QuestSignal(
    string SignalType,
    string TargetTag,
    Dictionary<string, object>? Payload = null)
{
    /// <summary>
    /// Retrieves a typed value from the payload, or default if not present.
    /// </summary>
    public T? GetPayload<T>(string key)
    {
        if (Payload == null || !Payload.TryGetValue(key, out object? value))
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
    /// Checks if the signal matches the given type and tag (case-insensitive tag comparison).
    /// </summary>
    public bool Matches(string signalType, string targetTag)
        => SignalType == signalType &&
           string.Equals(TargetTag, targetTag, StringComparison.OrdinalIgnoreCase);
}
