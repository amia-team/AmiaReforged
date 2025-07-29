using AmiaReforged.PwEngine.Systems.WorldEngine.Definitions;
using Anvil.Services;
using Microsoft.IdentityModel.Tokens;
using NLog;
using YamlDotNet.Serialization;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Economy.YamlLoaders;

[ServiceBinding(typeof(ClimateLoader))]
public class ClimateLoader : ILoader<ClimateDefinition>
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly Deserializer _deserializer = new();

    public List<ClimateDefinition> LoadedResources { get; } = [];
    public List<ResourceLoadError> Failures { get; } = [];
    public string DirectoryPath { get; } = FileSystemConfig.WorldEngineResourcesPath;

    public void LoadAll()
    {
        string resourcesDirectory = Path.Combine(DirectoryPath, "Climates");
        foreach (string file in Directory.GetFiles(resourcesDirectory, "*.yaml", SearchOption.AllDirectories))
        {
            try
            {
                using StreamReader reader = new(file);
                string yamlContents = reader.ReadToEnd();
                ClimateDefinition climate = _deserializer.Deserialize<ClimateDefinition>(yamlContents);

                string errors = string.Empty;

                if (climate.Name.IsNullOrEmpty())
                {
                    errors = $"Invalid name: Must not be empty.;{errors}";
                }

                if (climate.Tag.IsNullOrEmpty())
                {
                    errors = $"Invalid tag: Must not be empty.;{errors}";
                }

                if (LoadedResources.Any(r => r.Tag == climate.Tag))
                {
                    errors = $"Duplicate tag: {climate.Tag};{errors}";
                }

                if (climate.Description.IsNullOrEmpty())
                {
                    errors = $"Invalid description: Must not be empty.;{errors}";
                }

                if (errors.IsNullOrEmpty())
                {
                    LoadedResources.Add(climate);
                }
                else
                {
                    Failures.Add(new ResourceLoadError(file, errors));
                }
            }
            catch (Exception ex)
            {
                Failures.Add(new ResourceLoadError(file, ex.Message, ex));
            }
        }
    }
}
