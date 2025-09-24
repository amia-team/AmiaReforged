using AmiaReforged.PwEngine.Systems.WorldEngine.Domains;
using AmiaReforged.PwEngine.Systems.WorldEngine.Industries;
using AmiaReforged.PwEngine.Systems.WorldEngine.Items;
using AmiaReforged.PwEngine.Systems.WorldEngine.ResourceNodes;
using Anvil.Services;
using NLog;
using NWN.Core.NWNX;

namespace AmiaReforged.PwEngine.Systems.WorldEngine;

[ServiceBinding(typeof(EconomyLoaderService))]
public class EconomyLoaderService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly ResourceDefinitionLoadingService _resourceLoader;
    private readonly ItemDefinitionLoadingService _itemLoader;
    private readonly IndustryDefinitionLoadingService _industryLoader;
    private readonly RegionDefinitionLoadingService _regionLoader;

    public EconomyLoaderService(ResourceDefinitionLoadingService resourceLoader,
        ItemDefinitionLoadingService itemLoader,
        IndustryDefinitionLoadingService industryLoader,
        RegionDefinitionLoadingService regionLoader,
        ResourceWatcherService resourceWatcherService)
    {
        string resourcePath = UtilPlugin.GetEnvironmentVariable("RESOURCE_PATH");

        Environment.SetEnvironmentVariable("RESOURCE_PATH", resourcePath);

        _resourceLoader = resourceLoader;
        _itemLoader = itemLoader;
        _industryLoader = industryLoader;
        _regionLoader = regionLoader;

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

        LogErrors(_industryLoader.Failures());
        LogErrors(_itemLoader.Failures());
        LogErrors(_resourceLoader.Failures());
        LogErrors(_regionLoader.Failures());
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
