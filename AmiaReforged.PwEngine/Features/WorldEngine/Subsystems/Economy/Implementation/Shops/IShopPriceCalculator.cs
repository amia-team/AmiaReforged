using Anvil.API;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops;

public interface IShopPriceCalculator
{
    int CalculatePrice(NpcShop shop, NpcShopProduct product, NwCreature? buyer);
}
