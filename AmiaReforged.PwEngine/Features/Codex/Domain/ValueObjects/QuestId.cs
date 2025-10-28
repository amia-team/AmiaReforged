namespace AmiaReforged.PwEngine.Features.Codex.Domain.ValueObjects;

/// <summary>
/// Value object representing a unique quest identifier.
/// Prevents primitive obsession and enforces validation rules.
/// </summary>
public readonly record struct QuestId
{
    public string Value { get; }

    public QuestId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("QuestId cannot be null or whitespace", nameof(value));

        if (value.Length > 100)
            throw new ArgumentException("QuestId cannot exceed 100 characters", nameof(value));

        Value = value;
    }

    /// <summary>
    /// Creates a new QuestId with a unique GUID-based identifier.
    /// </summary>
    public static QuestId NewId() => new(Guid.NewGuid().ToString());

    /// <summary>
    /// Implicit conversion from QuestId to string for backward compatibility.
    /// </summary>
    public static implicit operator string(QuestId questId) => questId.Value;

    /// <summary>
    /// Explicit conversion from string to QuestId (requires validation).
    /// </summary>
    public static explicit operator QuestId(string value) => new(value);

    public override string ToString() => Value;
}
