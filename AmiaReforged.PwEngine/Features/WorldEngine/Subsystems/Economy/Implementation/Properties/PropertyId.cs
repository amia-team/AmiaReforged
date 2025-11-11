using System.Diagnostics.CodeAnalysis;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Properties;

/// <summary>
/// Value object identifying a rentable property within the world.
/// </summary>
public readonly record struct PropertyId
{
    public Guid Value { get; }

    private PropertyId(Guid value)
    {
        if (value == Guid.Empty)
            throw new ArgumentException("Property id cannot be empty.", nameof(value));

        Value = value;
    }

    public static PropertyId New() => new(Guid.NewGuid());

    public static PropertyId Parse(Guid value) => new(value);

    public static PropertyId Parse(string value)
    {
        if (!Guid.TryParse(value, out Guid guid))
            throw new FormatException($"Value '{value}' is not a valid property id.");

        return new PropertyId(guid);
    }

    public override string ToString() => Value.ToString();

    public static implicit operator Guid(PropertyId id) => id.Value;

    [return: NotNullIfNotNull("value")]
    public static PropertyId? FromNullable(Guid? value) => value is null ? null : new PropertyId(value.Value);
}
