using System;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops;

public sealed record ConsignedItemData
{
    public ConsignedItemData(byte[] itemData, int quantity, string? itemName, string? resRef)
    {
        ItemData = itemData ?? throw new ArgumentNullException(nameof(itemData));
        Quantity = Math.Max(1, quantity);
        ItemName = itemName;
        ResRef = resRef;
    }

    public byte[] ItemData { get; }

    public int Quantity { get; }

    public string? ItemName { get; }

    public string? ResRef { get; }
}
