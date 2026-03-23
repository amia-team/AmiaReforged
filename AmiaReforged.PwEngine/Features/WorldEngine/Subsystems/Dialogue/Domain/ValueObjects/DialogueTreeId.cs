namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Domain.ValueObjects;

/// <summary>
/// Value object representing a unique dialogue tree identifier.
/// Prevents primitive obsession and enforces validation rules.
/// </summary>
public readonly record struct DialogueTreeId
{
    public string Value { get; }

    public DialogueTreeId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("DialogueTreeId cannot be null or whitespace", nameof(value));

        if (value.Length > 100)
            throw new ArgumentException("DialogueTreeId cannot exceed 100 characters", nameof(value));

        Value = value;
    }

    /// <summary>
    /// Creates a new DialogueTreeId with a unique GUID-based identifier.
    /// </summary>
    public static DialogueTreeId NewId() => new(Guid.NewGuid().ToString());

    /// <summary>
    /// Implicit conversion from DialogueTreeId to string for backward compatibility.
    /// </summary>
    public static implicit operator string(DialogueTreeId id) => id.Value;

    /// <summary>
    /// Explicit conversion from string to DialogueTreeId (requires validation).
    /// </summary>
    public static explicit operator DialogueTreeId(string value) => new(value);

    public override string ToString() => Value;
}
