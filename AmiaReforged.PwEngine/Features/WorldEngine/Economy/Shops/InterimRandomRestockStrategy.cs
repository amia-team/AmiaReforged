using System;
using System.Collections.Generic;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops;

/// <summary>
/// Temporary restock strategy that schedules random top-ups until the full supply/demand simulation arrives.
/// Replace this with a data-driven production scheduler once factories and logistics are modeled end-to-end.
/// </summary>
[ServiceBinding(typeof(IShopRestockStrategy))]
public sealed class InterimRandomRestockStrategy : IShopRestockStrategy
{
    private static readonly TimeSpan MinimumFallback = TimeSpan.FromMinutes(30);

    public void Initialize(NpcShop shop)
    {
        if (shop == null)
        {
            throw new ArgumentNullException(nameof(shop));
        }

        if (!shop.ManualRestock)
        {
            ScheduleNextRestock(shop, DateTime.UtcNow);
        }
    }

    public bool ShouldRestock(NpcShop shop, DateTime utcNow)
    {
        if (shop == null)
        {
            throw new ArgumentNullException(nameof(shop));
        }

        if (shop.ManualRestock)
        {
            return false;
        }

        return utcNow >= shop.NextRestockUtc;
    }

    public IReadOnlyList<(NpcShopProduct Product, int Added)> Restock(NpcShop shop, DateTime utcNow)
    {
        if (shop == null)
        {
            throw new ArgumentNullException(nameof(shop));
        }

        if (shop.ManualRestock)
        {
            return Array.Empty<(NpcShopProduct, int)>();
        }

        IReadOnlyList<(NpcShopProduct Product, int Added)> restocked = shop.RestockAll();
        ScheduleNextRestock(shop, utcNow);
        return restocked;
    }

    private static void ScheduleNextRestock(NpcShop shop, DateTime fromTime)
    {
        int minMinutes = Math.Max(shop.RestockPolicy.MinMinutes, 1);
        int maxMinutes = Math.Max(minMinutes, shop.RestockPolicy.MaxMinutes);

        int minutesUntilRestock = Random.Shared.Next(minMinutes, maxMinutes + 1);
        if (minutesUntilRestock <= 0)
        {
            shop.SetNextRestock(fromTime + MinimumFallback);
            return;
        }

        shop.SetNextRestock(fromTime + TimeSpan.FromMinutes(minutesUntilRestock));
    }
}
