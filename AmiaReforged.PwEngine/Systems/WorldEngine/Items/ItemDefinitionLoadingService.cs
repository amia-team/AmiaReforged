using AmiaReforged.PwEngine.Systems.WorldEngine.ResourceNodes;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.Items;

[ServiceBinding(typeof(ItemDefinitionLoadingService))]
public class ItemDefinitionLoadingService(IItemDefinitionRepository repository) : IDefinitionLoader
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

        string[] jsonFiles = Directory.GetFiles(nodeDirectory, "*.json");

        foreach (string file in jsonFiles)
        {
            string fileName = Path.GetFileName(file);

            try
            {
                string json = File.ReadAllText(file);
                ItemDefinition? definition =
                    System.Text.Json.JsonSerializer.Deserialize<ItemDefinition>(json);

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

                repository.AddItemDefinition(definition);
            }
            catch (Exception ex)
            {
                _failures.Add(new FileLoadResult(ResultType.Fail, ex.Message, fileName));
            }
        }
    }

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

        error = null;
        return true;
    }
    public List<FileLoadResult> Failures()
    {
        return _failures;
    }

    public List<ItemDefinition> Definitions()
    {
        return repository.AllItems();
    }
}

