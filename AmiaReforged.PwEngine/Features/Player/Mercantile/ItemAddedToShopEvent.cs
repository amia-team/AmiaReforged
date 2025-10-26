namespace AmiaReforged.PwEngine.Features.Player.Mercantile;

public record ItemAddedToShopEvent(
    long ShopId,
    string ItemName,
    string ItemDescription,
    int Price,
    byte[] ItemData
);