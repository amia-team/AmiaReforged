namespace AmiaReforged.PwEngine.Features.Player.Mercantile;

public record ItemPurchasedEvent(
    long ShopId,
    string ShopTag,
    string AreaResRef,
    ItemId ItemId,
    string BuyerName,
    byte[] ItemData,
    int Cost
);