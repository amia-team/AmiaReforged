using System.ComponentModel.DataAnnotations;

namespace AmiaReforged.PwEngine.Features.Encounters.Models;

/// <summary>
/// A bonus effect applied to spawned creatures. This is a separate layer from the legacy
/// addon system (Greater/Cagey/Retribution/Ghostly) and stacks alongside it.
/// Bonuses can be profile-wide or attached to a mini-boss config.
/// </summary>
public class SpawnBonus
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// FK to the owning <see cref="SpawnProfile"/>. Null if attached to a MiniBossConfig instead.
    /// </summary>
    public Guid? SpawnProfileId { get; set; }

    /// <summary>
    /// FK to the owning <see cref="MiniBossConfig"/>. Null if profile-wide.
    /// </summary>
    public Guid? MiniBossConfigId { get; set; }

    /// <summary>
    /// Human-readable name for this bonus (e.g., "Chaos Tempering", "Region AC Buff").
    /// </summary>
    [Required]
    [MaxLength(128)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The effect type to apply.
    /// </summary>
    public SpawnBonusType Type { get; set; }

    /// <summary>
    /// Base value for the effect. Actual value may be scaled by chaos mutation axis.
    /// </summary>
    public int Value { get; set; }

    /// <summary>
    /// Duration of the effect in seconds. 0 = permanent (for the creature's lifetime).
    /// </summary>
    public int DurationSeconds { get; set; }

    /// <summary>
    /// Whether this bonus is active. Inactive bonuses are skipped during application.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Navigation property back to the owning profile (if profile-wide).
    /// </summary>
    public virtual SpawnProfile? SpawnProfile { get; set; }

    /// <summary>
    /// Navigation property back to the owning mini-boss config (if mini-boss specific).
    /// </summary>
    public virtual MiniBossConfig? MiniBossConfig { get; set; }
}
