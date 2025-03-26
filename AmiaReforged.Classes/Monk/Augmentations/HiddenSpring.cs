using AmiaReforged.Classes.Monk.Techniques.Body;
using AmiaReforged.Classes.Monk.Techniques.Martial;
using AmiaReforged.Classes.Monk.Techniques.Spirit;
using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.Classes.Monk.Augmentations;

public static class HiddenSpring
{
    public static void ApplyAugmentations(TechniqueType technique, OnSpellCast? castData = null, OnCreatureAttack? attackData = null)
    {
        switch (technique)
        {
            case TechniqueType.Stunning:
                AugmentStunningStrike(attackData);
                break;
            case TechniqueType.Eagle:
                AugmentEagleStrike(attackData);
                break;
            case TechniqueType.Axiomatic:
                AugmentAxiomaticStrike(attackData);
                break;
            case TechniqueType.EmptyBody:
                AugmentEmptyBody(castData);
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
            case TechniqueType.KiShout:
                KiShout.DoKiShout(castData);
                break;
        }
    }
    
    /// <summary>
    /// Stunning Strike does weaker effects if the target is immune to stun. Ki Focus I pacifies (making the
    /// target unable to attack), Ki Focus II dazes, and Ki Focus III paralyzes the target.
    /// </summary>
    private static void AugmentStunningStrike(OnCreatureAttack attackData)
    {
        SavingThrowResult stunningStrikeResult = StunningStrike.DoStunningStrike(attackData);

        if (attackData.Target is not NwCreature targetCreature) return;

        if (stunningStrikeResult != SavingThrowResult.Immune) return;

        NwCreature monk = attackData.Attacker;
        
        Effect? stunningEffect = MonkUtils.GetKiFocus(monk) switch
        {
            KiFocus.KiFocus1  => Effect.Pacified(),
            KiFocus.KiFocus2 => Effect.Dazed(),
            KiFocus.KiFocus3 => Effect.Paralyze(),
            _ => null
        };
        if (stunningEffect is null) return;
        
        Effect stunningVfx = Effect.VisualEffect(VfxType.FnfHowlOdd, false, 0.06f);
        TimeSpan stunningDuration = NwTimeSpan.FromRounds(1);
        
        stunningEffect.IgnoreImmunity = true;
        
        targetCreature.ApplyEffect(EffectDuration.Temporary, stunningEffect, stunningDuration);
        targetCreature.ApplyEffect(EffectDuration.Instant, stunningVfx);
    }
    
    /// <summary>
    /// Eagle Strike with Ki Focus I incurs a -1 penalty to attack rolls, increased to -2 with Ki Focus II and -3 with Ki Focus III.
    /// </summary>
    private static void AugmentEagleStrike(OnCreatureAttack attackData)
    {
        SavingThrowResult stunningStrikeResult = EagleStrike.DoEagleStrike(attackData);

        if (attackData.Target is not NwCreature targetCreature) return;

        if (stunningStrikeResult != SavingThrowResult.Failure) return;

        NwCreature monk = attackData.Attacker;

        int abDecrease = MonkUtils.GetKiFocus(monk) switch
        {
            KiFocus.KiFocus1 => 1,
            KiFocus.KiFocus2 => 2,
            KiFocus.KiFocus3 => 3,
            _ => 0
        };
        
        Effect eagleEffect = Effect.AttackDecrease(abDecrease);
        TimeSpan eagleDuration = NwTimeSpan.FromRounds(2);
        eagleEffect.Tag = "eaglestrike_hiddenspring";
        eagleEffect.IgnoreImmunity = true;
        
        foreach (Effect effect in targetCreature.ActiveEffects)
        {
            if (effect.Tag == "eaglestrike_hiddenspring")
                targetCreature.RemoveEffect(effect);
        }
        
        targetCreature.ApplyEffect(EffectDuration.Temporary, eagleEffect, eagleDuration);
    }
    
    /// <summary>
    /// Axiomatic Strike deals +1 bonus positive damage, increased by an additional +1 for every Ki Focus to a maximum
    /// of +4 bonus positive damage.
    /// </summary>
    private static void AugmentAxiomaticStrike(OnCreatureAttack attackData)
    {
        AxiomaticStrike.DoAxiomaticStrike(attackData);

        NwCreature monk = attackData.Attacker;
        DamageData<short> damageData = attackData.DamageData;
        short positiveDamage = damageData.GetDamageByType(DamageType.Positive);
            
        int bonusDamage = MonkUtils.GetKiFocus(monk) switch
        {
            KiFocus.KiFocus1 => 2,
            KiFocus.KiFocus2 => 3,
            KiFocus.KiFocus3 => 4,
            _ => 1
        };

        positiveDamage += (short)bonusDamage;
        damageData.SetDamageByType(DamageType.Positive, positiveDamage);
    }
    
    /// <summary>
    /// Empty Body gives +2 to fortitude and reflex saving throws. Each Ki Focus gives an additional +2 to
    /// a maximum of +8 to fortitude and reflex saving throws.
    /// </summary>
    private static void AugmentEmptyBody(OnSpellCast castData)
    {
        EmptyBody.DoEmptyBody(castData);
        
        NwCreature monk = (NwCreature)castData.Caster;
        int monkLevel = monk.GetClassInfo(ClassType.Monk)!.Level;
        int bonusAmount = MonkUtils.GetKiFocus(monk) switch
        {
            KiFocus.KiFocus1 => 4,
            KiFocus.KiFocus2 => 6,
            KiFocus.KiFocus3 => 8,
            _ => 2
        };
        Effect emptyBodyEffect = Effect.LinkEffects(Effect.SavingThrowIncrease(SavingThrow.Fortitude, bonusAmount), 
            Effect.SavingThrowIncrease(SavingThrow.Reflex, bonusAmount));
        TimeSpan effectDuration = NwTimeSpan.FromRounds(monkLevel);
        
        monk.ApplyEffect(EffectDuration.Temporary, emptyBodyEffect, effectDuration);
    }
}