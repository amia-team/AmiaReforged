using System.ComponentModel.DataAnnotations;

namespace AmiaReforged.PwEngine.Features.Encounters.Models;

/// <summary>
/// A spawn group is a collection of creature entries that can be selected together
/// when all of the group's conditions are satisfied. Groups within a profile are
/// chosen via weighted random selection among eligible groups.
/// </summary>
public class SpawnGroup
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// FK to the owning <see cref="SpawnProfile"/>.
    /// </summary>
    public Guid SpawnProfileId { get; set; }

    /// <summary>
    /// Human-readable name (e.g., "Night Wolves", "Daytime Bandits").
    /// </summary>
    [Required]
    [MaxLength(128)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Relative weight for selection among eligible groups. Higher = more likely.
    /// </summary>
    public int Weight { get; set; } = 1;

    /// <summary>
    /// All conditions must pass for this group to be eligible.
    /// </summary>
    public virtual List<SpawnCondition> Conditions { get; set; } = [];

    /// <summary>
    /// Creature entries that can be spawned from this group.
    /// </summary>
    public virtual List<SpawnEntry> Entries { get; set; } = [];

    /// <summary>
    /// When true, only the mutations listed in <see cref="MutationOverrides"/> are
    /// considered for this group's creatures (using each override's custom chance).
    /// If true but no overrides are defined, no mutations are ever applied.
    /// When false, the global mutation pool is used as normal.
    /// </summary>
    public bool OverrideMutations { get; set; }

    /// <summary>
    /// Per-group mutation overrides with custom appearance chances.
    /// Only used when <see cref="OverrideMutations"/> is true.
    /// </summary>
    public virtual List<GroupMutationOverride> MutationOverrides { get; set; } = [];

    /// <summary>
    /// Navigation property back to the owning profile.
    /// </summary>
    public virtual SpawnProfile SpawnProfile { get; set; } = null!;
}
