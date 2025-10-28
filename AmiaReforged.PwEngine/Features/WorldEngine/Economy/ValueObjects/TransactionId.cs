namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.ValueObjects;

/// <summary>
/// Unique identifier for a transaction.
/// </summary>
public readonly record struct TransactionId(Guid Value)
{
    /// <summary>
    /// Creates a new unique transaction ID.
    /// </summary>
    public static TransactionId NewId() => new(Guid.NewGuid());

    /// <summary>
    /// Creates a TransactionId from a GUID.
    /// </summary>
    public static TransactionId FromGuid(Guid guid) => new(guid);

    /// <summary>
    /// Implicit conversion to Guid for convenience.
    /// </summary>
    public static implicit operator Guid(TransactionId id) => id.Value;

    public override string ToString() => Value.ToString();
}

