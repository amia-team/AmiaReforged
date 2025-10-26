namespace AmiaReforged.PwEngine.Features.Player.Mercantile;

public record TransactionReceipt(
    ItemId ItemId,
    string ItemName,
    string BuyerName,
    int Cost,
    DateTime PurchaseDate
);