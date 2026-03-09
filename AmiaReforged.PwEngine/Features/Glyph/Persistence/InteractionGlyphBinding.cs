using System.ComponentModel.DataAnnotations;

namespace AmiaReforged.PwEngine.Features.Glyph.Persistence;

/// <summary>
/// Join entity that binds a <see cref="GlyphDefinition"/> to a specific interaction tag.
/// When an interaction lifecycle event fires for the bound tag, the associated graph is executed.
/// Optionally scoped to a specific area ResRef for location-specific behavior.
/// </summary>
public class InteractionGlyphBinding
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// The interaction definition tag this binding is for (e.g., "prospect_minerals", "harvest_wood").
    /// </summary>
    [Required]
    [MaxLength(128)]
    public string InteractionTag { get; set; } = string.Empty;

    /// <summary>
    /// Optional area ResRef to restrict this binding to a specific area.
    /// When null, the binding applies to all areas (global for this tag).
    /// </summary>
    [MaxLength(32)]
    public string? AreaResRef { get; set; }

    /// <summary>
    /// Foreign key to the Glyph definition being bound.
    /// </summary>
    public Guid GlyphDefinitionId { get; set; }

    /// <summary>
    /// Execution priority when multiple graphs are bound to the same interaction event.
    /// Lower numbers execute first. Default 0.
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Navigation property to the glyph definition.
    /// </summary>
    public virtual GlyphDefinition? GlyphDefinition { get; set; }
}
