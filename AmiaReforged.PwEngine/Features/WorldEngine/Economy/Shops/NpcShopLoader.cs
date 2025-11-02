using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops;

[ServiceBinding(typeof(NpcShopLoader))]
public sealed class NpcShopLoader : IDefinitionLoader
{
    private readonly List<FileLoadResult> _failures = new();
    private readonly INpcShopRepository _repository;

    public NpcShopLoader(INpcShopRepository repository)
    {
        _repository = repository;
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
                _failures.Add(new FileLoadResult(ResultType.Fail, ex.Message, fileName));
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

        foreach (NpcShopProductDefinition product in definition.Products)
        {
            if (string.IsNullOrWhiteSpace(product.ResRef))
            {
                _failures.Add(new FileLoadResult(ResultType.Fail, "Product ResRef must not be empty.", fileName));
                return false;
            }

            if (product.Price < 0)
            {
                _failures.Add(new FileLoadResult(ResultType.Fail,
                    $"Product '{product.ResRef}' has a negative price.", fileName));
                return false;
            }

            if (product.MaxStock <= 0)
            {
                _failures.Add(new FileLoadResult(ResultType.Fail,
                    $"Product '{product.ResRef}' must have MaxStock greater than zero.", fileName));
                return false;
            }

            if (product.RestockAmount <= 0)
            {
                _failures.Add(new FileLoadResult(ResultType.Fail,
                    $"Product '{product.ResRef}' must have RestockAmount greater than zero.", fileName));
                return false;
            }

            if (product.InitialStock < 0)
            {
                _failures.Add(new FileLoadResult(ResultType.Fail,
                    $"Product '{product.ResRef}' must not have a negative InitialStock.", fileName));
                return false;
            }

            if (product.InitialStock > product.MaxStock)
            {
                _failures.Add(new FileLoadResult(ResultType.Fail,
                    $"Product '{product.ResRef}' has InitialStock greater than MaxStock.", fileName));
                return false;
            }
        }

        return true;
    }
}
