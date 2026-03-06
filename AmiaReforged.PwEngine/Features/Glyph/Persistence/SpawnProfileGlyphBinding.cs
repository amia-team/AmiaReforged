using System.ComponentModel.DataAnnotations;
using AmiaReforged.PwEngine.Features.Encounters.Models;

namespace AmiaReforged.PwEngine.Features.Glyph.Persistence;

/// <summary>
/// Join entity that binds a <see cref="GlyphDefinition"/> to a <see cref="SpawnProfile"/>.
/// Allows many-to-many: a script can be reused across multiple profiles, and a profile
/// can have multiple scripts attached (for different or the same event types).
/// </summary>
public class SpawnProfileGlyphBinding
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// Foreign key to the spawn profile this script is attached to.
    /// </summary>
    public Guid SpawnProfileId { get; set; }

    /// <summary>
    /// Foreign key to the Glyph definition being bound.
    /// </summary>
    public Guid GlyphDefinitionId { get; set; }

    /// <summary>
    /// Execution priority when multiple graphs are bound to the same event on the same profile.
    /// Lower numbers execute first. Default 0.
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Navigation property to the spawn profile.
    /// </summary>
    public virtual SpawnProfile? SpawnProfile { get; set; }

    /// <summary>
    /// Navigation property to the glyph definition.
    /// </summary>
    public virtual GlyphDefinition? GlyphDefinition { get; set; }
}
