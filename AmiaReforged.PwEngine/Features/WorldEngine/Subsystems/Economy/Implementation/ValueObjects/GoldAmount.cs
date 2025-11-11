namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.ValueObjects;

/// <summary>
/// Value object representing an amount of gold.
/// Enforces non-negative values and provides arithmetic operations.
/// </summary>
public readonly record struct GoldAmount
{
    public int Value { get; }

    private GoldAmount(int value)
    {
        if (value < 0)
            throw new ArgumentException($"Gold amount cannot be negative. Got: {value}", nameof(value));

        Value = value;
    }

    /// <summary>
    /// Creates a GoldAmount from an integer, validating it's non-negative.
    /// </summary>
    public static GoldAmount Parse(int value) => new(value);

    /// <summary>
    /// Zero gold amount.
    /// </summary>
    public static GoldAmount Zero => new(0);

    /// <summary>
    /// Adds two gold amounts.
    /// </summary>
    public GoldAmount Add(GoldAmount other) => new(Value + other.Value);

    /// <summary>
    /// Subtracts a gold amount, ensuring result is non-negative.
    /// </summary>
    public GoldAmount Subtract(GoldAmount other)
    {
        int result = Value - other.Value;
        if (result < 0)
            throw new InvalidOperationException(
                $"Cannot subtract {other.Value} from {Value} - would result in negative amount");

        return new(result);
    }

    /// <summary>
    /// Checks if this amount is greater than or equal to another amount.
    /// </summary>
    public bool IsGreaterThanOrEqualTo(GoldAmount other) => Value >= other.Value;

    /// <summary>
    /// Checks if this amount is sufficient to cover another amount.
    /// </summary>
    public bool CanAfford(GoldAmount cost) => Value >= cost.Value;

    /// <summary>
    /// Implicit conversion to int for convenience.
    /// </summary>
    public static implicit operator int(GoldAmount amount) => amount.Value;

    /// <summary>
    /// Explicit conversion from int (use Parse for clarity).
    /// </summary>
    public static explicit operator GoldAmount(int value) => Parse(value);

    public override string ToString() => $"{Value} gold";
}

