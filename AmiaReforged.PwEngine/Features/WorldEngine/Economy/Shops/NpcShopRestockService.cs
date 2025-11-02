using System;
using System.Collections.Generic;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops;

[ServiceBinding(typeof(NpcShopRestockService))]
public sealed class NpcShopRestockService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly HashSet<string> _initialized = new(StringComparer.OrdinalIgnoreCase);
    private readonly INpcShopRepository _repository;
    private readonly IShopRestockStrategy _strategy;

    public NpcShopRestockService(
        INpcShopRepository repository,
        IShopRestockStrategy strategy,
        SchedulerService scheduler)
    {
        _repository = repository;
        _strategy = strategy;

        scheduler.ScheduleRepeating(Tick, TimeSpan.FromMinutes(5));
        Tick();
    }

    private void Tick()
    {
        DateTime now = DateTime.UtcNow;

        foreach (NpcShop shop in _repository.All())
        {
            if (_initialized.Add(shop.Tag))
            {
                _strategy.Initialize(shop);
                Log.Info("Initialized NPC shop {Tag} with next restock at {Restock}.", shop.Tag, shop.NextRestockUtc);
            }

            if (_strategy.ShouldRestock(shop, now))
            {
                _strategy.Restock(shop, now);
                Log.Info("Restocked NPC shop {Tag}. Next restock at {Restock}.", shop.Tag, shop.NextRestockUtc);
            }
        }
    }
}
