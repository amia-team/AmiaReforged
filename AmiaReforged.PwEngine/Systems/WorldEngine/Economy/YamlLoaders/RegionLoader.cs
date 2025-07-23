using AmiaReforged.PwEngine.Database.Entities.Economy;
using AmiaReforged.PwEngine.Systems.WorldEngine.Definitions;
using AmiaReforged.PwEngine.Systems.WorldEngine.Economy.HarvestActions;
using Anvil.Services;
using Microsoft.IdentityModel.Tokens;
using NLog;
using YamlDotNet.Serialization;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Economy.YamlLoaders;

[ServiceBinding(typeof(RegionLoader))]
public class RegionLoader : ILoader<RegionDefinition>
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly Deserializer _deserializer = new();

    public List<RegionDefinition> LoadedResources { get; } = [];
    public List<ResourceLoadError> Failures { get; } = [];
    public string DirectoryPath { get; } = FileSystemConfig.WorldEngineResourcesPath;

    public void LoadAll()
    {
        string resourcesDirectory = Path.Combine(DirectoryPath, "Regions");
        foreach (string file in Directory.GetFiles(resourcesDirectory, "*.yaml", SearchOption.AllDirectories))
        {
            try
            {
                using StreamReader reader = new(file);
                string yamlContents = reader.ReadToEnd();
                RegionDefinition region = _deserializer.Deserialize<RegionDefinition>(yamlContents);

                string errors = string.Empty;

                if (region.Name.IsNullOrEmpty())
                {
                    errors = $"Invalid name: Must not be empty.;{errors}";
                }

                if (region.Tag.IsNullOrEmpty())
                {
                    errors = $"Invalid tag: Must not be empty.;{errors}";
                }

                if (LoadedResources.Any(r => r.Tag == region.Tag))
                {
                    errors = $"Duplicate tag: {region.Tag};{errors}";
                }

                if (region.Areas.IsNullOrEmpty())
                {
                    errors = $"Invalid areas: Must not be empty.;{errors}";
                }

                foreach (AreaDefinition area in region.Areas)
                {
                    if (area.ResRef.IsNullOrEmpty())
                    {
                        errors = $"Invalid area: ResRef must not be empty.;{errors}";
                    }

                    if (area.ResRef.Length > 16)
                    {
                        errors = $"Invalid area: ResRef must not be longer than 16 characters.;{errors}";
                    }

                    if (area.SpawnableNodes.Count <= 0) continue;
                    errors = area.SpawnableNodes.Where(areaSpawnableNode => areaSpawnableNode.IsNullOrEmpty())
                        .Aggregate(errors,
                            (current, areaSpawnableNode) =>
                                $"Region's area {area.ResRef} has an invalid spawnable node: Must not be empty.;{current}");
                }

                if (errors.IsNullOrEmpty())
                {
                    LoadedResources.Add(region);
                }
                else
                {
                    Failures.Add(new ResourceLoadError(file, errors));
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"Failed to load {file}");
                Failures.Add(new ResourceLoadError(file, ex.Message, ex));
            }
        }
    }
}
