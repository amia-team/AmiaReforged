﻿using AmiaReforged.Core.Helpers;
using AmiaReforged.Core.Models.Settlement;
using AmiaReforged.Core.Services;
using AmiaReforged.Core.Services.Settlements.Economy;
using AmiaReforged.Settlements.Services.Economy.FileReaders;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Settlements.Services.Economy.Initialization;

/// <summary>
/// Implementation of <see cref="IResourceInitializer"/> for initializing <see cref="EconomyItem"/>s.
/// </summary>
[ServiceBinding(typeof(EconomyItemInitializer))]
[Obsolete("We decided not tod define items in yaml files, so this class is no longer used.")]
public class EconomyItemInitializer : IResourceInitializer
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly IRepository<EconomyItem, long> _itemDataService;
    private readonly IEnumerable<EconomyItem> _importedItems;

    public EconomyItemInitializer(IResourceImporter<EconomyItem> importer, IRepositoryFactory factory)
    {
        _itemDataService = factory.CreateRepository<EconomyItem, long>();
        _importedItems = importer.LoadResources();
    }

    public async Task Initialize()
    {
        Log.Info("Initializing Economy Items...");
        await ProcessItems();
    }

    private async Task ProcessItems()
    {
        foreach (EconomyItem item in _importedItems)
        {
            Log.Info($"Processing item: {item.Name}");

            EconomyItem? dbItem = await FindItem(item.Name);
            if (dbItem != null)
            {
                UpdateItem(dbItem, item);
                await _itemDataService.Update(dbItem);
            }
            else
            {
                await _itemDataService.Add(item);
            }
        }
    }

    private async Task<EconomyItem?> FindItem(string? itemName)
    {
        IEnumerable<EconomyItem?> items = await _itemDataService.GetAll();

        return items.FirstOrDefault(i => i?.Name == itemName);
    }

    private static void UpdateItem(EconomyItem? dbItem, EconomyItem item)
    {
        if (dbItem == null) return;

        dbItem.Name = item.Name;
        dbItem.MaterialId = item.MaterialId;
        dbItem.QualityId = item.QualityId;
        dbItem.BaseValue = item.BaseValue;
    }
}