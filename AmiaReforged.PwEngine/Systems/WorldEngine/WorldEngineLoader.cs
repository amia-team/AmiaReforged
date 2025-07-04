using AmiaReforged.Core;
using AmiaReforged.Core.Models.World;
using AmiaReforged.Core.Services;
using AmiaReforged.PwEngine.Systems.WorldEngine.Definitions;
using AmiaReforged.PwEngine.Systems.WorldEngine.Definitions.Economy;
using Anvil.Services;
using NLog;
using YamlDotNet.Serialization;

namespace AmiaReforged.PwEngine.Systems.WorldEngine;

// [ServiceBinding(typeof(WorldEngineLoader))]
public class WorldEngineLoader
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly IWorldConfigProvider _config;
    private readonly Deserializer _deserializer = new();

    private readonly string _resourcesPath;


    public List<ClimateDefinition> Climates { get; private set; } = [];
    public List<MaterialDefinition> Materials { get; private set; } = [];
    public List<NodeDefinition> ResourceNodes { get; private set; } = [];
    public List<RegionDefinition> Regions { get; private set; } = [];

    public WorldEngineLoader(IWorldConfigProvider config)
    {
        _config = config;
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
                NodeDefinition definition = _deserializer.Deserialize<NodeDefinition>(yamlContents);
                ResourceNodes.Add(definition);
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
        }
    }

    private void DoFirstTimeSetUp()
    {
    }

    public bool ResourceDirectoryExists()
    {
        return Directory.Exists(_resourcesPath);
    }
}

internal static class WorldConfigConstants
{
    public const string InitializedKey = "economy_initialized";
    public const string ConfigTypeBool = "bool";
}

[ServiceBinding(typeof(IWorldConfigProvider))]
public class WorldEngineConfig(DatabaseContextFactory factory) : IWorldConfigProvider
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly AmiaDbContext _ctx = factory.CreateDbContext();

    public bool GetBoolean(string key)
    {
        bool value = false;

        try
        {
            WorldConfiguration? entry = _ctx.WorldEngineConfig.FirstOrDefault(b =>
                b.Key == key && b.ValueType == WorldConfigConstants.ConfigTypeBool);

            if (entry != null) value = bool.Parse(entry.Value);
        }
        catch (Exception e)
        {
            Log.Error(e);
        }

        return value;
    }

    public int? GetInt(string key)
    {
        return null;
    }

    public float? GetFloat(string key) => null;

    public string? GetString(string key) => null;
}

public interface IWorldConfigProvider
{
    public bool GetBoolean(string key);
    public int? GetInt(string key);
    public float? GetFloat(string key);
    public string? GetString(string key);
}