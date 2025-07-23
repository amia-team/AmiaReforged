using AmiaReforged.PwEngine.Database.Entities.Economy;
using AmiaReforged.PwEngine.Systems.WorldEngine.Economy.HarvestActions;
using Anvil.Services;
using Microsoft.IdentityModel.Tokens;
using NLog;
using YamlDotNet.Serialization;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Economy.YamlLoaders;

[ServiceBinding(typeof(ResourceNodeLoader))]
public class ResourceNodeLoader : ILoaderStrategy<ResourceNodeDefinition>
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly Deserializer _deserializer = new();

    public List<ResourceNodeDefinition> LoadedResources { get; } = [];
    public List<ResourceLoadError> Failures { get; } = [];
    public string DirectoryPath { get; } = FileSystemConfig.WorldEngineResourcesPath;

    public void LoadAll()
    {
        string resourcesDirectory = Path.Combine(DirectoryPath, "ResourceNodes");
        foreach (string file in Directory.GetFiles(resourcesDirectory, "*.yaml", SearchOption.AllDirectories))
        {
            try
            {
                using StreamReader reader = new(file);
                string yamlContents = reader.ReadToEnd();
                ResourceNodeDefinition definition = _deserializer.Deserialize<ResourceNodeDefinition>(yamlContents);

                string errors = string.Empty;

                if (definition.Name.IsNullOrEmpty())
                {
                    errors = $"Invalid name: Must not be empty.;{errors}";
                }

                if (definition.Tag.IsNullOrEmpty())
                {
                    errors = $"Invalid tag: Must not be empty.;{errors}";
                }

                if (LoadedResources.Any(r => r.Tag == definition.Tag))
                {
                    errors = $"Duplicate tag: {definition.Tag};{errors}";
                }

                if (definition.Appearance <= 0)
                {
                    errors = $"Invalid appearance: Must be greater than 0.;{errors}";
                }

                if (definition.Type == ResourceType.Undefined)
                {
                    errors = $"Type must be defined.;{errors}";
                }

                if (definition.HarvestAction == HarvestActionEnum.Undefined)
                {
                    errors = $"Harvest Action must be defined.;{errors}";
                }

                if (errors.IsNullOrEmpty())
                {
                    LoadedResources.Add(definition);
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
