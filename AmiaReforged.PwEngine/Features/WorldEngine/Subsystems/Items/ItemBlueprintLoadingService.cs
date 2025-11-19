using System.Text.Json;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items;

[ServiceBinding(typeof(ItemBlueprintLoadingService))]
public sealed class ItemBlueprintLoadingService(IItemDefinitionRepository definitions) : IDefinitionLoader
{
    private readonly List<FileLoadResult> _failures = new();

    public void Load()
    {
        string? resourcePath = Environment.GetEnvironmentVariable("RESOURCE_PATH");
        if (string.IsNullOrWhiteSpace(resourcePath))
        {
            _failures.Add(new FileLoadResult(ResultType.Fail, "RESOURCE_PATH environment variable not set"));
            return;
        }

        string blueprintsDir = Path.Combine(resourcePath, "Items", "Blueprints");
        if (!Directory.Exists(blueprintsDir))
        {
            // Do not treat missing directory as failure; no blueprints yet.
            return;
        }

        foreach (string filePath in Directory.GetFiles(blueprintsDir, "*.json", SearchOption.AllDirectories))
        {
            string fileName = Path.GetFileName(filePath);
            try
            {
                string json = File.ReadAllText(filePath);
                Items.ItemData.ItemBlueprint? blueprint = JsonSerializer.Deserialize<Items.ItemData.ItemBlueprint>(json);
                if (blueprint is null)
                {
                    _failures.Add(new FileLoadResult(ResultType.Fail, "Failed to deserialize blueprint", fileName));
                    continue;
                }

                if (!TryValidate(blueprint, out string? error))
                {
                    _failures.Add(new FileLoadResult(ResultType.Fail, error, fileName));
                    continue;
                }

                // Store blueprint as an item definition (shared model).
                definitions.AddItemDefinition(blueprint);
            }
            catch (Exception ex)
            {
                _failures.Add(new FileLoadResult(ResultType.Fail, ex.Message, fileName, ex));
            }
        }
    }

    private static bool TryValidate(Items.ItemData.ItemBlueprint def, out string? error)
    {
        if (def.ResRef.Length > 16)
        {
            error = "ResRef must not exceed 16 characters.";
            return false;
        }
        if (string.IsNullOrWhiteSpace(def.ItemTag))
        {
            error = "ItemTag must not be empty.";
            return false;
        }
        if (string.IsNullOrWhiteSpace(def.Name))
        {
            error = "Name must not be empty.";
            return false;
        }
        if (def.Appearance is null)
        {
            error = "Appearance must not be null.";
            return false;
        }
        if (def.LocalVariables is { Count: > 0 })
        {
            foreach (var local in def.LocalVariables)
            {
                if (string.IsNullOrWhiteSpace(local.Name))
                {
                    error = "Local variable name must not be empty.";
                    return false;
                }
                switch (local.Type)
                {
                    case ItemData.JsonLocalVariableType.Int:
                        if (local.Value.ValueKind != System.Text.Json.JsonValueKind.Number)
                        {
                            error = $"Local variable '{local.Name}' expected number.";
                            return false;
                        }
                        break;
                    case ItemData.JsonLocalVariableType.String:
                        if (local.Value.ValueKind != System.Text.Json.JsonValueKind.String)
                        {
                            error = $"Local variable '{local.Name}' expected string.";
                            return false;
                        }
                        break;
                    case ItemData.JsonLocalVariableType.Json:
                        if (local.Value.ValueKind == System.Text.Json.JsonValueKind.Undefined)
                        {
                            error = $"Local variable '{local.Name}' JSON value undefined.";
                            return false;
                        }
                        break;
                }
            }
        }
        error = null;
        return true;
    }

    public List<FileLoadResult> Failures() => _failures;
}
