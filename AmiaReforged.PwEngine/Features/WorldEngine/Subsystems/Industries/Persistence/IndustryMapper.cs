using System.Text.Json;
using System.Text.Json.Serialization;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.KnowledgeSubsystem;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.Persistence;

/// <summary>
/// Maps between <see cref="Industry"/> domain objects and
/// <see cref="PersistedIndustryDefinition"/> EF entities.
/// </summary>
public static class IndustryMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    public static PersistedIndustryDefinition ToEntity(Industry industry)
    {
        return new PersistedIndustryDefinition
        {
            Tag = industry.Tag,
            Name = industry.Name,
            KnowledgeJson = JsonSerializer.Serialize(
                industry.Knowledge.Select(ToKnowledgeDto).ToList(), JsonOptions),
            RecipesJson = JsonSerializer.Serialize(
                industry.Recipes.Select(ToRecipeDto).ToList(), JsonOptions),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public static Industry ToDomain(PersistedIndustryDefinition entity)
    {
        var knowledgeDtos = JsonSerializer.Deserialize<List<KnowledgeJsonDto>>(entity.KnowledgeJson, JsonOptions)
                            ?? new List<KnowledgeJsonDto>();

        var recipeDtos = JsonSerializer.Deserialize<List<RecipeJsonDto>>(entity.RecipesJson, JsonOptions)
                         ?? new List<RecipeJsonDto>();

        return new Industry
        {
            Tag = entity.Tag,
            Name = entity.Name,
            Knowledge = knowledgeDtos.Select(FromKnowledgeDto).ToList(),
            Recipes = recipeDtos.Select(FromRecipeDto).ToList()
        };
    }

    public static void UpdateEntity(PersistedIndustryDefinition entity, Industry industry)
    {
        entity.Name = industry.Name;
        entity.KnowledgeJson = JsonSerializer.Serialize(
            industry.Knowledge.Select(ToKnowledgeDto).ToList(), JsonOptions);
        entity.RecipesJson = JsonSerializer.Serialize(
            industry.Recipes.Select(ToRecipeDto).ToList(), JsonOptions);
        entity.UpdatedAt = DateTime.UtcNow;
    }

    // ==================== Knowledge mapping ====================

    private static KnowledgeJsonDto ToKnowledgeDto(Knowledge k) => new()
    {
        Tag = k.Tag,
        Name = k.Name,
        Description = k.Description,
        Level = k.Level.ToString(),
        PointCost = k.PointCost,
        HarvestEffects = k.HarvestEffects.Select(e => new HarvestEffectJsonDto
        {
            NodeTag = e.NodeTag,
            StepModified = e.StepModified.ToString(),
            Value = e.Value,
            Operation = e.Operation.ToString()
        }).ToList(),
        Prerequisites = k.Prerequisites,
        Branch = k.Branch,
        Effects = k.Effects.Select(e => new KnowledgeEffectJsonDto
        {
            EffectType = e.EffectType.ToString(),
            TargetTag = e.TargetTag,
            Metadata = e.Metadata
        }).ToList()
    };

    private static Knowledge FromKnowledgeDto(KnowledgeJsonDto dto)
    {
        Enum.TryParse<ProficiencyLevel>(dto.Level, true, out var level);

        return new Knowledge
        {
            Tag = dto.Tag,
            Name = dto.Name,
            Description = dto.Description,
            Level = level,
            PointCost = dto.PointCost,
            HarvestEffects = dto.HarvestEffects?.Select(e =>
            {
                Enum.TryParse<Harvesting.HarvestStep>(e.StepModified, true, out var step);
                Enum.TryParse<KnowledgeSubsystem.EffectOperation>(e.Operation, true, out var op);
                return new KnowledgeHarvestEffect(e.NodeTag, step, e.Value, op);
            }).ToList() ?? [],
            Prerequisites = dto.Prerequisites ?? [],
            Branch = dto.Branch,
            Effects = dto.Effects?.Select(e =>
            {
                Enum.TryParse<KnowledgeEffectType>(e.EffectType, true, out var effectType);
                return new KnowledgeEffect
                {
                    EffectType = effectType,
                    TargetTag = e.TargetTag,
                    Metadata = e.Metadata ?? new Dictionary<string, object>()
                };
            }).ToList() ?? []
        };
    }

    // ==================== Recipe mapping ====================

    private static RecipeJsonDto ToRecipeDto(Recipe r) => new()
    {
        RecipeId = r.RecipeId.Value,
        Name = r.Name,
        Description = r.Description,
        IndustryTag = r.IndustryTag.Value,
        RequiredKnowledge = r.RequiredKnowledge,
        RequiredProficiency = r.RequiredProficiency.ToString(),
        Ingredients = r.Ingredients.Select(i => new IngredientJsonDto
        {
            ItemTag = i.ItemTag,
            Quantity = i.Quantity.Value,
            MinQuality = i.MinQuality,
            IsConsumed = i.IsConsumed
        }).ToList(),
        Products = r.Products.Select(p => new ProductJsonDto
        {
            ItemTag = p.ItemTag,
            Quantity = p.Quantity.Value,
            Quality = p.Quality,
            SuccessChance = p.SuccessChance
        }).ToList(),
        CraftingTimeSeconds = r.CraftingTimeSeconds,
        KnowledgePointsAwarded = r.KnowledgePointsAwarded,
        Metadata = r.Metadata,
        RequiredWorkstation = r.RequiredWorkstation?.Value,
        ProcessId = r.ProcessId
    };

    private static Recipe FromRecipeDto(RecipeJsonDto dto)
    {
        Enum.TryParse<ProficiencyLevel>(dto.RequiredProficiency, true, out var proficiency);

        return new Recipe
        {
            RecipeId = new RecipeId(dto.RecipeId),
            Name = dto.Name,
            Description = dto.Description,
            IndustryTag = new IndustryTag(dto.IndustryTag),
            RequiredKnowledge = dto.RequiredKnowledge ?? [],
            RequiredProficiency = proficiency,
            Ingredients = dto.Ingredients?.Select(i => new Ingredient
            {
                ItemTag = i.ItemTag,
                Quantity = Quantity.Parse(i.Quantity),
                MinQuality = i.MinQuality,
                IsConsumed = i.IsConsumed
            }).ToList() ?? [],
            Products = dto.Products?.Select(p => new Product
            {
                ItemTag = p.ItemTag,
                Quantity = Quantity.Parse(p.Quantity),
                Quality = p.Quality,
                SuccessChance = p.SuccessChance
            }).ToList() ?? [],
            CraftingTimeSeconds = dto.CraftingTimeSeconds,
            KnowledgePointsAwarded = dto.KnowledgePointsAwarded,
            Metadata = dto.Metadata ?? new Dictionary<string, object>(),
            RequiredWorkstation = !string.IsNullOrEmpty(dto.RequiredWorkstation)
                ? new WorkstationTag(dto.RequiredWorkstation)
                : null,
            ProcessId = dto.ProcessId
        };
    }

    // ==================== Internal JSON DTOs ====================

    private class KnowledgeJsonDto
    {
        public string Tag { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? Level { get; set; }
        public int PointCost { get; set; }
        public List<HarvestEffectJsonDto>? HarvestEffects { get; set; }
        public List<string>? Prerequisites { get; set; }
        public string? Branch { get; set; }
        public List<KnowledgeEffectJsonDto>? Effects { get; set; }
    }

    private class KnowledgeEffectJsonDto
    {
        public string EffectType { get; set; } = string.Empty;
        public string TargetTag { get; set; } = string.Empty;
        public Dictionary<string, object>? Metadata { get; set; }
    }

    private class HarvestEffectJsonDto
    {
        public string NodeTag { get; set; } = string.Empty;
        public string StepModified { get; set; } = string.Empty;
        public float Value { get; set; }
        public string Operation { get; set; } = string.Empty;
    }

    private class RecipeJsonDto
    {
        public string RecipeId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string IndustryTag { get; set; } = string.Empty;
        public List<string>? RequiredKnowledge { get; set; }
        public string? RequiredProficiency { get; set; }
        public List<IngredientJsonDto>? Ingredients { get; set; }
        public List<ProductJsonDto>? Products { get; set; }
        public int? CraftingTimeSeconds { get; set; }
        public int KnowledgePointsAwarded { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
        public string? RequiredWorkstation { get; set; }
        public string? ProcessId { get; set; }
    }

    private class IngredientJsonDto
    {
        public string ItemTag { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public int? MinQuality { get; set; }
        public bool IsConsumed { get; set; } = true;
    }

    private class ProductJsonDto
    {
        public string ItemTag { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public int? Quality { get; set; }
        public float? SuccessChance { get; set; }
    }
}
