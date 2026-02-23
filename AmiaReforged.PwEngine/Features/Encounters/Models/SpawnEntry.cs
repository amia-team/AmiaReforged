using System.ComponentModel.DataAnnotations;

namespace AmiaReforged.PwEngine.Features.Encounters.Models;

/// <summary>
/// A creature definition within a <see cref="SpawnGroup"/>.
/// Multiple entries in a group are selected via weighted random.
/// </summary>
public class SpawnEntry
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// FK to the owning <see cref="SpawnGroup"/>.
    /// </summary>
    public Guid SpawnGroupId { get; set; }

    /// <summary>
    /// The creature resref to spawn.
    /// </summary>
    [Required]
    [MaxLength(32)]
    public string CreatureResRef { get; set; } = string.Empty;

    /// <summary>
    /// Relative weight for selection within the group. Higher = more likely to be chosen.
    /// </summary>
    public int RelativeWeight { get; set; } = 1;

    /// <summary>
    /// Minimum number of this creature type to spawn per encounter.
    /// </summary>
    public int MinCount { get; set; } = 1;

    /// <summary>
    /// Maximum number of this creature type to spawn per encounter.
    /// </summary>
    public int MaxCount { get; set; } = 4;

    /// <summary>
    /// Navigation property back to the owning group.
    /// </summary>
    public virtual SpawnGroup SpawnGroup { get; set; } = null!;
}
