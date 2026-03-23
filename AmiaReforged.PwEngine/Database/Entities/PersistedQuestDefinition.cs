using System.ComponentModel.DataAnnotations;

namespace AmiaReforged.PwEngine.Database.Entities;

/// <summary>
/// EF Core entity for a global quest definition.
/// Shared across all players — individual players track progress separately.
/// Maps to the <c>codex_quest_definitions</c> table in the PwEngine database.
/// </summary>
public class PersistedQuestDefinition
{
    /// <summary>
    /// Unique quest identifier (natural key).
    /// </summary>
    [Key]
    [MaxLength(100)]
    public required string QuestId { get; set; }

    /// <summary>
    /// Display title of the quest entry.
    /// </summary>
    [MaxLength(200)]
    public required string Title { get; set; }

    /// <summary>
    /// Full quest description / body text.
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// JSON-serialized array of quest stage objects.
    /// Each stage has a numeric stage ID, journal text, optional completion flag,
    /// and per-stage hints. Stored as jsonb.
    /// e.g. <c>[{"stageId":10,"journalText":"Find the artifact","isCompletionStage":false,"hints":[]}]</c>
    /// </summary>
    public string StagesJson { get; set; } = "[]";

    /// <summary>
    /// Optional NPC who gives this quest.
    /// </summary>
    [MaxLength(200)]
    public string? QuestGiver { get; set; }

    /// <summary>
    /// Optional location where this quest takes place.
    /// </summary>
    [MaxLength(200)]
    public string? Location { get; set; }

    /// <summary>
    /// Comma-separated lowercase keywords for searching / filtering.
    /// </summary>
    [MaxLength(1000)]
    public string? Keywords { get; set; }

    /// <summary>
    /// JSON-serialized reward mix granted when the entire quest is completed.
    /// Stored as jsonb.
    /// e.g. <c>{"xp":500,"gold":1000,"knowledgePoints":3,"proficiencies":[{"industryTag":"alchemy","proficiencyXp":50}]}</c>
    /// </summary>
    public string CompletionRewardJson { get; set; } = "{}";

    /// <summary>
    /// When <c>true</c>, this quest entry is visible to every player without
    /// requiring an unlock / trigger.
    /// </summary>
    public bool IsAlwaysAvailable { get; set; }

    /// <summary>
    /// UTC timestamp when the definition was first persisted.
    /// </summary>
    public DateTime CreatedUtc { get; set; }
}
