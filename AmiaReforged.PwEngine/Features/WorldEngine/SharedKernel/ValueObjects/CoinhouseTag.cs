namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

/// <summary>
/// Value object representing a coinhouse identifier/tag.
/// Prevents primitive obsession and enforces validation rules.
/// Tags are normalized to lowercase for case-insensitive comparison.
/// </summary>
public readonly record struct CoinhouseTag
{
    public string Value { get; }

    public CoinhouseTag(string value)
    {
        Value = Validate(value);
    }

    private static string Validate(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            throw new ArgumentException("Coinhouse tag cannot be empty", nameof(tag));

        if (tag.Length > 100)
            throw new ArgumentException("Coinhouse tag cannot exceed 100 characters", nameof(tag));

        return tag.Trim().ToLowerInvariant();
    }

    /// <summary>
    /// Implicit conversion from CoinhouseTag to string for backward compatibility.
    /// </summary>
    public static implicit operator string(CoinhouseTag tag) => tag.Value;

    /// <summary>
    /// Explicit conversion from string to CoinhouseTag (requires validation).
    /// </summary>
    public static explicit operator CoinhouseTag(string value) => new(value);

    public override string ToString() => Value;
}

