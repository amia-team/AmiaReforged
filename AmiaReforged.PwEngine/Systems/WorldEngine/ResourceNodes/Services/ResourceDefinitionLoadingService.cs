using AmiaReforged.PwEngine.Systems.WorldEngine.ResourceNodes.ResourceNodeData;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Systems.WorldEngine.ResourceNodes.Services;

[ServiceBinding(typeof(ResourceDefinitionLoadingService))]
public class ResourceDefinitionLoadingService(IResourceNodeDefinitionRepository repository) : IDefinitionLoader
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

        string nodeDirectory = Path.Combine(resourcePath, "Nodes");
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
                ResourceNodeDefinition? definition =
                    System.Text.Json.JsonSerializer.Deserialize<ResourceNodeDefinition>(json);

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

                repository.Create(definition);
            }
            catch (Exception ex)
            {
                _failures.Add(new FileLoadResult(ResultType.Fail, ex.Message, fileName));
            }
        }
    }

    public List<FileLoadResult> Failures()
    {
        return _failures;
    }

    public List<ResourceNodeDefinition> Definitions()
    {
        return repository.All();
    }

    private static bool TryValidate(ResourceNodeDefinition definition, out string? error)
    {
        // Tag MUST be non-empty.
        if (string.IsNullOrWhiteSpace(definition.Tag))
        {
            error = "Tag must be non-empty.";
            return false;
        }

        // A requirement MUST be set.
        if (definition.Requirement is null)
        {
            error = "Requirement must be set.";
            return false;
        }

        // Outputs MUST be non-null and non-empty.
        if (definition.Outputs is null || definition.Outputs.Length == 0)
        {
            error = "Outputs must be a non-empty array.";
            return false;
        }

        error = null;
        return true;
    }
}
