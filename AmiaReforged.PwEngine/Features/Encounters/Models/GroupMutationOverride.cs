using System.ComponentModel.DataAnnotations;

namespace AmiaReforged.PwEngine.Features.Encounters.Models;

/// <summary>
/// Links a <see cref="SpawnGroup"/> to a specific <see cref="MutationTemplate"/>
/// with a custom appearance chance. When the parent group has
/// <see cref="SpawnGroup.OverrideMutations"/> enabled, only mutations listed here
/// are considered — and each uses its own <see cref="ChancePercent"/> instead of
/// the template's global <see cref="MutationTemplate.SpawnChancePercent"/>.
///
/// If the group overrides mutations but has no overrides defined, no mutations
/// are ever applied to that group's creatures.
/// </summary>
public class GroupMutationOverride
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// FK to the owning <see cref="SpawnGroup"/>.
    /// </summary>
    public Guid SpawnGroupId { get; set; }

    /// <summary>
    /// FK to the <see cref="MutationTemplate"/> being overridden for this group.
    /// </summary>
    public Guid MutationTemplateId { get; set; }

    /// <summary>
    /// Custom appearance chance (0–100) for this mutation when rolling for the group.
    /// Replaces <see cref="MutationTemplate.SpawnChancePercent"/> for this group.
    /// </summary>
    public int ChancePercent { get; set; } = 10;

    // Navigation properties

    public virtual SpawnGroup SpawnGroup { get; set; } = null!;
    public virtual MutationTemplate MutationTemplate { get; set; } = null!;
}
