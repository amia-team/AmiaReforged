using System.ComponentModel.DataAnnotations;

namespace AmiaReforged.PwEngine.Features.Encounters.Models;

/// <summary>
/// A condition that must be satisfied for its parent <see cref="BossConfig"/> to be eligible.
/// All conditions on a boss config are ANDed together. Reuses the same
/// <see cref="SpawnConditionType"/> enum and evaluation logic as <see cref="SpawnCondition"/>.
/// </summary>
public class BossCondition
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// FK to the owning <see cref="BossConfig"/>.
    /// </summary>
    public Guid BossConfigId { get; set; }

    /// <summary>
    /// The type of condition to evaluate.
    /// </summary>
    public SpawnConditionType Type { get; set; }

    /// <summary>
    /// Comparison operator. Interpretation depends on <see cref="Type"/>.
    /// Same semantics as <see cref="SpawnCondition.Operator"/>.
    /// </summary>
    [MaxLength(16)]
    public string Operator { get; set; } = "==";

    /// <summary>
    /// Serialized value for the condition. Format depends on <see cref="Type"/>.
    /// Same semantics as <see cref="SpawnCondition.Value"/>.
    /// </summary>
    [Required]
    [MaxLength(256)]
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Navigation property back to the owning boss config.
    /// </summary>
    public virtual BossConfig BossConfig { get; set; } = null!;
}
