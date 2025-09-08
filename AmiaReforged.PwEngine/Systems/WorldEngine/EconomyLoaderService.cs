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

    public EconomyLoaderService(ResourceDefinitionLoadingService resourceLoader,
        ItemDefinitionLoadingService itemLoader,
        IndustryDefinitionLoadingService industryLoader)
    {
        string resourcePath = UtilPlugin.GetEnvironmentVariable("RESOURCE_PATH");

        Environment.SetEnvironmentVariable("RESOURCE_PATH", resourcePath);

        _resourceLoader = resourceLoader;
        _itemLoader = itemLoader;
        _industryLoader = industryLoader;
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

        LogErrors(_industryLoader.Failures());
        LogErrors(_itemLoader.Failures());
        LogErrors(_resourceLoader.Failures());
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
