namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.ValueObjects;

/// <summary>
/// Value object representing a transaction reason/description.
/// Enforces non-empty, reasonable length strings.
/// </summary>
public readonly record struct TransactionReason
{
    public const int MaxLength = 200;
    public const int MinLength = 3;

    public string Value { get; }

    private TransactionReason(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Transaction reason cannot be empty", nameof(value));

        string trimmed = value.Trim();

        if (trimmed.Length < MinLength)
            throw new ArgumentException(
                $"Transaction reason must be at least {MinLength} characters. Got: {trimmed.Length}",
                nameof(value));

        if (trimmed.Length > MaxLength)
            throw new ArgumentException(
                $"Transaction reason cannot exceed {MaxLength} characters. Got: {trimmed.Length}",
                nameof(value));

        Value = trimmed;
    }

    /// <summary>
    /// Creates a TransactionReason, validating the input.
    /// </summary>
    public static TransactionReason Parse(string value) => new(value);

    /// <summary>
    /// Tries to create a TransactionReason, returning null if invalid.
    /// </summary>
    public static TransactionReason? TryParse(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        try
        {
            return new TransactionReason(value);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Implicit conversion to string for convenience.
    /// </summary>
    public static implicit operator string(TransactionReason reason) => reason.Value;

    public override string ToString() => Value;
}

