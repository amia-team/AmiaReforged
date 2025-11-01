namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Properties;

/// <summary>
/// Identifies a settlement or municipality where a property resides.
/// </summary>
public readonly record struct SettlementTag
{
    public string Value { get; }

    public SettlementTag(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Settlement tag cannot be empty.", nameof(value));

        Value = value.Trim();
    }

    public override string ToString() => Value;

    public static implicit operator string(SettlementTag tag) => tag.Value;
}
