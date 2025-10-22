namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;

/// <summary>
/// Value object representing an industry identifier.
/// Prevents primitive obsession and enforces validation rules.
/// </summary>
public readonly record struct IndustryTag
{
    public string Value { get; }

    public IndustryTag(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("IndustryTag cannot be null or whitespace", nameof(value));

        if (value.Length > 50)
            throw new ArgumentException("IndustryTag cannot exceed 50 characters", nameof(value));

        Value = value;
    }

    /// <summary>
    /// Implicit conversion from IndustryTag to string for backward compatibility.
    /// </summary>
    public static implicit operator string(IndustryTag tag) => tag.Value;

    /// <summary>
    /// Explicit conversion from string to IndustryTag (requires validation).
    /// </summary>
    public static explicit operator IndustryTag(string value) => new(value);

    public override string ToString() => Value;
}
