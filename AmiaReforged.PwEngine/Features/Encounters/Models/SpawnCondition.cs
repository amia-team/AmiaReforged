using System.ComponentModel.DataAnnotations;

namespace AmiaReforged.PwEngine.Features.Encounters.Models;

/// <summary>
/// A condition that must be satisfied for its parent <see cref="SpawnGroup"/> to be eligible.
/// All conditions on a group are ANDed together.
/// </summary>
public class SpawnCondition
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// FK to the owning <see cref="SpawnGroup"/>.
    /// </summary>
    public Guid SpawnGroupId { get; set; }

    /// <summary>
    /// The type of condition to evaluate.
    /// </summary>
    public SpawnConditionType Type { get; set; }

    /// <summary>
    /// Comparison operator. Interpretation depends on <see cref="Type"/>:
    /// <list type="bullet">
    ///   <item>TimeOfDay: "between" (value = "06:00-18:00")</item>
    ///   <item>ChaosThreshold: ">=", ">", "==", "&lt;=", "&lt;" (value = "Danger:50")</item>
    ///   <item>MinPlayerCount / MaxPlayerCount: ">=" / "&lt;=" (value = "3")</item>
    ///   <item>RegionTag: "==" (value = "region_cordor")</item>
    ///   <item>Custom: ignored</item>
    /// </list>
    /// </summary>
    [MaxLength(16)]
    public string Operator { get; set; } = "==";

    /// <summary>
    /// Serialized value for the condition. Format depends on <see cref="Type"/>.
    /// </summary>
    [Required]
    [MaxLength(256)]
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Navigation property back to the owning group.
    /// </summary>
    public virtual SpawnGroup SpawnGroup { get; set; } = null!;
}
