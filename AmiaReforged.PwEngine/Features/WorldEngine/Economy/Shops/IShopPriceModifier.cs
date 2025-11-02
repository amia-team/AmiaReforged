namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops;

public interface IShopPriceModifier
{
    int ModifyPrice(int currentPrice, ShopPriceContext context);
}
