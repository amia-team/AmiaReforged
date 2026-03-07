namespace AmiaReforged.AdminPanel.Models;

// ==================== Item Blueprint DTOs ====================

public class ItemBlueprintDto
{
    public string ResRef { get; set; } = string.Empty;
    public string ItemTag { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string[]? Materials { get; set; }
    public string? JobSystemType { get; set; }
    public int BaseItemType { get; set; }
    public AppearanceDataDto? Appearance { get; set; }
    public int BaseValue { get; set; } = 1;
    public int WeightIncreaseConstant { get; set; } = -1;
    public string? SourceFile { get; set; }
}

public class AppearanceDataDto
{
    public int ModelType { get; set; }
    public int? SimpleModelNumber { get; set; }
    public WeaponPartDataDto? Data { get; set; }
}

public class WeaponPartDataDto
{
    public int TopPartModel { get; set; }
    public int MiddlePartModel { get; set; }
    public int BottomPartModel { get; set; }
    public int TopPartColor { get; set; }
    public int MiddlePartColor { get; set; }
    public int BottomPartColor { get; set; }
}

// ==================== Resource Node DTOs ====================

public class ResourceNodeDefinitionDto
{
    public string Tag { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Description { get; set; }
    public int PlcAppearance { get; set; }
    public string? Type { get; set; }
    public int Uses { get; set; } = 50;
    public int BaseHarvestRounds { get; set; }
    public HarvestContextDto? Requirement { get; set; }
    public HarvestOutputDto[]? Outputs { get; set; }
    public FloraPropertiesDto? FloraProperties { get; set; }
}

public class HarvestContextDto
{
    public string? RequiredItemType { get; set; }
    public string? RequiredItemMaterial { get; set; }
}

public class HarvestOutputDto
{
    public string ItemDefinitionTag { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int Chance { get; set; } = 100;
}

public class FloraPropertiesDto
{
    public string? PreferredClimate { get; set; }
    public string? RequiredSoilQuality { get; set; }
}

// ==================== Common Response DTOs ====================

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}

public class ImportResult
{
    public int Succeeded { get; set; }
    public int Failed { get; set; }
    public int Total { get; set; }
    public List<string> Errors { get; set; } = new();
}

// ==================== Area Graph DTOs ====================

public class AreaNodeDto
{
    public string ResRef { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Region { get; set; }
    public bool HasSpawnProfile { get; set; }
    public string? SpawnProfileName { get; set; }
}

public class AreaEdgeDto
{
    public string SourceResRef { get; set; } = string.Empty;
    public string TargetResRef { get; set; } = string.Empty;
    public string TransitionType { get; set; } = string.Empty;
    public string TransitionTag { get; set; } = string.Empty;
}

public class AreaGraphDto
{
    public List<AreaNodeDto> Nodes { get; set; } = new();
    public List<AreaEdgeDto> Edges { get; set; } = new();
    public List<AreaNodeDto> DisconnectedAreas { get; set; } = new();
    public DateTime GeneratedAtUtc { get; set; }
}

// ==================== Region DTOs ====================

public class RegionDefinitionDto
{
    public string Tag { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<AreaDefinitionDto> Areas { get; set; } = new();
    public ChaosStateDto? DefaultChaos { get; set; }
}

public class AreaDefinitionDto
{
    public string ResRef { get; set; } = string.Empty;
    public List<string> DefinitionTags { get; set; } = new();
    public EnvironmentDataDto Environment { get; set; } = new();
    public List<PlaceOfInterestDto>? PlacesOfInterest { get; set; }
    public int? LinkedSettlement { get; set; }
}

public class EnvironmentDataDto
{
    public string? Climate { get; set; }
    public string? SoilQuality { get; set; }
    public QualityRangeDto MineralQualityRange { get; set; } = new();
    public ChaosStateDto? Chaos { get; set; }
}

public class QualityRangeDto
{
    public string? Min { get; set; }
    public string? Max { get; set; }
}

public class PlaceOfInterestDto
{
    public string ResRef { get; set; } = string.Empty;
    public string Tag { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Type { get; set; }
    public string? Description { get; set; }
}

public class ChaosStateDto
{
    public int Danger { get; set; }
    public int Corruption { get; set; }
    public int Density { get; set; }
    public int Mutation { get; set; }
}

// ==================== Codex Lore DTOs ====================

public class LoreDefinitionDto
{
    public string LoreId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int Category { get; set; }
    public string? CategoryName { get; set; }
    public int Tier { get; set; }
    public string? Keywords { get; set; }
    public bool IsAlwaysAvailable { get; set; }
    public DateTime CreatedUtc { get; set; }
}

// ==================== Trait Definition DTOs ====================

public class TraitDefinitionDto
{
    public string Tag { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int PointCost { get; set; }
    public string Category { get; set; } = "Background";
    public string DeathBehavior { get; set; } = "Persist";
    public bool RequiresUnlock { get; set; }
    public bool DmOnly { get; set; }
    public List<TraitEffectDto> Effects { get; set; } = [];
    public List<string> AllowedRaces { get; set; } = [];
    public List<string> AllowedClasses { get; set; } = [];
    public List<string> ForbiddenRaces { get; set; } = [];
    public List<string> ForbiddenClasses { get; set; } = [];
    public List<string> ConflictingTraits { get; set; } = [];
    public List<string> PrerequisiteTraits { get; set; } = [];
    public DateTime CreatedUtc { get; set; }
    public DateTime UpdatedUtc { get; set; }
}

public class TraitEffectDto
{
    public int EffectType { get; set; }
    public string? Target { get; set; }
    public int Magnitude { get; set; }
    public string? Description { get; set; }
}

public class EnumValueDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

// ==================== Industry DTOs ====================

public class IndustryDefinitionDto
{
    public string Tag { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<IndustryKnowledgeDto> Knowledge { get; set; } = [];
    public List<IndustryRecipeDto> Recipes { get; set; } = [];
}

public class IndustryKnowledgeDto
{
    public string Tag { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Level { get; set; }
    public int PointCost { get; set; }
    public List<IndustryHarvestEffectDto> HarvestEffects { get; set; } = [];
}

public class IndustryHarvestEffectDto
{
    public string NodeTag { get; set; } = string.Empty;
    public string? StepModified { get; set; }
    public float Value { get; set; }
    public string? Operation { get; set; }
}

public class IndustryRecipeDto
{
    public string RecipeId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string IndustryTag { get; set; } = string.Empty;
    public List<string> RequiredKnowledge { get; set; } = [];
    public string? RequiredProficiency { get; set; }
    public List<IndustryIngredientDto> Ingredients { get; set; } = [];
    public List<IndustryProductDto> Products { get; set; } = [];
    public int? CraftingTimeSeconds { get; set; }
    public int KnowledgePointsAwarded { get; set; }
}

public class IndustryIngredientDto
{
    public string ItemResRef { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int? MinQuality { get; set; }
    public bool IsConsumed { get; set; } = true;
}

public class IndustryProductDto
{
    public string ItemResRef { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int? Quality { get; set; }
    public float? SuccessChance { get; set; }
}

// ==================== Organization DTOs ====================

public class OrganizationDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Type { get; set; } = string.Empty;
    public Guid? ParentOrganizationId { get; set; }
}

public class OrganizationMemberDto
{
    public Guid Id { get; set; }
    public Guid CharacterId { get; set; }
    public Guid OrganizationId { get; set; }
    public string Rank { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime JoinedDate { get; set; }
    public DateTime? DepartedDate { get; set; }
    public string? Notes { get; set; }
    public List<string> Roles { get; set; } = [];
}

public class CreateOrganizationRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public Guid? ParentOrganizationId { get; set; }
}

public class AddMemberRequestDto
{
    public Guid CharacterId { get; set; }
    public string? Rank { get; set; }
}
