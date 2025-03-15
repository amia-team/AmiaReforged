using AmiaReforged.Classes.Monk.Techniques.Body;
using AmiaReforged.Classes.Monk.Techniques.Martial;
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
            case TechniqueType.KiShout:
                AugmentKiShout(castData);
                break;
            case TechniqueType.EmptyBody:
                AugmentEmptyBody(castData);
                break;
            case TechniqueType.Quivering:
                AugmentQuiveringPalm(castData);
                break;
            case TechniqueType.Wholeness:
                WholenessOfBody.DoWholenessOfBody(castData);
                break;
            case TechniqueType.KiBarrier:
                KiBarrier.DoKiBarrier(castData);
                break;
        }
    }
    
    /// <summary>
    /// Wisdom modifier applies to attacks rolls instead of strength or dexterity. Ki Focus II allows the use of
    /// Martial Techniques with ranged weapons.
    /// </summary>
    private static void AugmentStunningStrike(OnCreatureAttack attackData)
    {
        SavingThrowResult stunningStrikeResult = StunningStrike.DoStunningStrike(attackData);

        if (attackData.Target is not NwCreature targetCreature) return;

        if (stunningStrikeResult != SavingThrowResult.Immune) return;

        NwCreature monk = attackData.Attacker;
        
        Effect stunningEffect = MonkUtilFunctions.GetKiFocus(monk) switch
        {
            KiFocus.KiFocus1 or KiFocus.KiFocus2 => Effect.Pacified(),
            KiFocus.KiFocus3 => Effect.Paralyze(),
            _ => Effect.VisualEffect(VfxType.None)
        };
        Effect stunningVfx = Effect.VisualEffect(VfxType.FnfHowlOdd, false, 0.06f);
        TimeSpan stunningDuration = NwTimeSpan.FromRounds(1);
        
        stunningEffect.IgnoreImmunity = true;
        
        targetCreature.ApplyEffect(EffectDuration.Temporary, stunningEffect, stunningDuration);
        targetCreature.ApplyEffect(EffectDuration.Instant, stunningVfx);
    }
    
    /// <summary>
    /// Eagle Strike with Ki Focus I incurs a -1 penalty to attack rolls, and Ki Focus III increases the penalty to -2.
    /// </summary>
    private static void AugmentEagleStrike(OnCreatureAttack attackData)
    {
        SavingThrowResult stunningStrikeResult = EagleStrike.DoEagleStrike(attackData);

        if (attackData.Target is not NwCreature targetCreature) return;

        if (stunningStrikeResult != SavingThrowResult.Failure) return;

        NwCreature monk = attackData.Attacker;

        int abDecrease = MonkUtilFunctions.GetKiFocus(monk) switch
        {
            KiFocus.KiFocus1 or KiFocus.KiFocus2 => 1,
            KiFocus.KiFocus3 => 2,
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
    /// Axiomatic Strike with Ki Focus I adds one fourth of the base wisdom modifier as bonus physical damage,
    /// and Ki Focus III increases this to half the base wisdom modifier.
    /// </summary>
    private static void AugmentAxiomaticStrike(OnCreatureAttack attackData)
    {
        short bludgeoningDamage = AxiomaticStrike.DoAxiomaticStrike(attackData);

        NwCreature monk = attackData.Attacker;
        DamageData<short> damageData = attackData.DamageData;
        int baseWisdomModifier = (monk.GetRawAbilityScore(Ability.Wisdom) - 10) / 2;
        int bonusDamage = MonkUtilFunctions.GetKiFocus(monk) switch
        {
            KiFocus.KiFocus1 or KiFocus.KiFocus2 => baseWisdomModifier / 4,
            KiFocus.KiFocus3 => baseWisdomModifier / 2,
            _ => 0
        };

        bludgeoningDamage += (short)bonusDamage;
        damageData.SetDamageByType(DamageType.Bludgeoning, bludgeoningDamage);
    }

    private static void AugmentKiShout(OnSpellCast castData)
    {
        
    }

    private static void AugmentEmptyBody(OnSpellCast castData)
    {
    }

    private static void AugmentQuiveringPalm(OnSpellCast castData)
    {
    }
}