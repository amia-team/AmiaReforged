namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops;

public interface IShopPriceModifier
{
    int ModifyPrice(int currentPrice, ShopPriceContext context);
}
