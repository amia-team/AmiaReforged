namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops;

public interface IShopRestockStrategy
{
    void Initialize(NpcShop shop);
    bool ShouldRestock(NpcShop shop, DateTime utcNow);
    IReadOnlyList<(NpcShopProduct Product, int Added)> Restock(NpcShop shop, DateTime utcNow);
}
