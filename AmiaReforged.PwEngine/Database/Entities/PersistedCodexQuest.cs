using System.ComponentModel.DataAnnotations;

namespace AmiaReforged.PwEngine.Database.Entities;

/// <summary>
/// EF Core entity for persisting a player's quest entry in their codex.
/// Maps to the <c>codex_quests</c> table in the PwEngine database.
/// Composite key: (CharacterId, QuestId).
/// </summary>
public class PersistedCodexQuest
{
    /// <summary>
    /// The character this quest belongs to.
    /// </summary>
    public Guid CharacterId { get; set; }

    /// <summary>
    /// The quest definition ID (natural key from <c>codex_quest_definitions</c>).
    /// </summary>
    [MaxLength(100)]
    public required string QuestId { get; set; }

    /// <summary>
    /// Display title (cached from the definition at time of discovery).
    /// </summary>
    [MaxLength(200)]
    public required string Title { get; set; }

    /// <summary>
    /// Quest description / body text.
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// Integer value of QuestState enum.
    /// </summary>
    public int State { get; set; }

    /// <summary>
    /// The numeric stage ID the player has reached (0 = not started).
    /// </summary>
    public int CurrentStageId { get; set; }

    /// <summary>
    /// When the quest was first added to the codex.
    /// </summary>
    public DateTime DateStarted { get; set; }

    /// <summary>
    /// When the quest reached a terminal state (Completed/Failed/Abandoned/Expired).
    /// </summary>
    public DateTime? DateCompleted { get; set; }

    /// <summary>
    /// Optional NPC or source who gave this quest.
    /// </summary>
    [MaxLength(200)]
    public string? QuestGiver { get; set; }

    /// <summary>
    /// Optional location where the quest was acquired.
    /// </summary>
    [MaxLength(200)]
    public string? Location { get; set; }

    /// <summary>
    /// Comma-separated keywords for searching / filtering.
    /// </summary>
    [MaxLength(1000)]
    public string? Keywords { get; set; }

    /// <summary>
    /// JSON-serialized array of QuestStage objects (cached from definition).
    /// Stored as jsonb.
    /// </summary>
    public string StagesJson { get; set; } = "[]";

    /// <summary>
    /// Optional link to the dynamic quest template that spawned this entry.
    /// </summary>
    [MaxLength(100)]
    public string? SourceTemplateId { get; set; }

    /// <summary>
    /// UTC deadline for dynamic quest completion. Null = no time limit.
    /// </summary>
    public DateTime? Deadline { get; set; }

    /// <summary>
    /// What happens when the deadline elapses (integer value of ExpiryBehavior enum).
    /// </summary>
    public int? ExpiryBehavior { get; set; }

    /// <summary>
    /// How many times this character has completed this quest (for repeatables).
    /// </summary>
    public int CompletionCount { get; set; }
}
