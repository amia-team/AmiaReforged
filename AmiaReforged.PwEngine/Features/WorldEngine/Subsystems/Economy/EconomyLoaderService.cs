using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Banks;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Regions;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes.Services;
using Anvil.Services;
using NLog;
using NWN.Core.NWNX;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy;

[ServiceBinding(typeof(EconomyLoaderService))]
public class EconomyLoaderService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly ResourceDefinitionLoadingService _resourceLoader;
    private readonly ItemDefinitionLoadingService _itemLoader;
    private readonly IndustryDefinitionLoadingService _industryLoader;
    private readonly RegionDefinitionLoadingService _regionLoader;
    private readonly NpcShopLoader _shopLoader;
    private readonly CoinhouseLoader _coinhouseLoader;

    public EconomyLoaderService(ResourceDefinitionLoadingService resourceLoader,
        ItemDefinitionLoadingService itemLoader,
        IndustryDefinitionLoadingService industryLoader,
        RegionDefinitionLoadingService regionLoader,
        NpcShopLoader shopLoader,
        CoinhouseLoader coinhouseLoader,
        ResourceWatcherService resourceWatcherService)
    {
        string resourcePath = UtilPlugin.GetEnvironmentVariable("RESOURCE_PATH");

        Environment.SetEnvironmentVariable("RESOURCE_PATH", resourcePath);

        _resourceLoader = resourceLoader;
        _itemLoader = itemLoader;
        _industryLoader = industryLoader;
        _regionLoader = regionLoader;
        _shopLoader = shopLoader;
        _coinhouseLoader = coinhouseLoader;

        resourceWatcherService.FileSystemChanged += ReloadChanges;
    }

    private void ReloadChanges(object? sender, FileSystemEventArgs e)
    {
        if(e.Name is null) return;

        if(!e.Name.EndsWith(".json")) return;
        Log.Info($"Reloading {e.Name}");
        LoadDefinitions();
    }

    public void Startup()
    {
        LoadDefinitions();
    }

    private void LoadDefinitions()
    {
        _industryLoader.Load();
        _itemLoader.Load();
        _resourceLoader.Load();
        _regionLoader.Load();
        _shopLoader.Load();
        _coinhouseLoader.Load();

        LogErrors(_industryLoader.Failures());
        LogErrors(_itemLoader.Failures());
        LogErrors(_resourceLoader.Failures());
        LogErrors(_regionLoader.Failures());
        LogErrors(_shopLoader.Failures());
        LogErrors(_coinhouseLoader.Failures());
    }

    private static void LogErrors(List<FileLoadResult> failures)
    {
        foreach (FileLoadResult fileLoadResult in failures)
        {
            Log.Error($"Failed to load file {fileLoadResult.FileName}:");
            Log.Error(fileLoadResult.Message);
        }
    }
}
