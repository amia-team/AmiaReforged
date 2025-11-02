using Anvil.API;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops;

public interface IShopPriceCalculator
{
    int CalculatePrice(NpcShop shop, NpcShopProduct product, NwCreature? buyer);
}
