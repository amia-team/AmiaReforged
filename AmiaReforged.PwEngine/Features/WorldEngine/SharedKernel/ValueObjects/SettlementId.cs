using System.Text.Json.Serialization;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

/// <summary>
/// Value object representing a unique settlement identifier.
/// Prevents primitive obsession and enforces validation rules.
/// </summary>
[JsonConverter(typeof(SettlementIdJsonConverter))]
public readonly record struct SettlementId
{
    public int Value { get; }

    private SettlementId(int value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a SettlementId from an integer value.
    /// </summary>
    /// <param name="value">The settlement ID value (must be positive)</param>
    /// <returns>A validated SettlementId</returns>
    /// <exception cref="ArgumentException">Thrown when value is not positive</exception>
    public static SettlementId Parse(int value) =>
        value > 0
            ? new SettlementId(value)
            : throw new ArgumentException("Settlement ID must be positive", nameof(value));

    /// <summary>
    /// Implicit conversion from SettlementId to int for backward compatibility.
    /// </summary>
    public static implicit operator int(SettlementId settlementId) => settlementId.Value;

    /// <summary>
    /// Explicit conversion from int to SettlementId (requires validation).
    /// </summary>
    public static explicit operator SettlementId(int value) => Parse(value);

    public override string ToString() => Value.ToString();
}

