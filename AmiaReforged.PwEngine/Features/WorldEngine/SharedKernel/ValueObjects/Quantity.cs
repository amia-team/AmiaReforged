namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

/// <summary>
/// Value object representing a quantity of items, resources, or currency.
/// Prevents primitive obsession and enforces non-negative constraint.
/// </summary>
public readonly record struct Quantity
{
    public int Value { get; }

    private Quantity(int value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a Quantity from an integer value.
    /// </summary>
    /// <param name="value">The quantity value (must be non-negative)</param>
    /// <returns>A validated Quantity</returns>
    /// <exception cref="ArgumentException">Thrown when value is negative</exception>
    public static Quantity Parse(int value) =>
        value >= 0
            ? new Quantity(value)
            : throw new ArgumentException("Quantity cannot be negative", nameof(value));

    /// <summary>
    /// Represents zero quantity.
    /// </summary>
    public static Quantity Zero => new(0);

    /// <summary>
    /// Adds another quantity to this one.
    /// </summary>
    /// <param name="other">The quantity to add</param>
    /// <returns>A new Quantity representing the sum</returns>
    public Quantity Add(Quantity other) => new(Value + other.Value);

    /// <summary>
    /// Subtracts another quantity from this one.
    /// </summary>
    /// <param name="other">The quantity to subtract</param>
    /// <returns>A new Quantity representing the difference</returns>
    /// <exception cref="ArgumentException">Thrown when result would be negative</exception>
    public Quantity Subtract(Quantity other) => Parse(Value - other.Value);

    /// <summary>
    /// Multiplies this quantity by a factor.
    /// </summary>
    /// <param name="factor">The multiplication factor</param>
    /// <returns>A new Quantity representing the product</returns>
    public Quantity Multiply(int factor) => Parse(Value * factor);

    /// <summary>
    /// Checks if this quantity is greater than another.
    /// </summary>
    public bool IsGreaterThan(Quantity other) => Value > other.Value;

    /// <summary>
    /// Checks if this quantity is less than another.
    /// </summary>
    public bool IsLessThan(Quantity other) => Value < other.Value;

    /// <summary>
    /// Checks if this quantity is greater than or equal to another.
    /// </summary>
    public bool IsGreaterThanOrEqualTo(Quantity other) => Value >= other.Value;

    /// <summary>
    /// Checks if this quantity is less than or equal to another.
    /// </summary>
    public bool IsLessThanOrEqualTo(Quantity other) => Value <= other.Value;

    /// <summary>
    /// Addition operator for quantities.
    /// </summary>
    public static Quantity operator +(Quantity left, Quantity right) => left.Add(right);

    /// <summary>
    /// Subtraction operator for quantities.
    /// </summary>
    public static Quantity operator -(Quantity left, Quantity right) => left.Subtract(right);

    /// <summary>
    /// Multiplication operator for quantity and integer.
    /// </summary>
    public static Quantity operator *(Quantity quantity, int factor) => quantity.Multiply(factor);

    /// <summary>
    /// Multiplication operator for integer and quantity.
    /// </summary>
    public static Quantity operator *(int factor, Quantity quantity) => quantity.Multiply(factor);

    /// <summary>
    /// Implicit conversion from Quantity to int for backward compatibility.
    /// </summary>
    public static implicit operator int(Quantity quantity) => quantity.Value;

    /// <summary>
    /// Explicit conversion from int to Quantity (requires validation).
    /// </summary>
    public static explicit operator Quantity(int value) => Parse(value);

    public override string ToString() => Value.ToString();
}

