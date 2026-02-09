namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.ValueObjects;

/// <summary>
/// Value object representing a unique faction identifier.
/// Prevents primitive obsession and enforces validation rules.
/// </summary>
public readonly record struct FactionId
{
    public string Value { get; }

    public FactionId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("FactionId cannot be null or whitespace", nameof(value));

        if (value.Length > 50)
            throw new ArgumentException("FactionId cannot exceed 50 characters", nameof(value));

        Value = value;
    }

    /// <summary>
    /// Creates a new FactionId with a unique GUID-based identifier.
    /// </summary>
    public static FactionId NewId() => new(Guid.NewGuid().ToString());

    /// <summary>
    /// Implicit conversion from FactionId to string for backward compatibility.
    /// </summary>
    public static implicit operator string(FactionId factionId) => factionId.Value;

    /// <summary>
    /// Explicit conversion from string to FactionId (requires validation).
    /// </summary>
    public static explicit operator FactionId(string value) => new(value);

    public override string ToString() => Value;
}
