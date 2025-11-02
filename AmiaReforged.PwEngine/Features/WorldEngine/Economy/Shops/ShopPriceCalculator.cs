using System;
using System.Collections.Generic;
using System.Linq;
using Anvil.API;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops;

[ServiceBinding(typeof(IShopPriceCalculator))]
public sealed class ShopPriceCalculator : IShopPriceCalculator
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly IReadOnlyList<IShopPriceModifier> _modifiers;

    public ShopPriceCalculator(IEnumerable<IShopPriceModifier>? modifiers = null)
    {
        _modifiers = modifiers?.ToArray() ?? Array.Empty<IShopPriceModifier>();
    }

    public int CalculatePrice(NpcShop shop, NpcShopProduct product, NwCreature? buyer)
    {
        if (shop is null)
        {
            throw new ArgumentNullException(nameof(shop));
        }

        if (product is null)
        {
            throw new ArgumentNullException(nameof(product));
        }

        int currentPrice = product.Price;
        ShopPriceContext context = new(shop, product, buyer);

        foreach (IShopPriceModifier modifier in _modifiers)
        {
            try
            {
                currentPrice = modifier.ModifyPrice(currentPrice, context);
            }
            catch (Exception ex)
            {
                Log.Warn(ex,
                    "Shop price modifier {Modifier} failed for product {ProductResRef} in shop {ShopTag}.",
                    modifier.GetType().Name,
                    product.ResRef,
                    shop.Tag);
            }
        }

        if (currentPrice < 0)
        {
            return 0;
        }

        return currentPrice;
    }
}
