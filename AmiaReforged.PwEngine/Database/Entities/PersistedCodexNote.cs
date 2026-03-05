using System.ComponentModel.DataAnnotations;

namespace AmiaReforged.PwEngine.Database.Entities;

/// <summary>
/// EF Core entity for persisting codex notes.
/// Maps to the <c>codex_notes</c> table in the PwEngine database.
/// </summary>
public class PersistedCodexNote
{
    /// <summary>
    /// Unique note identifier.
    /// </summary>
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// The character this note belongs to (FK to persisted_characters).
    /// </summary>
    public Guid CharacterId { get; set; }

    /// <summary>
    /// Optional title for the note.
    /// </summary>
    [MaxLength(200)]
    public string? Title { get; set; }

    /// <summary>
    /// Note body content.
    /// </summary>
    public required string Content { get; set; }

    /// <summary>
    /// Integer value of NoteCategory enum.
    /// </summary>
    public int Category { get; set; }

    /// <summary>
    /// Whether this note was created by a DM.
    /// </summary>
    public bool IsDmNote { get; set; }

    /// <summary>
    /// Whether this note is private (visible only to the creator).
    /// </summary>
    public bool IsPrivate { get; set; }

    /// <summary>
    /// UTC timestamp when the note was created.
    /// </summary>
    public DateTime CreatedUtc { get; set; }

    /// <summary>
    /// UTC timestamp when the note was last modified.
    /// </summary>
    public DateTime ModifiedUtc { get; set; }
}
