using AmiaReforged.PwEngine.Systems.Economy.DomainModels;
using Anvil.Services;
using NLog;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace AmiaReforged.PwEngine.Systems.Economy;

// [ServiceBinding(typeof(EconomyService))]
public class EconomyService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly string _resourcesPath;
    private readonly Deserializer _deserializer;

    public List<PersistentResource> PersistentResources { get; set; } = new();
    public List<CultivatedResource> CultivatedResources { get; set; } = new();
    public List<Profession> Professions { get; set; } = new();
    public List<Material> Materials { get; set; } = new();
    public List<EnvironmentTrait> EnvironmentTraits { get; set; } = new();
    public List<Innovation> Innovations { get; set; } = new();

    public EconomyService()
    {
        _resourcesPath = Environment.GetEnvironmentVariable("ECONOMY_RESOURCES_PATH") ?? string.Empty;
        _deserializer = new();
        
        LoadYamlFiles();
    }

    public bool DirectoryExists()
    {
        return Directory.Exists(_resourcesPath);
    }

    private void LoadYamlFiles()
    {
        if (!Directory.Exists(_resourcesPath))
        {
            Log.Error($"Directory does not exist. Did not load economy system: {_resourcesPath}");
            return;
        }

        LoadMaterials();
        LoadEnvironmentTraits();
        LoadResources();
        LoadJobs();
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
                Material material = _deserializer.Deserialize<Material>(yamlContents);
                Materials.Add(material);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to load {file}");
            }
        }
    }

    private void LoadEnvironmentTraits()
    {
        string resourcesDirectory = Path.Combine(_resourcesPath, "EnvironmentTraits");
        foreach (string file in Directory.GetFiles(resourcesDirectory, "*.yaml", SearchOption.AllDirectories))
        {
            try
            {
                using StreamReader reader = new(file);
                string yamlContents = reader.ReadToEnd();
                EnvironmentTrait environmentTrait = _deserializer.Deserialize<EnvironmentTrait>(yamlContents);
                EnvironmentTraits.Add(environmentTrait);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to load {file}");
            }
        }
    }

    private void LoadResources()
    {
        LoadPersistentResources();
        LoadCultivatedResources();
    }

    private void LoadPersistentResources()
    {
        string resourcesDirectory = Path.Combine(_resourcesPath, "PersistentResources");
        foreach (string file in Directory.GetFiles(resourcesDirectory, "*.yaml", SearchOption.AllDirectories))
        {
            try
            {
                using StreamReader reader = new(file);
                string yamlContents = reader.ReadToEnd();
                PersistentResource resource = _deserializer.Deserialize<PersistentResource>(yamlContents);

                foreach (string tag in resource.MaterialTags)
                {
                    if (Materials.Any(t => t.MaterialType.ToString() == tag))
                    {
                        resource.Materials.Add(Materials.First(t => t.MaterialType.ToString() == tag));
                    }
                    else
                    {
                        Log.Warn($"Material tag {tag} not found in Materials.");
                    }
                }
                PersistentResources.Add(resource);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to load {file}");
            }
        }
    }

    private void LoadCultivatedResources()
    {
        string resourcesDirectory = Path.Combine(_resourcesPath, "CultivatedResources");
        foreach (string file in Directory.GetFiles(resourcesDirectory, "*.yaml", SearchOption.AllDirectories))
        {
            Log.Debug($"Reading from {file}");
            try
            {
                using StreamReader reader = new(file);
                string yamlContents = reader.ReadToEnd();

                Log.Debug($"Loading file: {file}\nContents: {yamlContents}");
                CultivatedResource resource = _deserializer.Deserialize<CultivatedResource>(yamlContents);

                foreach (string environmentTag in resource.EnvironmentTags)
                {
                    if (EnvironmentTraits.Any(t => t.Name == environmentTag))
                    {
                        resource.SuitableEnvironments.Add(EnvironmentTraits.First(t => t.Name == environmentTag));
                    }
                    else
                    {
                        Log.Warn($"Environment tag {environmentTag} not found in EnvironmentTraits.");
                    }
                }

                CultivatedResources.Add(resource);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to load {file}");
            }
        }
    }

    private void LoadJobs()
    {
    }
}