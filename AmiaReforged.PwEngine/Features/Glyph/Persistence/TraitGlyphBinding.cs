using System.ComponentModel.DataAnnotations;

namespace AmiaReforged.PwEngine.Features.Glyph.Persistence;

/// <summary>
/// Join entity that binds a <see cref="GlyphDefinition"/> to a specific trait tag.
/// When a trait event fires for the bound tag, the associated graph is executed.
/// </summary>
public class TraitGlyphBinding
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// The trait tag this binding is for (e.g., "trait_cursed", "trait_blessed").
    /// </summary>
    [Required]
    [MaxLength(128)]
    public string TraitTag { get; set; } = string.Empty;

    /// <summary>
    /// Foreign key to the Glyph definition being bound.
    /// </summary>
    public Guid GlyphDefinitionId { get; set; }

    /// <summary>
    /// Execution priority when multiple graphs are bound to the same trait event.
    /// Lower numbers execute first. Default 0.
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Navigation property to the glyph definition.
    /// </summary>
    public virtual GlyphDefinition? GlyphDefinition { get; set; }
}
