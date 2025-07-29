using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities.Economy;
using AmiaReforged.PwEngine.Systems.WorldEngine.Definitions;
using AmiaReforged.PwEngine.Systems.WorldEngine.Definitions.Economy;
using AmiaReforged.PwEngine.Systems.WorldEngine.Economy.YamlLoaders;
using Anvil.Services;
using NLog;
using YamlDotNet.Serialization;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Economy;

[ServiceBinding(typeof(EconomyDefinitions))]
public class EconomyDefinitions
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly IWorldConfigProvider _config;
    private readonly MaterialLoader _materials;
    private readonly ResourceNodeLoader _resourceNodes;
    private readonly ClimateLoader _climates;
    private readonly RegionLoader _regions;
    private readonly Deserializer _deserializer = new();

    private readonly string _resourcesPath;

    public List<ClimateDefinition> Climates { get; } = [];
    public List<MaterialDefinition> Materials { get; } = [];
    public List<ResourceNodeDefinition> NodeDefinitions { get; } = [];
    public List<RegionDefinition> Regions { get; } = [];


    public EconomyDefinitions(IWorldConfigProvider config, MaterialLoader materials, ResourceNodeLoader resourceNodes,
        ClimateLoader climates, RegionLoader regions)
    {
        _config = config;
        _materials = materials;
        _resourceNodes = resourceNodes;
        _climates = climates;
        _regions = regions;
        _resourcesPath = Environment.GetEnvironmentVariable("ECONOMY_RESOURCES_PATH") ?? string.Empty;

        if (_resourcesPath == string.Empty)
        {
            Log.Error("No directory defined.");
            return;
        }

        if (!Directory.Exists(_resourcesPath))
        {
            Log.Error($"{_resourcesPath} does not exist.");
            return;
        }

        LoadAllDefinitions();
    }

    private void LoadAllDefinitions()
    {
        LoadMaterials();
        LoadResourceNodes();
        LoadClimates();
        LoadRegions();
    }

    private void LoadMaterials()
    {
        _materials.LoadAll();

        if (_materials.Failures.Count > 0)
        {
            Log.Error("Failed to load some materials");
            foreach (ResourceLoadError failure in _materials.Failures)
            {
                Log.Error(failure.ToString());
            }
        }

        Materials.AddRange(_materials.LoadedResources);
    }

    private void LoadResourceNodes()
    {
        _resourceNodes.LoadAll();

        if (_resourceNodes.Failures.Count > 0)
        {
            Log.Error("Failed to load some resource nodes");
            foreach (ResourceLoadError failure in _resourceNodes.Failures)
            {
                Log.Error(failure.ToString());
            }
        }

        NodeDefinitions.AddRange(_resourceNodes.LoadedResources);
    }

    private void LoadClimates()
    {
        _climates.LoadAll();

        if (_climates.Failures.Count > 0)
        {
            Log.Error("Failed to load some climates");
            foreach (ResourceLoadError failure in _climates.Failures)
            {
                Log.Error(failure.ToString());
            }
        }

        Climates.AddRange(_climates.LoadedResources);
    }

    private void LoadRegions()
    {
        _regions.LoadAll();

        if (_regions.Failures.Count > 0)
        {
            Log.Error("Failed to load some regions");
            foreach (ResourceLoadError failure in _regions.Failures)
            {
                Log.Error(failure.ToString());
            }
        }

        Regions.AddRange(_regions.LoadedResources);
    }
}
