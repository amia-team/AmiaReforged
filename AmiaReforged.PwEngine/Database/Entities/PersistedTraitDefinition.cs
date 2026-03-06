using System.ComponentModel.DataAnnotations;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits;

namespace AmiaReforged.PwEngine.Database.Entities;

/// <summary>
/// EF Core entity for a global trait definition.
/// Defines the template for a character trait including its effects, restrictions,
/// death behavior, and eligibility rules.
/// Maps to the <c>trait_definitions</c> table in the PwEngine database.
/// </summary>
public class PersistedTraitDefinition
{
    /// <summary>
    /// Unique trait tag identifier (natural key, e.g. "brave", "hero").
    /// </summary>
    [Key]
    [MaxLength(50)]
    public required string Tag { get; set; }

    /// <summary>
    /// Display name shown to players.
    /// </summary>
    [MaxLength(200)]
    public required string Name { get; set; }

    /// <summary>
    /// Full description explaining what the trait does.
    /// </summary>
    public required string Description { get; set; }

    /// <summary>
    /// Point cost for trait selection. Negative values represent drawback traits that grant points.
    /// </summary>
    public int PointCost { get; set; }

    /// <summary>
    /// Broad category (Background, Personality, Physical, etc.).
    /// Stored as the enum name string.
    /// </summary>
    public TraitCategory Category { get; set; } = TraitCategory.Background;

    /// <summary>
    /// How this trait behaves on character death (Persist, ResetOnDeath, Permadeath, RemoveOnDeath).
    /// Stored as the enum name string.
    /// </summary>
    public TraitDeathBehavior DeathBehavior { get; set; } = TraitDeathBehavior.Persist;

    /// <summary>
    /// Whether this trait must be unlocked via a quest or special achievement before player selection.
    /// </summary>
    public bool RequiresUnlock { get; set; }

    /// <summary>
    /// Whether this trait can only be granted via a DM command, never player-selectable.
    /// </summary>
    public bool DmOnly { get; set; }

    /// <summary>
    /// Mechanical effects as JSON array.
    /// Each element: { "effectType": int, "target": string?, "magnitude": int, "description": string? }
    /// </summary>
    public string EffectsJson { get; set; } = "[]";

    /// <summary>
    /// JSON array of race names allowed to select this trait. Empty array means no restriction.
    /// </summary>
    public string AllowedRacesJson { get; set; } = "[]";

    /// <summary>
    /// JSON array of class names allowed to select this trait. Empty array means no restriction.
    /// </summary>
    public string AllowedClassesJson { get; set; } = "[]";

    /// <summary>
    /// JSON array of race names explicitly forbidden from selecting this trait.
    /// </summary>
    public string ForbiddenRacesJson { get; set; } = "[]";

    /// <summary>
    /// JSON array of class names explicitly forbidden from selecting this trait.
    /// </summary>
    public string ForbiddenClassesJson { get; set; } = "[]";

    /// <summary>
    /// JSON array of trait tags that conflict with this trait (mutual exclusion).
    /// </summary>
    public string ConflictingTraitsJson { get; set; } = "[]";

    /// <summary>
    /// JSON array of trait tags that must be selected before this trait becomes available.
    /// </summary>
    public string PrerequisiteTraitsJson { get; set; } = "[]";

    /// <summary>
    /// UTC timestamp when the definition was first created.
    /// </summary>
    public DateTime CreatedUtc { get; set; }

    /// <summary>
    /// UTC timestamp when the definition was last updated.
    /// </summary>
    public DateTime UpdatedUtc { get; set; }
}
