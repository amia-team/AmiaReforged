namespace AmiaReforged.AdminPanel.Models;

// ==================== Item Blueprint DTOs ====================

public class ItemBlueprintDto
{
    public string ResRef { get; set; } = string.Empty;
    public string ItemTag { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string[]? Materials { get; set; }
    public string? ItemForm { get; set; }
    public int BaseItemType { get; set; }
    public AppearanceDataDto? Appearance { get; set; }
    public List<LocalVariableDto>? LocalVariables { get; set; }
    public int BaseValue { get; set; } = 1;
    public int WeightIncreaseConstant { get; set; } = -1;
    public List<MaterialVariantDto>? Variants { get; set; }
    public bool IsTemplate { get; set; }
}

public class MaterialVariantDto
{
    public string Material { get; set; } = string.Empty;
    public AppearanceDataDto? Appearance { get; set; }
    public int? BaseValueOverride { get; set; }
}

public class LocalVariableDto
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "String";
    public object? Value { get; set; }
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
    public TreePropertiesDto? TreeProperties { get; set; }
    public string? MinQuality { get; set; }
    public string? MaxQuality { get; set; }
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

public class TreePropertiesDto
{
    public int MinLogs { get; set; } = 1;
    public int MaxLogs { get; set; } = 3;
    public string LogItemTag { get; set; } = string.Empty;
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

// ==================== Codex Quest DTOs ====================

public class QuestStageDto
{
    /// <summary>NWN-style numeric stage ID (e.g. 10, 20, 30). Gaps allowed for patching.</summary>
    public int StageId { get; set; }

    /// <summary>Journal text displayed to the player when this stage is reached.</summary>
    public string JournalText { get; set; } = string.Empty;

    /// <summary>If true, reaching this stage marks the quest as completed.</summary>
    public bool IsCompletionStage { get; set; }

    /// <summary>Optional hints revealed at this stage.</summary>
    public List<string> Hints { get; set; } = [];

    /// <summary>Objective groups that must be satisfied to advance past this stage.</summary>
    public List<ObjectiveGroupDto> ObjectiveGroups { get; set; } = [];

    /// <summary>Rewards granted when this stage is completed.</summary>
    public RewardMixDto? Rewards { get; set; }
}

public class ObjectiveGroupDto
{
    /// <summary>Display name for this group (e.g. "Find the stolen artifacts").</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>How objectives in this group must be satisfied: All, Any, or Sequence.</summary>
    public string CompletionMode { get; set; } = "All";

    /// <summary>The objectives in this group.</summary>
    public List<ObjectiveDefinitionDto> Objectives { get; set; } = [];
}

public class ObjectiveDefinitionDto
{
    /// <summary>Unique objective identifier (auto-generated if empty on create).</summary>
    public string ObjectiveId { get; set; } = string.Empty;

    /// <summary>Evaluator type tag: kill, collect, reach_location, dialog_choice, investigate, escort, composite.</summary>
    public string TypeTag { get; set; } = string.Empty;

    /// <summary>Player-visible description (e.g. "Kill 5 goblins").</summary>
    public string DisplayText { get; set; } = string.Empty;

    /// <summary>Tag of the target entity this objective watches for.</summary>
    public string? TargetTag { get; set; }

    /// <summary>Number of target events required (for counter-based objectives).</summary>
    public int RequiredCount { get; set; } = 1;

    /// <summary>Evaluator-specific configuration (clue graph, state machine, waypoints, etc.).</summary>
    public Dictionary<string, object>? Config { get; set; }
}

public class RewardMixDto
{
    /// <summary>Experience points awarded.</summary>
    public int Xp { get; set; }

    /// <summary>Gold pieces awarded.</summary>
    public int Gold { get; set; }

    /// <summary>Knowledge points awarded.</summary>
    public int KnowledgePoints { get; set; }

    /// <summary>Per-industry proficiency XP grants.</summary>
    public List<ProficiencyRewardDto> Proficiencies { get; set; } = [];

    /// <summary>True when all reward values are zero/empty.</summary>
    public bool IsEmpty => Xp == 0 && Gold == 0 && KnowledgePoints == 0 && Proficiencies.Count == 0;
}

public class ProficiencyRewardDto
{
    /// <summary>Tag of the industry (e.g. "alchemy", "smithing").</summary>
    public string IndustryTag { get; set; } = string.Empty;

    /// <summary>Amount of proficiency XP to award.</summary>
    public int ProficiencyXp { get; set; }
}

public class QuestDefinitionDto
{
    public string QuestId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<QuestStageDto> Stages { get; set; } = [];
    public RewardMixDto? CompletionReward { get; set; }
    public string? QuestGiver { get; set; }
    public string? Location { get; set; }
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
    public List<string> Prerequisites { get; set; } = [];
    public string? Branch { get; set; }
    public List<IndustryKnowledgeEffectDto> Effects { get; set; } = [];
    public List<IndustryCraftingModifierDto> CraftingModifiers { get; set; } = [];
}

public class IndustryKnowledgeEffectDto
{
    public string EffectType { get; set; } = string.Empty;
    public string TargetTag { get; set; } = string.Empty;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class IndustryHarvestEffectDto
{
    public string NodeTag { get; set; } = string.Empty;
    public string? StepModified { get; set; }
    public float Value { get; set; }
    public string? Operation { get; set; }
}

public class IndustryCraftingModifierDto
{
    public string TargetTag { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public string StepModified { get; set; } = string.Empty;
    public float Value { get; set; }
    public string Operation { get; set; } = string.Empty;
}

public class IndustryRecipeDto
{
    public string RecipeId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string IndustryTag { get; set; } = string.Empty;
    public List<string> RequiredKnowledge { get; set; } = [];
    public List<IndustryIngredientDto> Ingredients { get; set; } = [];
    public List<IndustryProductDto> Products { get; set; } = [];
    public int? CraftingTimeRounds { get; set; }
    public int ProgressionPointsAwarded { get; set; }
    public int ProficiencyXpAwarded { get; set; }
    public string? RequiredWorkstation { get; set; }
    public List<ToolRequirementDto> RequiredTools { get; set; } = [];
}

public class IndustryIngredientDto
{
    public string ItemTag { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int? MinQuality { get; set; }
    public bool IsConsumed { get; set; } = true;
}

public class IndustryProductDto
{
    public string ItemTag { get; set; } = string.Empty;
    public int Quantity { get; set; }
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

// ==================== Workstation DTOs ====================

public class WorkstationDefinitionDto
{
    public string Tag { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? PlaceableResRef { get; set; }
    public int? AppearanceId { get; set; }
    public List<string> SupportedIndustries { get; set; } = [];
}

// ==================== Coinhouse (Bank) DTOs ====================

public class CoinhouseDto
{
    public long Id { get; set; }
    public string Tag { get; set; } = string.Empty;
    public int Settlement { get; set; }
    public Guid EngineId { get; set; }
    public int StoredGold { get; set; }
    public string? PersonaIdString { get; set; }
    public int AccountCount { get; set; }
    public int TotalDeposits { get; set; }
    public int TotalCredits { get; set; }
}

// ==================== Interaction Definition DTOs ====================

public class InteractionDefinitionDto
{
    public string Tag { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string TargetMode { get; set; } = "Trigger";
    public int BaseRounds { get; set; } = 4;
    public int MinRounds { get; set; } = 2;
    public bool ProficiencyReducesRounds { get; set; } = true;
    public bool RequiresIndustryMembership { get; set; } = true;
    public List<string> RequiredIndustryTags { get; set; } = [];
    public List<string> AllowedAreaResRefs { get; set; } = [];
    public List<string> RequiredKnowledgeTags { get; set; } = [];
    public List<InteractionResponseDto> Responses { get; set; } = [];
    public DateTime? CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class InteractionResponseDto
{
    public string ResponseTag { get; set; } = string.Empty;
    public int Weight { get; set; } = 1;
    public string? MinProficiency { get; set; }
    public string? Message { get; set; }
    public List<InteractionResponseEffectDto> Effects { get; set; } = [];
}

public class InteractionResponseEffectDto
{
    public string EffectType { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public Dictionary<string, object>? Metadata { get; set; }
}

// ==================== Recipe Template DTOs ====================

public class RecipeTemplateDefinitionDto
{
    public string Tag { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string IndustryTag { get; set; } = string.Empty;
    public List<string> RequiredKnowledge { get; set; } = [];
    public List<TemplateIngredientDto> Ingredients { get; set; } = [];
    public List<TemplateProductDto> Products { get; set; } = [];
    public int? CraftingTimeRounds { get; set; }
    public int ProgressionPointsAwarded { get; set; }
    public int ProficiencyXpAwarded { get; set; }
    public string? RequiredWorkstation { get; set; }
    public List<ToolRequirementDto> RequiredTools { get; set; } = [];
}

public class TemplateIngredientDto
{
    public string RequiredCategory { get; set; } = string.Empty;
    public string RequiredForm { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public int? MinQuality { get; set; }
    public bool IsConsumed { get; set; } = true;
    public int SlotIndex { get; set; }
}

public class TemplateProductDto
{
    public string OutputForm { get; set; } = string.Empty;
    public int MaterialSourceSlot { get; set; }
    public int Quantity { get; set; }
    public float? SuccessChance { get; set; }
}

public class ItemEnumsDto
{
    public List<EnumOptionDto> Materials { get; set; } = [];
    public List<EnumOptionDto> ItemForms { get; set; } = [];
}

public class RecipeTemplateEnumsDto
{
    public List<EnumOptionDto> MaterialCategories { get; set; } = [];
    public List<EnumOptionDto> ItemForms { get; set; } = [];
    public List<EnumOptionDto> ToolForms { get; set; } = [];
    public List<EnumOptionDto> Materials { get; set; } = [];
}

public class EnumOptionDto
{
    public string Value { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string? Group { get; set; }
    public string? Category { get; set; }
}

public class ToolRequirementDto
{
    public string? RequiredForm { get; set; }
    public string? RequiredMaterial { get; set; }
    public int? MinQuality { get; set; }
    public string? ExactItemTag { get; set; }
}

// ==================== Knowledge Progression DTOs ====================

public class ProgressionConfigDto
{
    public int BaseCost { get; set; } = 100;
    public float ScalingFactor { get; set; } = 1.15f;
    public string CurveType { get; set; } = "Exponential";
    public int SoftCap { get; set; } = 100;
    public int HardCap { get; set; } = 150;
    public float SoftCapPenaltyMultiplier { get; set; } = 3.0f;
}

public class KnowledgeCapProfileDto
{
    public string Tag { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int SoftCap { get; set; } = 100;
    public int HardCap { get; set; } = 150;
}
