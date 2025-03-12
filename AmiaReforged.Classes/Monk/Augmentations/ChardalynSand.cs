using AmiaReforged.Classes.Monk.Constants;
using AmiaReforged.Classes.Monk.Techniques.Body;
using AmiaReforged.Classes.Monk.Techniques.Martial;
using AmiaReforged.Classes.Monk.Techniques.Spirit;
using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.Classes.Monk.Augmentations;

public static class ChardalynSand
{
    public static void ApplyAugmentations(TechniqueType technique, OnSpellCast? castData = null, OnCreatureAttack? attackData = null)
    {
        switch (technique)
        {
            
            case TechniqueType.Axiomatic:
                AugmentAxiomaticStrike(attackData);
                break;
            case TechniqueType.Eagle:
                AugmentEagleStrike(attackData);
                break;
            case TechniqueType.KiShout:
                AugmentKiShout(castData);
                break;
            case TechniqueType.EmptyBody:
                AugmentEmptyBody(castData);
                break;
            case TechniqueType.Stunning:
                StunningStrike.DoStunningStrike(attackData);
                break;
            case TechniqueType.Wholeness:
                WholenessOfBody.DoWholenessOfBody(castData);
                break;
            case TechniqueType.KiBarrier:
                KiBarrier.DoKiBarrier(castData);
                break;
            case TechniqueType.Quivering:
                QuiveringPalm.DoQuiveringPalm(castData);
                break;
        }
    }
    
    /// <summary>
    /// Eagle Strike has a 1% chance to impart a wild magic effect.
    /// Each Ki Focus increases the chance by 1% to a maximum of 5%.
    /// </summary>
    private static void AugmentEagleStrike(OnCreatureAttack attackData)
    {
        EagleStrike.DoEagleStrike(attackData);
        
        if (attackData.Target is not NwCreature targetCreature) return;
        
        NwCreature monk = attackData.Attacker;
        
        if (!targetCreature.IsReactionTypeHostile(monk)) return;
        
        int dc = MonkUtilFunctions.CalculateMonkDc(monk);
        int monkLevel = monk.GetClassInfo(ClassType.Monk)!.Level;
        int wildMagicPct = monkLevel switch
        {
            >= MonkLevel.KiFocusI and < MonkLevel.KiFocusIi => 2,
            >= MonkLevel.KiFocusIi and < MonkLevel.KiFocusIii => 3,
            MonkLevel.KiFocusIii => 4,
            _ => 1
        };

        int d100Roll = Random.Shared.Roll(100);
        
        if (d100Roll <= wildMagicPct)
            WildMagicEffects.DoWildMagic(monk, targetCreature, monkLevel, dc);
    }
    
    /// <summary>
    /// Axiomatic Strike deals +1 bonus magical damage. Each Ki Focus increases the damage by 1,
    /// to a maximum of +4 bonus magical damage.
    /// </summary>
    private static void AugmentAxiomaticStrike(OnCreatureAttack attackData)
    {
        AxiomaticStrike.DoAxiomaticStrike(attackData);

        NwCreature monk = attackData.Attacker;
        int monkLevel = monk.GetClassInfo(ClassType.Monk)!.Level;
        DamageType elementalType = MonkUtilFunctions.GetElementalType(monk);
        DamageData<short> damageData = attackData.DamageData;
        short magicalDamage = damageData.GetDamageByType(DamageType.Magical);
        short bonusDamage = monkLevel switch
        {
            >= MonkLevel.KiFocusI and < MonkLevel.KiFocusIi => 2,
            >= MonkLevel.KiFocusIi and < MonkLevel.KiFocusIii => 3,
            MonkLevel.KiFocusIii => 4,
            _ => 1
        };

        magicalDamage += bonusDamage;
        damageData.SetDamageByType(elementalType, magicalDamage);
    }

    /// <summary>
    /// Empty Body grants a spell mantle that absorbs up to 2 spells and spell-like abilities.
    /// Each Ki Focus increases the effects it can absorb by 2, to a maximum of 8 spells or spell-like abilities.
    /// </summary>
    private static void AugmentEmptyBody(OnSpellCast castData)
    {
        NwCreature monk = (NwCreature)castData.Caster;
        int monkLevel = monk.GetClassInfo(ClassType.Monk)!.Level;
        
        int spellsAbsorbed = monkLevel switch
        {
            >= MonkLevel.KiFocusI and < MonkLevel.KiFocusIi => 2,
            >= MonkLevel.KiFocusIi and < MonkLevel.KiFocusIii => 3,
            MonkLevel.KiFocusIii => 4,
            _ => 1
        };
        Effect spellAbsorb = Effect.SpellLevelAbsorption(spellsAbsorbed);
        Effect spellAbsorbVfx = Effect.VisualEffect(VfxType.DurSpellturning);
        Effect emptyBodyEffect = Effect.LinkEffects(spellAbsorb, spellAbsorbVfx);
        TimeSpan effectDuration = NwTimeSpan.FromRounds(monkLevel);
        
        monk.ApplyEffect(EffectDuration.Temporary, emptyBodyEffect, effectDuration);
    }

    private static void AugmentKiShout(OnSpellCast castData)
    {
    }
    
    
}