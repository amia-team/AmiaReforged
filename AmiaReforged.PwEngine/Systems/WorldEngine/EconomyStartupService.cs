using AmiaReforged.PwEngine.Systems.WorldEngine.Industries;
using AmiaReforged.PwEngine.Systems.WorldEngine.Items;
using AmiaReforged.PwEngine.Systems.WorldEngine.ResourceNodes;
using Anvil.Services;
using DotNetEnv;
using Microsoft.IdentityModel.Tokens;
using NLog;
using NWN.Core.NWNX;

namespace AmiaReforged.PwEngine.Systems.WorldEngine;

[ServiceBinding(typeof(EconomyStartupService))]
public class EconomyStartupService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly ResourceDefinitionLoadingService _resourceLoader;
    private readonly ItemDefinitionLoadingService _itemLoader;
    private readonly IndustryDefinitionLoadingService _industryLoader;
    private readonly IWorldConfigProvider _configProvider;

    public EconomyStartupService(ResourceDefinitionLoadingService resourceLoader,
        ItemDefinitionLoadingService itemLoader,
        IndustryDefinitionLoadingService industryLoader,
        IWorldConfigProvider configProvider)
    {
        string resourcePath = UtilPlugin.GetEnvironmentVariable("RESOURCE_PATH");

        Environment.SetEnvironmentVariable("RESOURCE_PATH", resourcePath);

        _resourceLoader = resourceLoader;
        _itemLoader = itemLoader;
        _industryLoader = industryLoader;
        _configProvider = configProvider;

        LoadDefinitions();

        if (!_configProvider.GetBoolean(WorldConstants.InitializedKey))
        {
            DoFirstTimeSetup();
        }
    }

    private void DoFirstTimeSetup()
    {
        Log.Info("Performing first-time setup.");
        // _configProvider.SetBoolean(WorldConstants.InitializedKey, true);
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

    private void LogErrors(List<FileLoadResult> failures)
    {
        foreach (FileLoadResult fileLoadResult in failures)
        {
            Log.Error($"Failed to load file {fileLoadResult.FileName}:");
            Log.Error(fileLoadResult.Message);
        }
    }
}
