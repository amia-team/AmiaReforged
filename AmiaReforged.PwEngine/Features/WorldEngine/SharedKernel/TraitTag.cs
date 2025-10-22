namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;

/// <summary>
/// Value object representing a trait identifier.
/// Prevents primitive obsession and enforces validation rules.
/// </summary>
public readonly record struct TraitTag
{
    public string Value { get; }

    public TraitTag(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("TraitTag cannot be null or whitespace", nameof(value));

        if (value.Length > 50)
            throw new ArgumentException("TraitTag cannot exceed 50 characters", nameof(value));

        Value = value;
    }

    /// <summary>
    /// Implicit conversion from TraitTag to string for backward compatibility.
    /// </summary>
    public static implicit operator string(TraitTag tag) => tag.Value;

    /// <summary>
    /// Explicit conversion from string to TraitTag (requires validation).
    /// </summary>
    public static explicit operator TraitTag(string value) => new(value);

    public override string ToString() => Value;
}
