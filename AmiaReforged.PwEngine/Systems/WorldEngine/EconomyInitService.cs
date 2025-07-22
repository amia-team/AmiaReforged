using AmiaReforged.PwEngine.Systems.WorldEngine.Definitions;
using AmiaReforged.PwEngine.Systems.WorldEngine.Definitions.Economy;
using AmiaReforged.PwEngine.Systems.WorldEngine.Economy.ResourceNodes;
using Anvil.API;
using NLog;
using YamlDotNet.Serialization;

namespace AmiaReforged.PwEngine.Systems.WorldEngine;

// [ServiceBinding(typeof(WorldEngineLoader))]
public class EconomyInitService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly IWorldConfigProvider _config;
    private readonly IEnumerable<ISubSystemInitializer> _initalizers;
    private readonly Deserializer _deserializer = new();

    private readonly string _resourcesPath;


    public List<ClimateDefinition> Climates { get; } = [];
    public List<MaterialDefinition> Materials { get; } = [];
    public List<ResourceNodeDefinition> NodeDefinitions { get; } = [];
    public List<RegionDefinition> Regions { get; } = [];
    
    public List<ResourceNodeInstance> ResourceInstances { get; } = [];

    public EconomyInitService(IWorldConfigProvider config, IEnumerable<ISubSystemInitializer> initalizers)
    {
        _config = config;
        _initalizers = initalizers;
        _resourcesPath = Environment.GetEnvironmentVariable("ECONOMY_RESOURCES_PATH") ?? string.Empty;

        LoadAllDefinitions();
        Startup();
    }

    private void LoadAllDefinitions()
    {
        LoadMaterials();
        LoadResourceNodes();
        LoadClimates();
        LoadRegions();
    }

    private void LoadRegions()
    {
        string resourcesDirectory = Path.Combine(_resourcesPath, "Regions");
        foreach (string file in Directory.GetFiles(resourcesDirectory, "*.yaml", SearchOption.AllDirectories))
        {
            try
            {
                using StreamReader reader = new(file);
                string yamlContents = reader.ReadToEnd();
                RegionDefinition region = _deserializer.Deserialize<RegionDefinition>(yamlContents);

                ClimateDefinition? climate = Climates.FirstOrDefault(c => c.Tag == region.ClimateTag);
                if (climate != null)
                {
                    region.Climate = climate;
                }

                Regions.Add(region);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to load {file}");
            }
        }
    }

    private void LoadMaterials()
    {
        string resourcesDirectory = Path.Combine(_resourcesPath, "Materials");
        foreach (string file in Directory.GetFiles(resourcesDirectory, "*.yaml", SearchOption.AllDirectories))
        {
            try
            {
                using StreamReader reader = new(file);
                string yamlContents = reader.ReadToEnd();
                MaterialDefinition mat = _deserializer.Deserialize<MaterialDefinition>(yamlContents);
                Materials.Add(mat);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to load {file}");
            }
        }
    }

    private void LoadResourceNodes()
    {
        string resourcesDirectory = Path.Combine(_resourcesPath, "ResourceNodes");
        foreach (string file in Directory.GetFiles(resourcesDirectory, "*.yaml", SearchOption.AllDirectories))
        {
            try
            {
                using StreamReader reader = new(file);
                string yamlContents = reader.ReadToEnd();
                ResourceNodeDefinition definition = _deserializer.Deserialize<ResourceNodeDefinition>(yamlContents);
                NodeDefinitions.Add(definition);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to load {file}");
            }
        }
    }

    private void LoadClimates()
    {
        string resourcesDirectory = Path.Combine(_resourcesPath, "Climates");
        foreach (string file in Directory.GetFiles(resourcesDirectory, "*.yaml", SearchOption.AllDirectories))
        {
            try
            {
                using StreamReader reader = new(file);
                string yamlContents = reader.ReadToEnd();
                ClimateDefinition environmentTrait = _deserializer.Deserialize<ClimateDefinition>(yamlContents);
                Climates.Add(environmentTrait);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to load {file}");
            }
        }

        foreach (ClimateDefinition climateDefinition in from climateDefinition in Climates
                 let climatesWithCurrentTag = Climates.Where(c => c.Tag == climateDefinition.Tag).ToList()
                 where climatesWithCurrentTag.Count > 1
                 select climateDefinition)
        {
            Log.Error($"({climateDefinition.Name}) Multiple climates with the same tag: {climateDefinition.Tag}");
        }
    }

    private void Startup()
    {
        bool initialized = _config.GetBoolean(WorldConfigConstants.InitializedKey);

        if (!initialized)
        {
            DoFirstTimeSetUp();
            return;
        }
    }

    private void DoFirstTimeSetUp()
    {
        foreach (ISubSystemInitializer subSystemInitializer in _initalizers)
        {
            subSystemInitializer.Init(this);
        }
    }

    private void SeedRandomAgents()
    {
    }

    private void SeedOres()
    {
    }

    public bool ResourceDirectoryExists()
    {
        return Directory.Exists(_resourcesPath);
    }
}