using AmiaReforged.PwEngine.Systems.JobSystem.Entities;
using AmiaReforged.PwEngine.Systems.WorldEngine.Definitions.Common;
using AmiaReforged.PwEngine.Systems.WorldEngine.Definitions.Economy;
using Anvil.Services;
using Microsoft.IdentityModel.Tokens;
using NLog;
using YamlDotNet.Serialization;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Economy.YamlLoaders;

[ServiceBinding(typeof(MaterialLoader))]
public class MaterialLoader : ILoader<MaterialDefinition>
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    public List<MaterialDefinition> LoadedResources { get; } = [];
    public List<ResourceLoadError> Failures { get; } = [];
    public string DirectoryPath { get; } = FileSystemConfig.WorldEngineResourcesPath;

    private readonly Deserializer _deserializer = new();

    public void LoadAll()
    {
        string resourcesDirectory = Path.Combine(DirectoryPath, "Materials");
        foreach (string file in Directory.GetFiles(resourcesDirectory, "*.yaml", SearchOption.AllDirectories))
        {
            try
            {
                using StreamReader reader = new(file);
                string yamlContents = reader.ReadToEnd();
                MaterialDefinition mat = _deserializer.Deserialize<MaterialDefinition>(yamlContents);

                string errors = string.Empty;

                if (mat.CostModifier == 0)
                {
                    errors = $"Invalid cost modifier: Must be greater than 0.;{errors}";
                }

                if (mat.MagicModifier == 0)
                {
                    errors = $"Invalid magic modifier: Must be greater than 0.;{errors}";
                }

                if (mat.DurabilityModifier == 0)
                {
                    errors = $"Invalid durability modifier: Must be greater than 0.;{errors}";
                }

                if (mat.WeightModifier == 0)
                {
                    errors = $"Invalid weight modifier: Must be greater than 0.;{errors}";
                }

                if (mat.MaterialType == MaterialEnum.None)
                {
                    errors = $"Invalid material type: Must be defined.;{errors}";
                }


                if (errors.IsNullOrEmpty())
                {
                    LoadedResources.Add(mat);
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
