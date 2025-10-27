using System.Text.Json.Serialization;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

/// <summary>
/// Value object representing an area identifier/tag.
/// Prevents primitive obsession and enforces validation rules.
/// Tags are normalized to lowercase for case-insensitive comparison.
/// </summary>
[JsonConverter(typeof(AreaTagJsonConverter))]
public readonly record struct AreaTag
{
    public string Value { get; }

    public AreaTag(string value)
    {
        Value = Validate(value);
    }

    private static string Validate(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            throw new ArgumentException("Area tag cannot be empty", nameof(tag));

        if (tag.Length > 100)
            throw new ArgumentException("Area tag cannot exceed 100 characters", nameof(tag));

        return tag.Trim().ToLowerInvariant();
    }

    /// <summary>
    /// Implicit conversion from AreaTag to string for backward compatibility.
    /// </summary>
    public static implicit operator string(AreaTag tag) => tag.Value;

    /// <summary>
    /// Explicit conversion from string to AreaTag (requires validation).
    /// </summary>
    public static explicit operator AreaTag(string value) => new(value);

    public override string ToString() => Value;
}

