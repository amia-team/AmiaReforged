namespace AmiaReforged.PwEngine.Features.Codex.Domain.ValueObjects;

/// <summary>
/// Value object representing a search keyword.
/// Prevents primitive obsession and enforces validation rules.
/// </summary>
public readonly record struct Keyword
{
    public string Value { get; }

    public Keyword(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Keyword cannot be null or whitespace", nameof(value));

        if (value.Length > 50)
            throw new ArgumentException("Keyword cannot exceed 50 characters", nameof(value));

        Value = value.Trim().ToLowerInvariant();
    }

    /// <summary>
    /// Checks if this keyword matches the search term (case-insensitive).
    /// </summary>
    public bool Matches(string searchTerm) =>
        Value.Contains(searchTerm.ToLowerInvariant(), StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Implicit conversion from Keyword to string for backward compatibility.
    /// </summary>
    public static implicit operator string(Keyword keyword) => keyword.Value;

    /// <summary>
    /// Explicit conversion from string to Keyword (requires validation).
    /// </summary>
    public static explicit operator Keyword(string value) => new(value);

    public override string ToString() => Value;
}
