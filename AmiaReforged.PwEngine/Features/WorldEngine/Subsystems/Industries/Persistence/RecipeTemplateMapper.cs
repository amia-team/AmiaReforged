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
            RequiredProficiency = template.RequiredProficiency.ToString(),
            IngredientsJson = JsonSerializer.Serialize(
                template.Ingredients.Select(ToIngredientDto).ToList(), JsonOptions),
            ProductsJson = JsonSerializer.Serialize(
                template.Products.Select(ToProductDto).ToList(), JsonOptions),
            CraftingTimeSeconds = template.CraftingTimeSeconds,
            KnowledgePointsAwarded = template.KnowledgePointsAwarded,
            RequiredWorkstation = template.RequiredWorkstation?.Value,
            RequiredToolsJson = JsonSerializer.Serialize(template.RequiredTools, JsonOptions),
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

        Enum.TryParse<ProficiencyLevel>(entity.RequiredProficiency, true, out ProficiencyLevel proficiency);

        return new RecipeTemplate
        {
            Tag = entity.Tag,
            Name = entity.Name,
            Description = entity.Description,
            IndustryTag = new IndustryTag(entity.IndustryTag),
            RequiredKnowledge = knowledge,
            RequiredProficiency = proficiency,
            Ingredients = ingredientDtos.Select(FromIngredientDto).ToList(),
            Products = productDtos.Select(FromProductDto).ToList(),
            CraftingTimeSeconds = entity.CraftingTimeSeconds,
            KnowledgePointsAwarded = entity.KnowledgePointsAwarded,
            RequiredWorkstation = !string.IsNullOrEmpty(entity.RequiredWorkstation)
                ? new WorkstationTag(entity.RequiredWorkstation)
                : null,
            RequiredTools = JsonSerializer.Deserialize<List<string>>(entity.RequiredToolsJson, JsonOptions) ?? [],
            Metadata = metadata
        };
    }

    public static void UpdateEntity(PersistedRecipeTemplate entity, RecipeTemplate template)
    {
        entity.Name = template.Name;
        entity.Description = template.Description;
        entity.IndustryTag = template.IndustryTag.Value;
        entity.RequiredKnowledgeJson = JsonSerializer.Serialize(template.RequiredKnowledge, JsonOptions);
        entity.RequiredProficiency = template.RequiredProficiency.ToString();
        entity.IngredientsJson = JsonSerializer.Serialize(
            template.Ingredients.Select(ToIngredientDto).ToList(), JsonOptions);
        entity.ProductsJson = JsonSerializer.Serialize(
            template.Products.Select(ToProductDto).ToList(), JsonOptions);
        entity.CraftingTimeSeconds = template.CraftingTimeSeconds;
        entity.KnowledgePointsAwarded = template.KnowledgePointsAwarded;
        entity.RequiredWorkstation = template.RequiredWorkstation?.Value;
        entity.RequiredToolsJson = JsonSerializer.Serialize(template.RequiredTools, JsonOptions);
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
        Enum.TryParse<JobSystemItemType>(dto.RequiredForm, true, out JobSystemItemType form);

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
        Quality = p.Quality,
        SuccessChance = p.SuccessChance
    };

    private static TemplateProduct FromProductDto(TemplateProductJsonDto dto)
    {
        Enum.TryParse<JobSystemItemType>(dto.OutputForm, true, out JobSystemItemType form);

        return new TemplateProduct
        {
            OutputForm = form,
            MaterialSourceSlot = dto.MaterialSourceSlot,
            Quantity = Quantity.Parse(dto.Quantity),
            Quality = dto.Quality,
            SuccessChance = dto.SuccessChance
        };
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
        public int? Quality { get; set; }
        public float? SuccessChance { get; set; }
    }
}
