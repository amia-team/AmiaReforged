using AmiaReforged.Classes.Monk.Techniques.Body;
using AmiaReforged.Classes.Monk.Techniques.Martial;
using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.Classes.Monk.Augmentations;

public static class CrystalTides
{
    public static void ApplyAugmentations(TechniqueType technique, OnSpellCast? castData = null, OnCreatureAttack? attackData = null)
    {
        switch (technique)
        {
            case TechniqueType.Stunning:
                AugmentStunningStrike(attackData);
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
            case TechniqueType.Eagle:
                EagleStrike.DoEagleStrike(attackData);
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

        Effect? stunningEffect = null, visualEffect = null;
        TimeSpan effectDuration = NwTimeSpan.FromRounds(1);
        
        switch (MonkUtilFunctions.GetKiFocus(monk))
        {
            case KiFocus.KiFocus1 or KiFocus.KiFocus2:
                stunningEffect = Effect.Pacified();
                visualEffect = Effect.VisualEffect(VfxType.FnfHowlOdd, false, 0.06f);
                break;
            case KiFocus.KiFocus3:
                stunningEffect = Effect.Paralyze();
                visualEffect = Effect.VisualEffect(VfxType.DurParalyzeHold);
                break;
        }

        if (stunningEffect is null || visualEffect is null) return;
        
        stunningEffect.IgnoreImmunity = true;
        
        targetCreature.ApplyEffect(EffectDuration.Temporary, stunningEffect, effectDuration);
        targetCreature.ApplyEffect(EffectDuration.Instant, visualEffect);
    }

    private static void AugmentAxiomaticStrike(OnCreatureAttack attackData)
    {
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