using System.ComponentModel.DataAnnotations;

namespace AmiaReforged.PwEngine.Features.Encounters.Models;

/// <summary>
/// Configuration for a mini-boss that can spawn in the area alongside regular encounters.
/// Spawns as a single creature with its own bonus set.
/// </summary>
public class MiniBossConfig
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// FK to the owning <see cref="SpawnProfile"/>.
    /// </summary>
    public Guid SpawnProfileId { get; set; }

    /// <summary>
    /// The creature resref to spawn as the mini-boss.
    /// </summary>
    [Required]
    [MaxLength(32)]
    public string CreatureResRef { get; set; } = string.Empty;

    /// <summary>
    /// Percentage chance (0–100) for the mini-boss to spawn per encounter trigger.
    /// </summary>
    public int SpawnChancePercent { get; set; } = 5;

    /// <summary>
    /// Bonus effects applied specifically to the mini-boss on spawn.
    /// </summary>
    public virtual List<SpawnBonus> Bonuses { get; set; } = [];

    /// <summary>
    /// Navigation property back to the owning profile.
    /// </summary>
    public virtual SpawnProfile SpawnProfile { get; set; } = null!;
}
