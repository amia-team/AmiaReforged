using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops;

[ServiceBinding(typeof(IShopPriceModifier))]
public sealed class ShopMarkupPriceModifier : IShopPriceModifier
{
    public int ModifyPrice(int currentPrice, ShopPriceContext context)
    {
        if (currentPrice <= 0)
        {
            return 0;
        }

        int markup = Math.Max(context.Shop.MarkupPercent, 0);
        if (markup <= 0)
        {
            return currentPrice;
        }

        decimal price = currentPrice;
        decimal adjustment = price * markup / 100m;
        int adjusted = (int)Math.Round(price + adjustment, MidpointRounding.AwayFromZero);
        return Math.Max(0, adjusted);
    }
}
