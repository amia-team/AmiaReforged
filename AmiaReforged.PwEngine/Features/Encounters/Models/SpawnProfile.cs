using System.ComponentModel.DataAnnotations;

namespace AmiaReforged.PwEngine.Features.Encounters.Models;

/// <summary>
/// A spawn profile defines the dynamic encounter configuration for a specific area.
/// Each area resref in the module can have at most one active profile.
/// When no profile exists or the profile is inactive, the legacy encounter system is used.
/// </summary>
public class SpawnProfile
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// The area resref this profile applies to. Unique index — one profile per area.
    /// </summary>
    [Required]
    [MaxLength(32)]
    public string AreaResRef { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable name for this profile (e.g., "Howling Woods Encounters").
    /// </summary>
    [Required]
    [MaxLength(128)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Whether this profile is active. Inactive profiles allow legacy fallback.
    /// </summary>
    public bool IsActive { get; set; }

    /// <summary>
    /// Cooldown duration in seconds between trigger activations. Default 900 (15 minutes).
    /// </summary>
    public int CooldownSeconds { get; set; } = 900;

    /// <summary>
    /// Duration in seconds before spawned creatures are destroyed. Default 600 (10 minutes).
    /// </summary>
    public int DespawnSeconds { get; set; } = 600;

    /// <summary>
    /// Maximum total number of creatures that can be spawned per encounter trigger.
    /// Null means no cap. Mini-boss spawns are exempt from this limit.
    /// </summary>
    public int? MaxTotalSpawns { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The spawn groups available in this profile.
    /// </summary>
    public virtual List<SpawnGroup> SpawnGroups { get; set; } = [];

    /// <summary>
    /// Bonus effects applied to all creatures spawned by this profile.
    /// </summary>
    public virtual List<SpawnBonus> Bonuses { get; set; } = [];

    /// <summary>
    /// Optional mini-boss configuration for this profile.
    /// </summary>
    public virtual MiniBossConfig? MiniBoss { get; set; }
}
