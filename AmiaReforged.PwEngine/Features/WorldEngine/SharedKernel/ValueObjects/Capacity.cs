namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

/// <summary>
/// Value object representing a capacity (storage, inventory, etc.).
/// Prevents primitive obsession and enforces non-negative constraint.
/// </summary>
public readonly record struct Capacity
{
    public int Value { get; }

    private Capacity(int value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a Capacity from an integer value.
    /// </summary>
    /// <param name="value">The capacity value (must be non-negative)</param>
    /// <returns>A validated Capacity</returns>
    /// <exception cref="ArgumentException">Thrown when value is negative</exception>
    public static Capacity Parse(int value) =>
        value >= 0
            ? new Capacity(value)
            : throw new ArgumentException("Capacity cannot be negative", nameof(value));

    /// <summary>
    /// Represents zero capacity.
    /// </summary>
    public static Capacity Zero => new(0);

    /// <summary>
    /// Checks if this capacity can accept the specified amount.
    /// </summary>
    /// <param name="amount">The amount to check</param>
    /// <returns>True if the capacity can hold the amount, false otherwise</returns>
    public bool CanAccept(int amount) => Value >= amount;

    /// <summary>
    /// Checks if this capacity can accept the specified quantity.
    /// </summary>
    /// <param name="quantity">The quantity to check</param>
    /// <returns>True if the capacity can hold the quantity, false otherwise</returns>
    public bool CanAccept(Quantity quantity) => Value >= quantity.Value;

    /// <summary>
    /// Implicit conversion from Capacity to int for backward compatibility.
    /// </summary>
    public static implicit operator int(Capacity capacity) => capacity.Value;

    /// <summary>
    /// Explicit conversion from int to Capacity (requires validation).
    /// </summary>
    public static explicit operator Capacity(int value) => Parse(value);

    public override string ToString() => Value.ToString();
}

