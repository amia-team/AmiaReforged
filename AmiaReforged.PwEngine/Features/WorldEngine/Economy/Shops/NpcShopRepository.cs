using System;
using System.Collections.Generic;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops;

[ServiceBinding(typeof(INpcShopRepository))]
public sealed class NpcShopRepository : INpcShopRepository
{
    private readonly Dictionary<string, NpcShop> _shops = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _shopkeeperToShop = new(StringComparer.OrdinalIgnoreCase);
    private readonly IShopItemBlacklist _blacklist;

    public NpcShopRepository(IShopItemBlacklist blacklist)
    {
        _blacklist = blacklist;
    }

    public void Upsert(NpcShop shop)
    {
        if (shop is null)
        {
            throw new ArgumentNullException(nameof(shop));
        }

        _shops[shop.Tag] = shop;
        _shopkeeperToShop[shop.ShopkeeperTag] = shop.Tag;
    }

    public void Upsert(NpcShopDefinition definition)
    {
        NpcShop shop = new(definition, _blacklist);
        Upsert(shop);
    }

    public bool TryGet(string shopTag, out NpcShop? shop)
    {
        if (string.IsNullOrWhiteSpace(shopTag))
        {
            shop = null;
            return false;
        }

        return _shops.TryGetValue(shopTag, out shop);
    }

    public bool TryGetByShopkeeper(string shopkeeperTag, out NpcShop? shop)
    {
        shop = null;

        if (string.IsNullOrWhiteSpace(shopkeeperTag))
        {
            return false;
        }

        if (!_shopkeeperToShop.TryGetValue(shopkeeperTag, out string? shopTag))
        {
            return false;
        }

        return _shops.TryGetValue(shopTag, out shop);
    }

    public IReadOnlyCollection<NpcShop> All()
    {
        return _shops.Values;
    }
}
