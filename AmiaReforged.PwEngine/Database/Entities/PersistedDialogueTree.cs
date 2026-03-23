using System.ComponentModel.DataAnnotations;

namespace AmiaReforged.PwEngine.Database.Entities;

/// <summary>
/// EF Core entity for a dialogue tree definition.
/// Stores the complete node graph as a JSON column for simplicity.
/// Maps to the <c>dialogue_trees</c> table in the PwEngine database.
/// </summary>
public class PersistedDialogueTree
{
    /// <summary>
    /// Unique dialogue tree identifier (natural key).
    /// </summary>
    [Key]
    [MaxLength(100)]
    public required string DialogueTreeId { get; set; }

    /// <summary>
    /// Display title for admin panel identification.
    /// </summary>
    [MaxLength(200)]
    public required string Title { get; set; }

    /// <summary>
    /// Designer notes / description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// GUID of the root node (serialized as string).
    /// </summary>
    [MaxLength(36)]
    public string? RootNodeId { get; set; }

    /// <summary>
    /// Default NPC creature tag that speaks this dialogue.
    /// Used for lookup when an NPC is interacted with.
    /// </summary>
    [MaxLength(64)]
    public string? SpeakerTag { get; set; }

    /// <summary>
    /// JSON-serialized array of all dialogue nodes including their choices, actions, and conditions.
    /// Stored as jsonb in PostgreSQL.
    /// </summary>
    public string NodesJson { get; set; } = "[]";

    /// <summary>
    /// UTC timestamp when the definition was first persisted.
    /// </summary>
    public DateTime CreatedUtc { get; set; }

    /// <summary>
    /// UTC timestamp when the definition was last updated.
    /// </summary>
    public DateTime? UpdatedUtc { get; set; }

}
