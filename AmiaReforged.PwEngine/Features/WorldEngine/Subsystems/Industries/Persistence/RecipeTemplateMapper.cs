using System.Text.Json;
using System.Text.Json.Serialization;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.ItemData;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.Persistence;

/// <summary>
/// Maps between <see cref="RecipeTemplate"/> domain objects and
/// <see cref="PersistedRecipeTemplate"/> EF entities.
/// </summary>
public static class RecipeTemplateMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static PersistedRecipeTemplate ToEntity(RecipeTemplate template)
    {
        return new PersistedRecipeTemplate
        {
            Tag = template.Tag,
            Name = template.Name,
            Description = template.Description,
            IndustryTag = template.IndustryTag.Value,
            RequiredKnowledgeJson = JsonSerializer.Serialize(template.RequiredKnowledge, JsonOptions),
            IngredientsJson = JsonSerializer.Serialize(
                template.Ingredients.Select(ToIngredientDto).ToList(), JsonOptions),
            ProductsJson = JsonSerializer.Serialize(
                template.Products.Select(ToProductDto).ToList(), JsonOptions),
            CraftingTimeRounds = template.CraftingTimeRounds,
            ProgressionPointsAwarded = template.ProgressionPointsAwarded,
            RequiredWorkstation = template.RequiredWorkstation?.Value,
            RequiredToolsJson = JsonSerializer.Serialize(
                template.RequiredTools.Select(ToToolRequirementDto).ToList(), JsonOptions),
            MetadataJson = JsonSerializer.Serialize(template.Metadata, JsonOptions),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public static RecipeTemplate ToDomain(PersistedRecipeTemplate entity)
    {
        List<string> knowledge = JsonSerializer.Deserialize<List<string>>(entity.RequiredKnowledgeJson, JsonOptions) ?? [];
        List<TemplateIngredientJsonDto> ingredientDtos = JsonSerializer.Deserialize<List<TemplateIngredientJsonDto>>(entity.IngredientsJson, JsonOptions) ?? [];
        List<TemplateProductJsonDto> productDtos = JsonSerializer.Deserialize<List<TemplateProductJsonDto>>(entity.ProductsJson, JsonOptions) ?? [];
        Dictionary<string, object> metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(entity.MetadataJson, JsonOptions) ?? new();

        return new RecipeTemplate
        {
            Tag = entity.Tag,
            Name = entity.Name,
            Description = entity.Description,
            IndustryTag = new IndustryTag(entity.IndustryTag),
            RequiredKnowledge = knowledge,
            Ingredients = ingredientDtos.Select(FromIngredientDto).ToList(),
            Products = productDtos.Select(FromProductDto).ToList(),
            CraftingTimeRounds = entity.CraftingTimeRounds,
            ProgressionPointsAwarded = entity.ProgressionPointsAwarded,
            RequiredWorkstation = !string.IsNullOrEmpty(entity.RequiredWorkstation)
                ? new WorkstationTag(entity.RequiredWorkstation)
                : null,
            RequiredTools = DeserializeToolRequirements(entity.RequiredToolsJson),
            Metadata = metadata
        };
    }

    public static void UpdateEntity(PersistedRecipeTemplate entity, RecipeTemplate template)
    {
        entity.Name = template.Name;
        entity.Description = template.Description;
        entity.IndustryTag = template.IndustryTag.Value;
        entity.RequiredKnowledgeJson = JsonSerializer.Serialize(template.RequiredKnowledge, JsonOptions);
        entity.IngredientsJson = JsonSerializer.Serialize(
            template.Ingredients.Select(ToIngredientDto).ToList(), JsonOptions);
        entity.ProductsJson = JsonSerializer.Serialize(
            template.Products.Select(ToProductDto).ToList(), JsonOptions);
        entity.CraftingTimeRounds = template.CraftingTimeRounds;
        entity.ProgressionPointsAwarded = template.ProgressionPointsAwarded;
        entity.RequiredWorkstation = template.RequiredWorkstation?.Value;
        entity.RequiredToolsJson = JsonSerializer.Serialize(
            template.RequiredTools.Select(ToToolRequirementDto).ToList(), JsonOptions);
        entity.MetadataJson = JsonSerializer.Serialize(template.Metadata, JsonOptions);
        entity.UpdatedAt = DateTime.UtcNow;
    }

    // ==================== Ingredient mapping ====================

    private static TemplateIngredientJsonDto ToIngredientDto(TemplateIngredient i) => new()
    {
        RequiredCategory = i.RequiredCategory.ToString(),
        RequiredForm = i.RequiredForm.ToString(),
        Quantity = i.Quantity.Value,
        MinQuality = i.MinQuality,
        IsConsumed = i.IsConsumed,
        SlotIndex = i.SlotIndex
    };

    private static TemplateIngredient FromIngredientDto(TemplateIngredientJsonDto dto)
    {
        Enum.TryParse<MaterialCategory>(dto.RequiredCategory, true, out MaterialCategory category);
        Enum.TryParse<ItemForm>(dto.RequiredForm, true, out ItemForm form);

        return new TemplateIngredient
        {
            RequiredCategory = category,
            RequiredForm = form,
            Quantity = Quantity.Parse(dto.Quantity),
            MinQuality = dto.MinQuality,
            IsConsumed = dto.IsConsumed,
            SlotIndex = dto.SlotIndex
        };
    }

    // ==================== Product mapping ====================

    private static TemplateProductJsonDto ToProductDto(TemplateProduct p) => new()
    {
        OutputForm = p.OutputForm.ToString(),
        MaterialSourceSlot = p.MaterialSourceSlot,
        Quantity = p.Quantity.Value,
        SuccessChance = p.SuccessChance
    };

    private static TemplateProduct FromProductDto(TemplateProductJsonDto dto)
    {
        Enum.TryParse<ItemForm>(dto.OutputForm, true, out ItemForm form);

        return new TemplateProduct
        {
            OutputForm = form,
            MaterialSourceSlot = dto.MaterialSourceSlot,
            Quantity = Quantity.Parse(dto.Quantity),
            SuccessChance = dto.SuccessChance
        };
    }

    // ==================== Tool requirement mapping ====================

    private static ToolRequirementJsonDto ToToolRequirementDto(ToolRequirement tr) => new()
    {
        RequiredForm = tr.RequiredForm.ToString(),
        RequiredMaterial = tr.RequiredMaterial?.ToString(),
        MinQuality = tr.MinQuality,
        ExactItemTag = tr.ExactItemTag
    };

    private static ToolRequirement FromToolRequirementDto(ToolRequirementJsonDto dto)
    {
        Enum.TryParse<ItemForm>(dto.RequiredForm, true, out ItemForm form);
        MaterialEnum? material = null;
        if (!string.IsNullOrEmpty(dto.RequiredMaterial) &&
            Enum.TryParse<MaterialEnum>(dto.RequiredMaterial, true, out MaterialEnum parsedMaterial))
        {
            material = parsedMaterial;
        }

        return new ToolRequirement
        {
            RequiredForm = form,
            RequiredMaterial = material,
            MinQuality = dto.MinQuality,
            ExactItemTag = dto.ExactItemTag
        };
    }

    /// <summary>
    /// Deserializes tool requirements with backward compatibility.
    /// Handles both the new structured format and legacy <c>["tag1","tag2"]</c> string arrays.
    /// </summary>
    private static List<ToolRequirement> DeserializeToolRequirements(string json)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "[]")
            return [];

        // Try the new structured format first
        try
        {
            List<ToolRequirementJsonDto>? dtos =
                JsonSerializer.Deserialize<List<ToolRequirementJsonDto>>(json, JsonOptions);
            if (dtos != null && dtos.Count > 0)
            {
                // Verify it's actually the new format (has at least one non-empty field)
                if (dtos.Any(d => !string.IsNullOrEmpty(d.RequiredForm) || !string.IsNullOrEmpty(d.ExactItemTag)))
                {
                    return dtos.Select(FromToolRequirementDto).ToList();
                }
            }
        }
        catch (JsonException)
        {
            // Not the new format — try legacy
        }

        // Fall back to legacy string array format
        try
        {
            List<string>? legacyTags = JsonSerializer.Deserialize<List<string>>(json, JsonOptions);
            if (legacyTags != null)
            {
                return legacyTags
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .Select(tag => new ToolRequirement { ExactItemTag = tag })
                    .ToList();
            }
        }
        catch (JsonException)
        {
            // Completely unrecognized — return empty
        }

        return [];
    }

    // ==================== Internal JSON DTOs ====================

    private class TemplateIngredientJsonDto
    {
        public string RequiredCategory { get; set; } = string.Empty;
        public string RequiredForm { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public int? MinQuality { get; set; }
        public bool IsConsumed { get; set; } = true;
        public int SlotIndex { get; set; }
    }

    private class TemplateProductJsonDto
    {
        public string OutputForm { get; set; } = string.Empty;
        public int MaterialSourceSlot { get; set; }
        public int Quantity { get; set; }
        public float? SuccessChance { get; set; }
    }

    private class ToolRequirementJsonDto
    {
        public string RequiredForm { get; set; } = string.Empty;
        public string? RequiredMaterial { get; set; }
        public int? MinQuality { get; set; }
        public string? ExactItemTag { get; set; }
    }
}
