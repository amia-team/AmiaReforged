using AmiaReforged.Classes.Monk.Constants;
using AmiaReforged.Classes.Monk.Techniques.Body;
using AmiaReforged.Classes.Monk.Techniques.Martial;
using AmiaReforged.Classes.Monk.Techniques.Spirit;
using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.Classes.Monk.Augmentations;

public static class IroncladBull
{
    public static void ApplyAugmentations(TechniqueType technique, OnSpellCast? castData = null, OnCreatureAttack? attackData = null)
    {
        switch (technique)
        {
            case TechniqueType.Eagle:
                AugmentEagleStrike(attackData);
                break;
            case TechniqueType.KiBarrier:
                AugmentKiBarrier(castData);
                break;
            case TechniqueType.Wholeness:
                AugmentWholenessOfBody(castData);
                break;
            case TechniqueType.Quivering:
                AugmentQuiveringPalm(castData);
                break;
            case TechniqueType.Stunning:
                StunningStrike.DoStunningStrike(attackData);
                break;
            case TechniqueType.Axiomatic:
                AxiomaticStrike.DoAxiomaticStrike(attackData);
                break;
            case TechniqueType.EmptyBody:
                EmptyBody.DoEmptyBody(castData);
                break;
            case TechniqueType.KiShout:
                KiShout.DoKiShout(castData);
                break;
        }
    }

    /// <summary>
    /// Eagle Strike has a 1% chance to regenerate a Body Ki Point. Each Ki Focus increases the chance by 1%,
    /// to a maximum of 4% chance.
    /// </summary>
    private static void AugmentEagleStrike(OnCreatureAttack attackData)
    {
        EagleStrike.DoEagleStrike(attackData);

        NwCreature monk = attackData.Attacker;

        // Target must be a hostile creature
        if (!monk.IsReactionTypeHostile((NwCreature)attackData.Target)) return;

        int monkLevel = monk.GetClassInfo(ClassType.Monk)!.Level;

        // The effect only affects Body Ki Point recharge, so duh
        if (monkLevel < MonkLevel.BodyKiPointsI) return;

        int kiBodyRegenChance = MonkUtils.GetKiFocus(monk) switch
        {
            KiFocus.KiFocus1 => 2,
            KiFocus.KiFocus2 => 3,
            KiFocus.KiFocus3 => 4,
            _ => 1
        };

        int d100Roll = Random.Shared.Roll(100);

        if (d100Roll <= kiBodyRegenChance)
            monk.IncrementRemainingFeatUses(NwFeat.FromFeatId(MonkFeat.BodyKiPoint)!);
    }

    /// <summary>
    /// Ki Barrier grants 6/- physical damage resistance, with each Ki Focus increasing it by 3,
    /// to a maximum of 15/- physical damage resistance.
    /// </summary>
    private static void AugmentKiBarrier(OnSpellCast castData)
    {
        KiBarrier.DoKiBarrier(castData);

        NwCreature monk = (NwCreature)castData.Caster;
        int monkLevel = monk.GetClassInfo(ClassType.Monk)!.Level;
        int resistanceAmount = MonkUtils.GetKiFocus(monk) switch
        {
            KiFocus.KiFocus1 => 9,
            KiFocus.KiFocus2 => 12,
            KiFocus.KiFocus3 => 15,
            _ => 6
        };
        Effect kiBarrierEffect = Effect.LinkEffects(Effect.DamageResistance(DamageType.Bludgeoning, resistanceAmount),
            Effect.DamageResistance(DamageType.Slashing, resistanceAmount),
            Effect.DamageResistance(DamageType.Piercing, resistanceAmount), Effect.VisualEffect(VfxType.DurCessatePositive));
        TimeSpan effectDuration = NwTimeSpan.FromTurns(monkLevel);

        monk.ApplyEffect(EffectDuration.Temporary, kiBarrierEffect, effectDuration);
    }

    /// <summary>
    /// Wholeness of Body grants 20 temporary hit points until removed. Each Ki Focus increases the amount of temporary
    /// hit points by 20, to a maximum of 80 temporary hit points.
    /// </summary>
    private static void AugmentWholenessOfBody(OnSpellCast castData)
    {
        WholenessOfBody.DoWholenessOfBody(castData);

        NwCreature monk = (NwCreature)castData.Caster;

        int tempHpAmount = MonkUtils.GetKiFocus(monk) switch
        {
            KiFocus.KiFocus1 => 40,
            KiFocus.KiFocus2 => 60,
            KiFocus.KiFocus3 => 80,
            _ => 20
        };

        monk.ApplyEffect(EffectDuration.Permanent, Effect.TemporaryHitpoints(tempHpAmount));
    }

    /// <summary>
    /// Quivering Palm binds the target with Stonehold for one round if they fail a reflex saving throw.
    /// Each Ki Focus increases the duration by one round, to a maximum of four rounds.
    /// </summary>
    private static void AugmentQuiveringPalm(OnSpellCast castData)
    {
        TouchAttackResult touchAttackResult = QuiveringPalm.DoQuiveringPalm(castData);

        if (castData.TargetObject is not NwCreature targetCreature) return;
        if (touchAttackResult is TouchAttackResult.Miss) return;

        NwCreature monk = (NwCreature)castData.Caster;

        int dc = MonkUtils.CalculateMonkDc(monk);
        int roundsAmount = MonkUtils.GetKiFocus(monk) switch
        {
            KiFocus.KiFocus1 => 2,
            KiFocus.KiFocus2 => 3,
            KiFocus.KiFocus3 => 4,
            _ => 1
        };

        Effect quiveringEffect = Effect.LinkEffects(Effect.Paralyze(), Effect.VisualEffect(VfxType.DurStonehold));
        // Base game paralysis is stopped by mind immunity, so we do our own freedom check
        quiveringEffect.IgnoreImmunity = true;

        TimeSpan quiveringDuration = NwTimeSpan.FromRounds(roundsAmount);

        SavingThrowResult savingThrowResult =
            targetCreature.RollSavingThrow(SavingThrow.Reflex, dc, SavingThrowType.Paralysis, monk);

        if (targetCreature.IsImmuneTo(ImmunityType.Paralysis)) return;

        if (savingThrowResult is SavingThrowResult.Success)
        {
            targetCreature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpReflexSaveThrowUse));
            return;
        }

        targetCreature.ApplyEffect(EffectDuration.Temporary, quiveringEffect, quiveringDuration);
    }
}
