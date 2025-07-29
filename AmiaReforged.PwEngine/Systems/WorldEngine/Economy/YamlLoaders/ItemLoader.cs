using AmiaReforged.PwEngine.Systems.JobSystem.Entities;
using AmiaReforged.PwEngine.Systems.WorldEngine.Definitions.Economy;
using Microsoft.IdentityModel.Tokens;
using NLog;
using YamlDotNet.Serialization;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Economy.YamlLoaders;

public class ItemLoader : ILoader<ItemDefinition>
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly Deserializer _deserializer = new();

    public List<ItemDefinition> LoadedResources { get; } = [];
    public List<ResourceLoadError> Failures { get; } = [];
    public string DirectoryPath { get; } = FileSystemConfig.WorldEngineResourcesPath;

    public void LoadAll()
    {
        string itemDirectory = Path.Combine(DirectoryPath, "Items");

        foreach (string file in Directory.GetFiles(itemDirectory, "*.yaml", SearchOption.AllDirectories))
        {
            try
            {
                using StreamReader reader = new(file);
                string yamlContents = reader.ReadToEnd();
                ItemDefinition item = _deserializer.Deserialize<ItemDefinition>(yamlContents);

                string errors = string.Empty;
                if (item.BaseItemResRef.IsNullOrEmpty())
                {
                    errors = $"Invalid base item resref: Must not be empty.;{errors}";
                }

                if (item.Name.IsNullOrEmpty())
                {
                    errors = $"Invalid name: Must not be empty.;{errors}";
                }

                if (item.Tag.IsNullOrEmpty())
                {
                    errors = $"Invalid tag: Must not be empty.;{errors}";
                }

                if (item.Description.IsNullOrEmpty())
                {
                    errors = $"Invalid description: Must not be empty.;{errors}";
                }

                if (item.Appearance <= 0)
                {
                    errors = $"Invalid appearance number: Must be greater than 0.;{errors}";
                }

                if (item.MaxQuality == QualityEnum.Undefined)
                {
                    errors = $"Invalid max quality: Must be defined.;{errors}";
                }

                if (item.MinQuality == QualityEnum.Undefined)
                {
                    errors = $"Invalid min quality: Must be defined.;{errors}";
                }

                if (item.MinQuality >= item.MaxQuality)
                {
                    errors = $"Invalid quality range: Min quality must be less than, or equal to, max quality.;{errors}";
                }

                if (item.ItemType == ItemType.Undefined)
                {
                    errors = $"Invalid item type: Must be defined.;{errors}";
                }

                if (errors.IsNullOrEmpty())
                {
                    LoadedResources.Add(item);
                }
                else
                {
                    Failures.Add(new ResourceLoadError(file, errors));
                }
            }
            catch(Exception ex)
            {
                Failures.Add(new ResourceLoadError(file, ex.Message, ex));
            }
        }
    }
}
