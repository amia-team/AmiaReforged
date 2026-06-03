using Anvil.API;

namespace AmiaReforged.Classes.EffectUtils.Polymorph;

public static class PolymorphUtils
{
    private const string GreaterWildshapeBonusTag = "greater_wildshape_bonus";

    public static bool PreventDoublePolymorph(NwCreature caster)
    {
        if (caster.ActiveEffects.All(e => e.EffectType != EffectType.Polymorph)) return false;
        caster.ControllingPlayer?.SendServerMessage("Cannot polymorph while polymorphed. Unshift first.");
        return true;
    }

    /// <summary>
    /// Creates a bonus effects for Greater Wildshape polymorph based on the shifter's level and selected polymorph type.
    /// </summary>
    /// <param name="shifterLevel">Shifter class level.</param>
    /// <param name="polymorphType">The polymorph table entry defining the type of polymorph and associated bonuses.</param>
    /// <param name="masterSpell">The master spell associated with the polymorph.</param>
    /// <param name="polymorphName">The name of the polymorph form being applied.</param>
    /// <param name="message">An output parameter that returns a descriptive message about the applied bonuses, if any.</param>
    public static Effect? GreaterWildshapeBonusEffect(int shifterLevel, PolymorphTableEntry polymorphType,
        Spell masterSpell, string polymorphName, out string? message)
    {
        message = null;

        // Bonuses don't apply to Wild Shape or Elemental Shape forms
        if (masterSpell is Spell.AbilityWildShape or Spell.AbilityElementalShape) return null;

        Effect effect = ShifterAcEffect(shifterLevel, out string? shifterAcMessage);

        if (polymorphType.NaturalAcBonus is { } acBonus)
        {
            effect = Effect.LinkEffects(effect, Effect.AttackIncrease(acBonus));
            message = shifterAcMessage + $" Applied +{acBonus} attack bonus with {polymorphName} form.";
        }
        else
        {
            message = shifterAcMessage;
        }

        effect.SubType = EffectSubType.Extraordinary;
        effect.Tag = GreaterWildshapeBonusTag;
        return effect;
    }

    public static bool RemoveGreaterWildshapeBonus(NwCreature creature)
    {
        bool effectRemoved = false;

        foreach (Effect effect in creature.ActiveEffects)
        {
            if (effect.Tag != GreaterWildshapeBonusTag) continue;

            creature.RemoveEffect(effect);
            if (!effectRemoved)
                effectRemoved = true;
        }

        return effectRemoved;
    }

    /// <summary>
    /// Helper function to create an AC effect based on the shifter class level.
    /// </summary>
    private static Effect ShifterAcEffect(int casterLevel, out string? message)
    {
        int naturalAcBonus = casterLevel switch
        {
            >= 1 and < 5 => 1,
            >= 5 and < 9 => 2,
            >= 9 and < 13 => 3,
            >= 13 and < 17 => 4,
            >= 17 => 5,
            _ => 0
        };

        int deflectionAcBonus = casterLevel switch
        {
            >= 2 and < 6 => 1,
            >= 6 and < 10 => 2,
            >= 10 and < 14 => 3,
            >= 14 and < 18 => 4,
            >= 18 => 5,
            _ => 0
        };

        int dodgeAcBonus = casterLevel switch
        {
            >= 3 and < 7 => 1,
            >= 7 and < 11 => 2,
            >= 11 and < 15 => 3,
            >= 15 and < 19 => 4,
            >= 19 => 5,
            _ => 0
        };

        int armorAcBonus = casterLevel switch
        {
            >= 4 and < 8 => 1,
            >= 8 and < 12 => 2,
            >= 12 and < 16 => 3,
            >= 16 and < 20 => 4,
            >= 20 => 5,
            _ => 0
        };

        int shieldAcBonus = casterLevel switch
        {
            >= 1 and < 3 => 3,
            >= 3 and < 6 => 4,
            >= 6 and < 9 => 5,
            >= 9 and < 12 => 6,
            >= 12 and < 15 => 7,
            >= 15 => 8,
            _ => 0
        };

        message = $"[Polymorph] Applied +{naturalAcBonus} Natural, +{deflectionAcBonus} Deflection, " +
                  $"+{dodgeAcBonus} Dodge, +{armorAcBonus} Armor, and +{shieldAcBonus} Shield AC bonus.";

        return Effect.LinkEffects
        (
            Effect.ACIncrease(naturalAcBonus, ACBonus.Natural),
            Effect.ACIncrease(deflectionAcBonus, ACBonus.Deflection),
            Effect.ACIncrease(dodgeAcBonus, ACBonus.Dodge),
            Effect.ACIncrease(armorAcBonus, ACBonus.ArmourEnchantment),
            Effect.ACIncrease(shieldAcBonus, ACBonus.ShieldEnchantment)
        );
    }
}
