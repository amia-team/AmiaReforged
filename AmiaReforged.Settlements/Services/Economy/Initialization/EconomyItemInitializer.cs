using AmiaReforged.Core.Helpers;
using AmiaReforged.Core.Models.Settlement;
using AmiaReforged.Core.Services;
using AmiaReforged.Core.Services.Settlements.Economy;
using AmiaReforged.Settlements.Services.Economy.FileReaders;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Settlements.Services.Economy.Initialization;

[ServiceBinding(typeof(IResourceInitializer))]
public class EconomyItemInitializer : IResourceInitializer
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly IRepository<EconomyItem, long> _itemDataService;
    private readonly NwTaskHelper _taskHelper;
    private readonly IEnumerable<EconomyItem> _importedItems;

    public EconomyItemInitializer(IResourceImporter<EconomyItem> importer, IRepositoryFactory factory,
        NwTaskHelper taskHelper)
    {
        _itemDataService = factory.CreateRepository<EconomyItem, long>();
        _taskHelper = taskHelper;
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

        await _taskHelper.TrySwitchToMainThread();
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