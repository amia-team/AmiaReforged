namespace AmiaReforged.PwEngine.Features.Player.Mercantile;

public record ShopItem(
    ItemId ItemId,
    string Name,
    string Description,
    int Cost,
    byte[] ItemData
);