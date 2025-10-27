namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

/// <summary>
/// Value object representing an industry code/identifier.
/// Prevents primitive obsession and enforces validation rules.
/// Codes are normalized to lowercase for case-insensitive comparison.
/// </summary>
public readonly record struct IndustryCode
{
    public string Value { get; }

    public IndustryCode(string value)
    {
        Value = Validate(value);
    }

    private static string Validate(string code)
    {
        if (string.IsNullOrWhiteSpace(code))
            throw new ArgumentException("Industry code cannot be empty", nameof(code));

        if (code.Length > 50)
            throw new ArgumentException("Industry code cannot exceed 50 characters", nameof(code));

        return code.Trim().ToLowerInvariant();
    }

    /// <summary>
    /// Implicit conversion from IndustryCode to string for backward compatibility.
    /// </summary>
    public static implicit operator string(IndustryCode code) => code.Value;

    /// <summary>
    /// Explicit conversion from string to IndustryCode (requires validation).
    /// </summary>
    public static explicit operator IndustryCode(string value) => new(value);

    public override string ToString() => Value;
}

