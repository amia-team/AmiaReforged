namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops;

public interface INpcShopRepository
{
    void Upsert(NpcShopDefinition definition);
    void Upsert(NpcShop shop);
    bool TryGet(string shopTag, out NpcShop? shop);
    bool TryGetByShopkeeper(string shopkeeperTag, out NpcShop? shop);
    IReadOnlyCollection<NpcShop> All();
}
