using System.ComponentModel.DataAnnotations;

namespace AmiaReforged.PwEngine.Features.Encounters.Models;

/// <summary>
/// A single effect within a <see cref="MutationTemplate"/>.
/// When the mutation fires, all active effects are applied to the creature.
/// </summary>
public class MutationEffect
{
    [Key]
    public Guid Id { get; set; }

    /// <summary>
    /// FK to the owning <see cref="MutationTemplate"/>.
    /// </summary>
    public Guid MutationTemplateId { get; set; }

    /// <summary>
    /// The type of effect to apply.
    /// </summary>
    public MutationEffectType Type { get; set; }

    /// <summary>
    /// Base value for the effect (e.g., bonus amount, temp HP amount).
    /// </summary>
    public int Value { get; set; }

    /// <summary>
    /// Which ability to buff. Only used when <see cref="Type"/> is <see cref="MutationEffectType.AbilityBonus"/>.
    /// </summary>
    public NwnAbilityType? AbilityType { get; set; }

    /// <summary>
    /// Which damage type for bonus damage or damage shield effects.
    /// Only used when <see cref="Type"/> is <see cref="MutationEffectType.DamageBonus"/> or <see cref="MutationEffectType.DamageShield"/>.
    /// </summary>
    public NwnDamageType? DamageType { get; set; }

    /// <summary>
    /// Duration of the effect in seconds. 0 = permanent (creature lifetime).
    /// </summary>
    public int DurationSeconds { get; set; }

    /// <summary>
    /// Whether this effect is active. Inactive effects are skipped.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Navigation property to the owning template.
    /// </summary>
    public virtual MutationTemplate MutationTemplate { get; set; } = null!;
}
