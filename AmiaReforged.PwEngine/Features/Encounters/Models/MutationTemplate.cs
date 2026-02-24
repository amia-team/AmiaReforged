using System.ComponentModel.DataAnnotations;

namespace AmiaReforged.PwEngine.Features.Encounters.Models;

/// <summary>
/// A global mutation template that can be applied to any spawned creature.
/// At spawn time, the system rolls against the region's Mutation chaos axis.
/// If that gate succeeds, it shuffles all active templates and rolls each
/// template's <see cref="SpawnChancePercent"/> — the first to succeed wins
/// and its prefix is prepended to the creature name, and its effects are applied.
/// </summary>
public class MutationTemplate
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// The prefix prepended to the creature's name (e.g., "Frenzied").
    /// </summary>
    [Required]
    [MaxLength(64)]
    public string Prefix { get; set; } = string.Empty;

    /// <summary>
    /// Optional description for admin reference.
    /// </summary>
    [MaxLength(256)]
    public string? Description { get; set; }

    /// <summary>
    /// Percentage chance (0–100) for this mutation to be selected when rolling.
    /// </summary>
    public int SpawnChancePercent { get; set; } = 10;

    /// <summary>
    /// Whether this mutation is active. Inactive mutations are skipped during rolling.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// The effects applied to the creature when this mutation fires.
    /// </summary>
    public virtual List<MutationEffect> Effects { get; set; } = [];
}
