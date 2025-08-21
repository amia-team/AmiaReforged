namespace AmiaReforged.PwEngine.Systems.WorldEngine.Features.IndustryAndCraft.ValueObjects;

public readonly record struct ItemTag(string Value)
{
    public static ItemTag From(string value) => new(value.Trim().ToLowerInvariant());
}

public sealed class Quantity
{
    public ItemTag Item { get; }
    public int Amount { get; }
    public Quantity(ItemTag item, int amount)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount));
        Item = item;
        Amount = amount;
    }
}
