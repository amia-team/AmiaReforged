using System.ComponentModel.DataAnnotations;

namespace AmiaReforged.PwEngine.Features.Glyph.Persistence;

/// <summary>Canonical source draft and persisted activation history. Executable IR is derived.</summary>
public class GlyphDefinition
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Human-readable name for this Glyph script (e.g., "Double Spawns at Night").
    /// </summary>
    [Required]
    [MaxLength(128)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of what this script does.
    /// </summary>
    [MaxLength(512)]
    public string? Description { get; set; }

    /// <summary>
    /// The encounter event type this graph listens to.
    /// Stored as a string for forward-compatibility.
    /// </summary>
    [Required]
    [MaxLength(64)]
    public string EventType { get; set; } = string.Empty;

    /// <summary>
    /// The script category (Encounter, Trait, Environmental, Narrative).
    /// Stored as a string for forward-compatibility. Defaults to "Encounter".
    /// </summary>
    [Required]
    [MaxLength(32)]
    public string Category { get; set; } = "Encounter";

    [Required]
    public string SourceText { get; set; } = string.Empty;

    public int LanguageVersion { get; set; } = Language.Compilation.GlyphLanguageVersion.Current;

    /// <summary>Server-owned version records; never accepted from authoring APIs.</summary>
    public string PublishedVersionsJson { get; set; } = "[]";

    /// <summary>
    /// Whether this script definition is active and available for binding.
    /// </summary>
    public bool IsActive { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Profile bindings for this definition. Navigation property.
    /// </summary>
    public virtual List<SpawnProfileGlyphBinding> Bindings { get; set; } = [];
}
