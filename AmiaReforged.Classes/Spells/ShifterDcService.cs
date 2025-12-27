using Anvil.API;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.Classes.Spells;

/// <summary>
/// Service for calculating DCs and caster levels for Shifter forms.
/// Ported from inc_td_shifter.nss
/// </summary>
[ServiceBinding(typeof(ShifterDcService))]
public class ShifterDcService
{
    private const int MaxShifterDc = 35;

    /// <summary>
    /// Gets the appropriate DC for a spell/ability cast by a creature.
    /// If the creature is polymorphed and has Shifter levels, calculates DC based on Shifter progression.
    /// Otherwise returns the fallback DC (typically the normal spell DC).
    /// </summary>
    /// <param name="creature">The creature casting the spell/ability</param>
    /// <param name="fallbackDc">The DC to use if the creature is not a polymorphed Shifter</param>
    /// <returns>The calculated DC</returns>
    public int GetShifterDc(NwCreature creature, int fallbackDc)
    {
        if (!IsPolymorphedShifter(creature))
        {
            return fallbackDc;
        }

        int shifterLevel = GetShifterLevel(creature);
        int wisdomModifier = creature.GetAbilityModifier(Ability.Wisdom);

        // Formula: 10 + (Shifter Level / 2) + Wisdom Modifier, capped at 35
        int calculatedDc = 10 + (shifterLevel / 2) + wisdomModifier;
        return Math.Min(calculatedDc, MaxShifterDc);
    }

    /// <summary>
    /// Gets the caster level for a polymorphed Shifter.
    /// Returns Druid + Shifter levels if polymorphed with Shifter levels.
    /// Otherwise returns the fallback caster level or the creature's normal caster level.
    /// </summary>
    /// <param name="creature">The creature to check</param>
    /// <param name="fallbackCasterLevel">Optional fallback caster level (0 means use normal caster level)</param>
    /// <returns>The calculated caster level</returns>
    public int GetShifterCasterLevel(NwCreature creature, int fallbackCasterLevel = 0)
    {
        if (IsPolymorphedShifter(creature))
        {
            int druidLevel = GetClassLevel(creature, NWScript.CLASS_TYPE_DRUID);
            int shifterLevel = GetShifterLevel(creature);
            return druidLevel + shifterLevel;
        }

        if (fallbackCasterLevel > 0)
        {
            return fallbackCasterLevel;
        }

        return creature.CasterLevel;
    }

    /// <summary>
    /// Checks if a creature is currently polymorphed and has Shifter class levels.
    /// </summary>
    public bool IsPolymorphedShifter(NwCreature creature)
    {
        // Check if creature has any polymorph effects active
        bool isPolymorphed = creature.ActiveEffects.Any(e => e.EffectType == EffectType.Polymorph);
        int shifterLevel = GetShifterLevel(creature);
        return isPolymorphed && shifterLevel > 0;
    }

    private static int GetShifterLevel(NwCreature creature)
    {
        return GetClassLevel(creature, NWScript.CLASS_TYPE_SHIFTER);
    }

    private static int GetClassLevel(NwCreature creature, int classType)
    {
        return NWScript.GetLevelByClass(classType, creature);
    }
}
