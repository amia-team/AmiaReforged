using Anvil.API;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops;

public readonly record struct ShopPriceContext(
    NpcShop Shop,
    NpcShopProduct Product,
    NwCreature? Buyer);
