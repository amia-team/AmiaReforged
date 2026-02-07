using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.Classes.Spells;

/// <summary>
/// Service for death spell enhancements based on Necromancy spell focus feats.
/// Death spells deal percentage-based negative energy damage to targets even if they pass their save,
/// making them useful against high fortitude enemies.
/// </summary>
[ServiceBinding(typeof(DeathSpellService))]
public class DeathSpellService
{
    /// <summary>
    /// Default percentage of max health dealt as damage per spell focus tier.
    /// </summary>
    public const int DefaultPercentPerFocus = 5;

    /// <summary>
    /// Finger of Death percentage of max health dealt as damage per spell focus tier.
    /// </summary>
    public const int FingerOfDeathPercentPerFocus = 8;

    /// <summary>
    /// Gets the number of necromancy spell focus tiers the caster has.
    /// </summary>
    /// <param name="caster">The creature casting the spell</param>
    /// <returns>0-3 based on spell focus feats (none, focus, greater, epic)</returns>
    public int GetNecromancyFocusTier(NwCreature caster)
    {
        if (caster.KnowsFeat(Feat.EpicSpellFocusNecromancy!))
            return 3;
        if (caster.KnowsFeat(Feat.GreaterSpellFocusNecromancy!))
            return 2;
        if (caster.KnowsFeat(Feat.SpellFocusNecromancy!))
            return 1;
        return 0;
    }

    /// <summary>
    /// Calculates the percentage of max health to deal as negative energy damage.
    /// </summary>
    /// <param name="caster">The creature casting the spell</param>
    /// <param name="percentPerFocus">Percentage per spell focus tier (default 5%)</param>
    /// <returns>Percentage of max health (e.g., 15 for 15%)</returns>
    public int GetDeathSpellDamagePercent(NwCreature caster, int percentPerFocus = DefaultPercentPerFocus)
    {
        int focusTier = GetNecromancyFocusTier(caster);
        return focusTier * percentPerFocus;
    }

    /// <summary>
    /// Calculates the actual damage amount based on target's max health and caster's spell focuses.
    /// </summary>
    /// <param name="caster">The creature casting the spell</param>
    /// <param name="target">The target creature</param>
    /// <param name="percentPerFocus">Percentage per spell focus tier (default 5%)</param>
    /// <returns>The amount of negative energy damage to deal</returns>
    public int CalculateDeathSpellDamage(NwCreature caster, NwCreature target, int percentPerFocus = DefaultPercentPerFocus)
    {
        int percentDamage = GetDeathSpellDamagePercent(caster, percentPerFocus);
        if (percentDamage <= 0)
            return 0;

        int maxHealth = target.MaxHP;
        int damage = (maxHealth * percentDamage) / 100;

        // Minimum 1 damage if they have any spell focus
        return Math.Max(damage, 1);
    }

    /// <summary>
    /// Calculates healing amount for undead targets (same as damage calculation).
    /// </summary>
    /// <param name="caster">The creature casting the spell</param>
    /// <param name="target">The undead target creature</param>
    /// <param name="percentPerFocus">Percentage per spell focus tier (default 5%)</param>
    /// <returns>The amount of healing to apply</returns>
    public int CalculateDeathSpellHealing(NwCreature caster, NwCreature target, int percentPerFocus = DefaultPercentPerFocus)
    {
        return CalculateDeathSpellDamage(caster, target, percentPerFocus);
    }

    /// <summary>
    /// Applies percentage-based negative energy damage from a death spell.
    /// Undead are healed instead, and death-immune creatures are unaffected.
    /// This damage is applied regardless of whether the target passed their save.
    /// </summary>
    /// <param name="caster">The creature casting the spell</param>
    /// <param name="target">The target creature</param>
    /// <param name="applyVfx">Whether to apply a visual effect (default true)</param>
    /// <param name="percentPerFocus">Percentage per spell focus tier (default 5%)</param>
    public void ApplyDeathSpellDamage(NwCreature caster, NwCreature target, bool applyVfx = true, int percentPerFocus = DefaultPercentPerFocus)
    {
        // Death immunity negates the effect completely
        if (IsDeathImmune(target))
            return;

        // Undead are healed by negative energy
        if (target.Race.RacialType == RacialType.Undead)
        {
            ApplyDeathSpellHealing(caster, target, applyVfx, percentPerFocus);
            return;
        }

        int damage = CalculateDeathSpellDamage(caster, target, percentPerFocus);
        if (damage <= 0)
            return;

        Effect damageEffect = Effect.Damage(damage, DamageType.Negative);
        target.ApplyEffect(EffectDuration.Instant, damageEffect);

        if (applyVfx)
        {
            Effect vfx = Effect.VisualEffect(VfxType.ImpNegativeEnergy);
            target.ApplyEffect(EffectDuration.Instant, vfx);
        }
    }

    /// <summary>
    /// Applies healing to undead targets from negative energy death spells.
    /// </summary>
    /// <param name="caster">The creature casting the spell</param>
    /// <param name="target">The undead target creature</param>
    /// <param name="applyVfx">Whether to apply a visual effect (default true)</param>
    /// <param name="percentPerFocus">Percentage per spell focus tier (default 5%)</param>
    public void ApplyDeathSpellHealing(NwCreature caster, NwCreature target, bool applyVfx = true, int percentPerFocus = DefaultPercentPerFocus)
    {
        int healing = CalculateDeathSpellHealing(caster, target, percentPerFocus);
        if (healing <= 0)
            return;

        Effect healEffect = Effect.Heal(healing);
        target.ApplyEffect(EffectDuration.Instant, healEffect);

        if (applyVfx)
        {
            Effect vfx = Effect.VisualEffect(VfxType.ImpHealingM);
            target.ApplyEffect(EffectDuration.Instant, vfx);
        }
    }

    /// <summary>
    /// Checks if a creature is immune to death magic.
    /// </summary>
    /// <param name="creature">The creature to check</param>
    /// <returns>True if immune to death magic</returns>
    public bool IsDeathImmune(NwCreature creature)
    {
        // Check for death immunity effect
        return creature.ActiveEffects.Any(e => e.EffectType == EffectType.Immunity
            && e.IntParams.Contains((int)ImmunityType.Death));
    }
}
