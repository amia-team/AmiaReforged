namespace AmiaReforged.PwEngine.Features.Player.Mercantile;

public record PurchaseResult(bool IsSuccess, string? ErrorMessage, ShopItem? Item)
{
    public static PurchaseResult Success(ShopItem item) => new(true, null, item);
    public static PurchaseResult Failure(string error) => new(false, error, null);
}