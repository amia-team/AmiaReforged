using System.ComponentModel.DataAnnotations;

namespace AmiaReforged.PwEngine.Features.Glyph.Persistence;

/// <summary>
/// Persisted Glyph script definition. The <see cref="GraphJson"/> column stores the
/// complete serialized <see cref="Core.GlyphGraph"/> (nodes, edges, variables, positions)
/// as a JSON string. Graphs are always loaded and saved as a unit.
/// </summary>
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

    /// <summary>
    /// The complete serialized graph (nodes, edges, variables, canvas positions).
    /// </summary>
    [Required]
    public string GraphJson { get; set; } = "{}";

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
