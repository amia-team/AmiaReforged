using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using AmiaReforged.PwEngine.Systems.WorldEngine.ResourceNodes;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Items;

[ServiceBinding(typeof(ItemDefinitionLoadingService))]
public class ItemDefinitionLoadingService(IItemDefinitionRepository items) : IDefinitionLoader
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

        string nodeDirectory = Path.Combine(resourcePath, "Items");
        if (!Directory.Exists(nodeDirectory))
        {
            _failures.Add(new FileLoadResult(ResultType.Fail, $"Directory does not exist: {nodeDirectory}"));
            return;
        }

        string[] jsonFiles = Directory.GetFiles(nodeDirectory, "*.json", SearchOption.AllDirectories);

        foreach (string file in jsonFiles)
        {
            string fileName = Path.GetFileName(file);

            try
            {
                string json = File.ReadAllText(file);
                ItemDefinition? definition =
                    JsonSerializer.Deserialize<ItemDefinition>(json);

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

                items.AddItemDefinition(definition);
            }
            catch (Exception ex)
            {
                _failures.Add(new FileLoadResult(ResultType.Fail, ex.Message, fileName));
            }
        }
    }

    [SuppressMessage("ReSharper", "ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract")]
    private static bool TryValidate(ItemDefinition definition, out string? error)
    {
        if (definition.ResRef.Length > 16)
        {
            error = "ResRef must not exceed 16 characters.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(definition.ItemTag))
        {
            error = "ItemTag must not be empty.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(definition.Name))
        {
            error = "Name must not be empty.";
            return false;
        }

        if (definition.Appearance is null)
        {
            error = "Appearance must not be null.";
            return false;
        }

        error = null;
        return true;
    }

    public List<FileLoadResult> Failures()
    {
        return _failures;
    }

    public List<ItemDefinition> Definitions()
    {
        return items.AllItems();
    }
}
