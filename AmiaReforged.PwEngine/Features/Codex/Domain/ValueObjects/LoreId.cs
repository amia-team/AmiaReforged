namespace AmiaReforged.PwEngine.Features.Codex.Domain.ValueObjects;

/// <summary>
/// Value object representing a unique lore entry identifier.
/// Prevents primitive obsession and enforces validation rules.
/// </summary>
public readonly record struct LoreId
{
    public string Value { get; }

    public LoreId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("LoreId cannot be null or whitespace", nameof(value));

        if (value.Length > 100)
            throw new ArgumentException("LoreId cannot exceed 100 characters", nameof(value));

        Value = value;
    }

    /// <summary>
    /// Creates a new LoreId with a unique GUID-based identifier.
    /// </summary>
    public static LoreId NewId() => new(Guid.NewGuid().ToString());

    /// <summary>
    /// Implicit conversion from LoreId to string for backward compatibility.
    /// </summary>
    public static implicit operator string(LoreId loreId) => loreId.Value;

    /// <summary>
    /// Explicit conversion from string to LoreId (requires validation).
    /// </summary>
    public static explicit operator LoreId(string value) => new(value);

    public override string ToString() => Value;
}
