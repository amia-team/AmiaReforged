using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace AmiaReforged.Shared.Quests;

// ==================== Codex Quest DTOs ====================

public class QuestStageDto
{
    /// <summary>NWN-style numeric stage ID (e.g. 10, 20, 30). Gaps allowed for patching.</summary>
    public int StageId { get; set; }

    /// <summary>Optional authoring name displayed in the stage list and links.</summary>
    public string? Name { get; set; }

    /// <summary>Journal text displayed to the player when this stage is reached.</summary>
    public string JournalText { get; set; } = string.Empty;

    /// <summary>If true, reaching this stage marks the quest as completed. Prefer QuestState for new quests.</summary>
    public bool IsCompletionStage { get; set; }

    /// <summary>
    /// The quest state applied when the player reaches this stage.
    /// Values: Discovered, InProgress, Completed, Failed, Abandoned, Expired.
    /// Null means no state change.
    /// </summary>
    public string? QuestState { get; set; }

    /// <summary>
    /// Explicit next-stage override. When all objective groups complete and no group-level
    /// CompletionStageId fires first, the quest advances to this stage.
    /// If null, falls back to the next numeric stage ID.
    /// </summary>
    public int? NextStageId { get; set; }

    private List<string>? _hints;
    /// <summary>Optional hints revealed at this stage.</summary>
    public List<string> Hints
    {
        get => _hints ??= [];
        set => _hints = value ?? [];
    }

    private List<ObjectiveGroupDto>? _objectiveGroups;
    /// <summary>Objective groups that must be satisfied to advance past this stage.</summary>
    public List<ObjectiveGroupDto> ObjectiveGroups
    {
        get => _objectiveGroups ??= [];
        set => _objectiveGroups = value ?? [];
    }

    /// <summary>Rewards granted when this stage is completed.</summary>
    public RewardMixDto? Rewards { get; set; }
}

public class ObjectiveGroupDto
{
    /// <summary>Display name for this group (e.g. "Find the stolen artifacts").</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Optional stage ID the quest advances to when this specific group completes.
    /// Overrides the stage-level NextStageId. Enables branching quest paths.
    /// </summary>
    public int? CompletionStageId { get; set; }

    /// <summary>How objectives in this group must be satisfied: All, Any, or Sequence.</summary>
    public string CompletionMode { get; set; } = "All";

    /// <summary>The objectives in this group.</summary>
    private List<ObjectiveDefinitionDto>? _objectives;
    public List<ObjectiveDefinitionDto> Objectives
    {
        get => _objectives ??= [];
        set => _objectives = value ?? [];
    }
}

public class ObjectiveDefinitionDto
{
    /// <summary>Unique objective identifier within the quest.</summary>
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
    private Dictionary<string, object>? _config;
    public Dictionary<string, object> Config
    {
        get => _config ??= new();
        set => _config = value ?? new();
    }
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
    private List<ProficiencyRewardDto>? _proficiencies;
    public List<ProficiencyRewardDto> Proficiencies
    {
        get => _proficiencies ??= [];
        set => _proficiencies = value ?? [];
    }

    /// <summary>True when all reward values are zero/empty.</summary>
    [JsonIgnore]
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
    [Required]
    [StringLength(100)]
    public string QuestId { get; set; } = string.Empty;

    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    private List<QuestStageDto>? _stages;
    public List<QuestStageDto> Stages
    {
        get => _stages ??= [];
        set => _stages = value ?? [];
    }
    public RewardMixDto? CompletionReward { get; set; }
    public string? QuestGiver { get; set; }
    public string? Location { get; set; }
    public string? Keywords { get; set; }
    public bool IsAlwaysAvailable { get; set; }
    public DateTime CreatedUtc { get; set; }
}
