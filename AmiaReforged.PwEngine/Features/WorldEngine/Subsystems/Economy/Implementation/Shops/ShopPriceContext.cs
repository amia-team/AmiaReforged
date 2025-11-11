using Anvil.API;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops;

public readonly record struct ShopPriceContext(
    NpcShop Shop,
    NpcShopProduct Product,
    NwCreature? Buyer);
