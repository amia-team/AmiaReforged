using System;
using AmiaReforged.PwEngine.Database.Entities.Economy.Shops;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops;

public interface INpcShopRepository
{
    void Reload();
    void Upsert(NpcShopDefinition definition);
    void Upsert(ShopRecord record);
    bool TryGet(string shopTag, out NpcShop? shop);
    bool TryGetByShopkeeper(string shopkeeperTag, out NpcShop? shop);
    IReadOnlyCollection<NpcShop> All(ShopKind? kind = null);
    bool TryConsumeProduct(string shopTag, string productResRef, int quantity, out ConsignedItemData? consumedItem);
    void ReturnProduct(string shopTag, string productResRef, int quantity, ConsignedItemData? consignedItem = null);
    bool TryStorePlayerProduct(string shopTag, ShopProductRecord product, ConsignedItemData consignedItem);
    bool TryUpdateNextRestock(string shopTag, DateTime? nextRestockUtc);
    void ApplyRestock(NpcShop shop, IReadOnlyList<(NpcShopProduct Product, int Added)> restocked);
}
