using System.Text.Json.Serialization;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

/// <summary>
/// Value object representing a region identifier/tag.
/// Prevents primitive obsession and enforces validation rules.
/// Tags are normalized to lowercase for case-insensitive comparison.
/// </summary>
[JsonConverter(typeof(RegionTagJsonConverter))]
public readonly record struct RegionTag
{
    public string Value { get; }

    public RegionTag(string value)
    {
        Value = Validate(value);
    }

    private static string Validate(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag))
            throw new ArgumentException("Region tag cannot be empty", nameof(tag));

        if (tag.Length > 100)
            throw new ArgumentException("Region tag cannot exceed 100 characters", nameof(tag));

        return tag.Trim().ToLowerInvariant();
    }

    /// <summary>
    /// Implicit conversion from RegionTag to string for backward compatibility.
    /// </summary>
    public static implicit operator string(RegionTag tag) => tag.Value;

    /// <summary>
    /// Explicit conversion from string to RegionTag (requires validation).
    /// </summary>
    public static explicit operator RegionTag(string value) => new(value);

    public override string ToString() => Value;
}

