namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.ValueObjects;

/// <summary>
/// Value object representing a unique objective identifier within a quest.
/// Prevents primitive obsession and enforces validation rules.
/// </summary>
public readonly record struct ObjectiveId
{
    public string Value { get; }

    public ObjectiveId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("ObjectiveId cannot be null or whitespace", nameof(value));

        if (value.Length > 100)
            throw new ArgumentException("ObjectiveId cannot exceed 100 characters", nameof(value));

        Value = value;
    }

    /// <summary>
    /// Creates a new ObjectiveId with a unique GUID-based identifier.
    /// </summary>
    public static ObjectiveId NewId() => new(Guid.NewGuid().ToString());

    /// <summary>
    /// Implicit conversion from ObjectiveId to string for convenience.
    /// </summary>
    public static implicit operator string(ObjectiveId objectiveId) => objectiveId.Value;

    /// <summary>
    /// Explicit conversion from string to ObjectiveId (requires validation).
    /// </summary>
    public static explicit operator ObjectiveId(string value) => new(value);

    public override string ToString() => Value;
}
