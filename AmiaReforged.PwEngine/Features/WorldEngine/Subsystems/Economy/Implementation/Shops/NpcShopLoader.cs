using System.Text.Json;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.ItemData;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops;

[ServiceBinding(typeof(NpcShopLoader))]
public sealed class NpcShopLoader : IDefinitionLoader
{
    private readonly List<FileLoadResult> _failures = new();
    private readonly INpcShopRepository _repository;
    private readonly IItemDefinitionRepository _itemDefinitions; // new dependency

    public NpcShopLoader(INpcShopRepository repository, IItemDefinitionRepository itemDefinitions)
    {
        _repository = repository;
        _itemDefinitions = itemDefinitions;
    }

    public void Load()
    {
        _failures.Clear();

        string? resourcePath = Environment.GetEnvironmentVariable("RESOURCE_PATH");
        if (string.IsNullOrEmpty(resourcePath))
        {
            _failures.Add(new FileLoadResult(ResultType.Fail, "RESOURCE_PATH environment variable not set"));
            return;
        }

        string directory = Path.Combine(resourcePath, "Economy", "NpcShops");
        if (!Directory.Exists(directory))
        {
            _failures.Add(new FileLoadResult(ResultType.Fail, $"Directory does not exist: {directory}"));
            return;
        }

        string[] jsonFiles = Directory.GetFiles(directory, "*.json", SearchOption.AllDirectories);
        Array.Sort(jsonFiles, StringComparer.OrdinalIgnoreCase);

        JsonSerializerOptions jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        HashSet<string> seenTags = new(StringComparer.OrdinalIgnoreCase);

        foreach (string file in jsonFiles)
        {
            string fileName = Path.GetFileName(file);

            try
            {
                string json = File.ReadAllText(file);
                NpcShopDefinition? definition = JsonSerializer.Deserialize<NpcShopDefinition>(json, jsonOptions);

                if (definition == null)
                {
                    _failures.Add(new FileLoadResult(ResultType.Fail, "Failed to deserialize definition", fileName));
                    continue;
                }

                if (!TryValidate(definition, fileName, seenTags))
                {
                    continue;
                }

                _repository.Upsert(definition);
            }
            catch (Exception ex)
            {
                _failures.Add(new FileLoadResult(ResultType.Fail, ex.Message, fileName, ex));
            }
        }
    }

    public List<FileLoadResult> Failures()
    {
        return _failures;
    }

    private bool TryValidate(NpcShopDefinition definition, string fileName, HashSet<string> seenTags)
    {
        if (string.IsNullOrWhiteSpace(definition.Tag))
        {
            _failures.Add(new FileLoadResult(ResultType.Fail, "Shop Tag must not be empty.", fileName));
            return false;
        }

        if (!seenTags.Add(definition.Tag))
        {
            _failures.Add(new FileLoadResult(ResultType.Fail,
                $"Duplicate shop tag '{definition.Tag}' detected in definitions.", fileName));
            return false;
        }

        if (string.IsNullOrWhiteSpace(definition.DisplayName))
        {
            _failures.Add(new FileLoadResult(ResultType.Fail, "DisplayName must not be empty.", fileName));
            return false;
        }

        if (string.IsNullOrWhiteSpace(definition.ShopkeeperTag))
        {
            _failures.Add(new FileLoadResult(ResultType.Fail, "ShopkeeperTag must not be empty.", fileName));
            return false;
        }

        if (definition.Restock == null)
        {
            _failures.Add(new FileLoadResult(ResultType.Fail, "Restock configuration is required.", fileName));
            return false;
        }

        if (definition.Restock.MinMinutes <= 0 || definition.Restock.MaxMinutes <= 0)
        {
            _failures.Add(new FileLoadResult(ResultType.Fail,
                "Restock MinMinutes and MaxMinutes must be positive integers.", fileName));
            return false;
        }

        if (definition.Restock.MaxMinutes < definition.Restock.MinMinutes)
        {
            _failures.Add(new FileLoadResult(ResultType.Fail,
                "Restock MaxMinutes cannot be less than MinMinutes.", fileName));
            return false;
        }

        if (definition.Products == null || definition.Products.Count == 0)
        {
            _failures.Add(new FileLoadResult(ResultType.Fail, "At least one product must be defined.", fileName));
            return false;
        }

        if (definition.MarkupPercent < 0)
        {
            _failures.Add(new FileLoadResult(ResultType.Fail,
                "MarkupPercent must not be negative.", fileName));
            return false;
        }

        if (definition.AcceptCategories is { Count: > 0 })
        {
            HashSet<int> categories = new();
            foreach (int category in definition.AcceptCategories)
            {
                if (category < 0)
                {
                    _failures.Add(new FileLoadResult(ResultType.Fail,
                        "AcceptCategories must contain non-negative integers.", fileName));
                    return false;
                }

                if (!categories.Add(category))
                {
                    _failures.Add(new FileLoadResult(ResultType.Fail,
                        "AcceptCategories must not contain duplicate entries.", fileName));
                    return false;
                }
            }
        }

        foreach (NpcShopProductDefinition product in definition.Products)
        {
            if (string.IsNullOrWhiteSpace(product.ItemTag))
            {
                _failures.Add(new FileLoadResult(ResultType.Fail, "Product ItemTag must not be empty.", fileName));
                return false;
            }

            ItemBlueprint? blueprint = _itemDefinitions.GetByTag(product.ItemTag);
            if (string.IsNullOrWhiteSpace(product.Name) && blueprint is null)
            {
                _failures.Add(new FileLoadResult(ResultType.Fail,
                    $"Product '{product.ItemTag}' must define a Name or have a matching blueprint.", fileName));
                return false;
            }

            if (product.Price < 0)
            {
                _failures.Add(new FileLoadResult(ResultType.Fail,
                    $"Product '{product.ItemTag}' has a negative price.", fileName));
                return false;
            }

            bool allowsPlayerStock = product.BaseItemType.HasValue;

            if (!allowsPlayerStock && product.MaxStock <= 0)
            {
                _failures.Add(new FileLoadResult(ResultType.Fail,
                    $"Product '{product.ItemTag}' must have MaxStock greater than zero.", fileName));
                return false;
            }

            if (allowsPlayerStock && product.MaxStock < 0)
            {
                _failures.Add(new FileLoadResult(ResultType.Fail,
                    $"Product '{product.ItemTag}' must not define a negative MaxStock.", fileName));
                return false;
            }

            if (!allowsPlayerStock && product.RestockAmount <= 0)
            {
                _failures.Add(new FileLoadResult(ResultType.Fail,
                    $"Product '{product.ItemTag}' must have RestockAmount greater than zero.", fileName));
                return false;
            }

            if (allowsPlayerStock && product.RestockAmount < 0)
            {
                _failures.Add(new FileLoadResult(ResultType.Fail,
                    $"Product '{product.ItemTag}' must not define a negative RestockAmount.", fileName));
                return false;
            }

            if (product.InitialStock < 0)
            {
                _failures.Add(new FileLoadResult(ResultType.Fail,
                    $"Product '{product.ItemTag}' must not have a negative InitialStock.", fileName));
                return false;
            }

            if (product.MaxStock > 0 && product.InitialStock > product.MaxStock)
            {
                _failures.Add(new FileLoadResult(ResultType.Fail,
                    $"Product '{product.ItemTag}' has InitialStock greater than MaxStock.", fileName));
                return false;
            }

            if (product.BaseItemType is < 0)
            {
                _failures.Add(new FileLoadResult(ResultType.Fail,
                    $"Product '{product.ItemTag}' defines an invalid BaseItemType; expected non-negative integer.", fileName));
                return false;
            }

            if (!ValidateLocalVariables(product, fileName))
            {
                return false;
            }

            if (!ValidateAppearance(product, fileName))
            {
                return false;
            }
        }

        return true;
    }

    private bool ValidateLocalVariables(NpcShopProductDefinition product, string fileName)
    {
        if (product.LocalVariables is null || product.LocalVariables.Count == 0)
        {
            return true;
        }

        HashSet<string> names = new(StringComparer.OrdinalIgnoreCase);

        foreach (JsonLocalVariableDefinition local in product.LocalVariables)
        {
            if (string.IsNullOrWhiteSpace(local.Name))
            {
                _failures.Add(new FileLoadResult(ResultType.Fail,
                    $"Product '{product.ItemTag}' defines a local variable with an empty name.", fileName));
                return false;
            }

            if (!names.Add(local.Name))
            {
                _failures.Add(new FileLoadResult(ResultType.Fail,
                    $"Product '{product.ItemTag}' defines duplicate local variable '{local.Name}'.", fileName));
                return false;
            }

            try
            {
                _ = NpcShopLocalVariable.FromDefinition(local);
            }
            catch (Exception ex)
            {
                _failures.Add(new FileLoadResult(ResultType.Fail,
                    $"Local variable '{local.Name}' for product '{product.ItemTag}' is invalid: {ex.Message}", fileName));
                return false;
            }
        }

        return true;
    }

    private bool ValidateAppearance(NpcShopProductDefinition product, string fileName)
    {
        if (product.Appearance is null)
        {
            return true;
        }

        try
        {
            _ = new SimpleModelAppearance(product.Appearance.ModelType, product.Appearance.SimpleModelNumber);
            return true;
        }
        catch (Exception ex)
        {
            _failures.Add(new FileLoadResult(ResultType.Fail,
                $"Appearance for product '{product.ItemTag}' is invalid: {ex.Message}", fileName));
            return false;
        }
    }
}
