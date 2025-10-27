namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

/// <summary>
/// Value object representing a unique resource node identifier.
/// Prevents primitive obsession and enforces validation rules.
/// </summary>
public readonly record struct ResourceNodeId
{
    public int Value { get; }

    private ResourceNodeId(int value)
    {
        Value = value;
    }

    /// <summary>
    /// Creates a ResourceNodeId from an integer value.
    /// </summary>
    /// <param name="value">The resource node ID value (must be positive)</param>
    /// <returns>A validated ResourceNodeId</returns>
    /// <exception cref="ArgumentException">Thrown when value is not positive</exception>
    public static ResourceNodeId Parse(int value) =>
        value > 0
            ? new ResourceNodeId(value)
            : throw new ArgumentException("Resource node ID must be positive", nameof(value));

    /// <summary>
    /// Implicit conversion from ResourceNodeId to int for backward compatibility.
    /// </summary>
    public static implicit operator int(ResourceNodeId nodeId) => nodeId.Value;

    /// <summary>
    /// Explicit conversion from int to ResourceNodeId (requires validation).
    /// </summary>
    public static explicit operator ResourceNodeId(int value) => Parse(value);

    public override string ToString() => Value.ToString();
}

