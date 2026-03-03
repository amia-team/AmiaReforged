using System.ComponentModel.DataAnnotations;

namespace AmiaReforged.PwEngine.Features.Encounters.Models;

/// <summary>
/// A boss creature definition within a <see cref="SpawnProfile"/>'s boss pool.
/// Multiple boss configs per profile are selected via weighted random among eligible,
/// active entries. Unlike spawn groups, individual bosses can be set to inactive to
/// permanently exclude them from consideration.
/// </summary>
public class BossConfig
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// FK to the owning <see cref="SpawnProfile"/>.
    /// </summary>
    public Guid SpawnProfileId { get; set; }

    /// <summary>
    /// The creature resref to spawn as the boss.
    /// </summary>
    [Required]
    [MaxLength(32)]
    public string CreatureResRef { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable name for this boss (e.g., "Ancient Red Dragon", "Lich Lord").
    /// </summary>
    [Required]
    [MaxLength(128)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Relative weight for selection among eligible, active bosses. Higher = more likely.
    /// </summary>
    public int Weight { get; set; } = 1;

    /// <summary>
    /// When false, this boss is never considered for selection regardless of conditions.
    /// Use this to permanently or temporarily disable a boss without deleting it.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// All conditions must pass for this boss to be eligible for selection.
    /// Empty = unconditional (always eligible when active).
    /// </summary>
    public virtual List<BossCondition> Conditions { get; set; } = [];

    /// <summary>
    /// Bonus effects applied specifically to this boss on spawn.
    /// </summary>
    public virtual List<SpawnBonus> Bonuses { get; set; } = [];

    /// <summary>
    /// Navigation property back to the owning profile.
    /// </summary>
    public virtual SpawnProfile SpawnProfile { get; set; } = null!;
}
