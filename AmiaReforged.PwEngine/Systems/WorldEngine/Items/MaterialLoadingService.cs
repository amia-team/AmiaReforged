using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using AmiaReforged.PwEngine.Systems.WorldEngine.ResourceNodes;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Items;

[ServiceBinding(typeof(MaterialLoadingService))]
public class MaterialLoadingService(IMaterialRepository materials) : IDefinitionLoader
{
    private readonly List<FileLoadResult> _failures = [];

    public void Load()
    {
        string? resourcePath = Environment.GetEnvironmentVariable("RESOURCE_PATH");
        if (string.IsNullOrEmpty(resourcePath))
        {
            _failures.Add(new FileLoadResult(ResultType.Fail, "RESOURCE_PATH environment variable not set"));
            return;
        }

        string materialsDirectory = Path.Combine(resourcePath, "Materials");
        if (!Directory.Exists(materialsDirectory))
        {
            _failures.Add(new FileLoadResult(ResultType.Fail, $"Directory does not exist: {materialsDirectory}"));
            return;
        }

        string[] jsonFiles = Directory.GetFiles(materialsDirectory, "*.json", SearchOption.AllDirectories);

        foreach (string file in jsonFiles)
        {
            string fileName = Path.GetFileName(file);

            try
            {
                string json = File.ReadAllText(file);
                Material? definition =
                    JsonSerializer.Deserialize<Material>(json);

                if (definition == null)
                {
                    _failures.Add(new FileLoadResult(ResultType.Fail, "Failed to deserialize definition", fileName));
                    continue;
                }

                if (!TryValidate(definition, out string? error))
                {
                    _failures.Add(new FileLoadResult(ResultType.Fail, error, fileName));
                    continue;
                }

                materials.Upsert(definition);
            }
            catch (Exception ex)
            {
                _failures.Add(new FileLoadResult(ResultType.Fail, ex.Message, fileName));
            }
        }
    }

    [SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract")]
    private static bool TryValidate(Material definition, out string? error)
    {
        if (definition.CostMultiplier <= 0)
        {
            error = "Cost multiplier must never be less than or equal to 0.";
            return false;
        }

        if (definition.Hardness <= 0)
        {
            error = "Hardness must never be less than or equal to 0.";
            return false;
        }

        if (definition.Magic <= 0)
        {
            error = "Magic must never be less than or equal to 0.";
            return false;
        }

        error = null;
        return true;
    }

    public List<FileLoadResult> Failures()
    {
        return [];
    }
}
