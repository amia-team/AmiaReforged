using System;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops;

public interface IShopRestockStrategy
{
    void Initialize(NpcShop shop);
    bool ShouldRestock(NpcShop shop, DateTime utcNow);
    IReadOnlyList<(NpcShopProduct Product, int Added)> Restock(NpcShop shop, DateTime utcNow);
}
