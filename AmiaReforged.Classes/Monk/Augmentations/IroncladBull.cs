using AmiaReforged.Classes.Monk.Techniques.Body;
using AmiaReforged.Classes.Monk.Techniques.Martial;
using AmiaReforged.Classes.Monk.Techniques.Spirit;
using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Monk.Augmentations;

[ServiceBinding(typeof(IAugmentation))]
public sealed class IroncladBull : IAugmentation
{
    private const string IroncladWholenessTag = "ironcladbull_wholenessofbody";

    public PathType PathType => PathType.IroncladBull;
    public void ApplyAttackAugmentation(NwCreature monk, TechniqueType technique, OnCreatureAttack attackData)
    {
        switch (technique)
        {
            case TechniqueType.StunningStrike:
                AugmentStunningStrike(monk, attackData);
                break;
            case TechniqueType.EagleStrike:
                EagleStrike.DoEagleStrike(monk, attackData);
                break;
            case TechniqueType.AxiomaticStrike:
                AxiomaticStrike.DoAxiomaticStrike(attackData);
                break;
        }
    }
    public void ApplyCastAugmentation(NwCreature monk, TechniqueType technique, OnSpellCast castData)
    {
        switch (technique)
        {
            case TechniqueType.WholenessOfBody:
                AugmentWholenessOfBody(monk);
                break;
            case TechniqueType.KiBarrier:
                AugmentKiBarrier(monk);
                break;
            case TechniqueType.QuiveringPalm:
                AugmentQuiveringPalm(monk, castData);
                break;
            case TechniqueType.EmptyBody:
                EmptyBody.DoEmptyBody(monk);
                break;
            case TechniqueType.KiShout:
                KiShout.DoKiShout(monk);
                break;
        }
    }

    /// <summary>
    /// When Stunning Strike lands, gain immunity to damage vulnerability for 1 round. Ki Focus I grants immunity
    /// to armor decrease, Ki Focus II grants immunity to flanking, and Ki Focus III grants immunity to knockdown.
    /// </summary>
    private static void AugmentStunningStrike(NwCreature monk, OnCreatureAttack attackData)
    {
        StunningStrike.DoStunningStrike(attackData);

        if (attackData.Target is not NwCreature target) return;

        if (!monk.IsReactionTypeHostile(target)) return;

        Effect immunities = MonkUtils.GetKiFocus(monk) switch
        {
            KiFocus.KiFocus1 => Effect.LinkEffects(Effect.Immunity(ImmunityType.DamageImmunityDecrease),
                Effect.Immunity(ImmunityType.AcDecrease)),
            KiFocus.KiFocus2 => Effect.LinkEffects(Effect.Immunity(ImmunityType.DamageImmunityDecrease),
                Effect.Immunity(ImmunityType.AcDecrease), Effect.BonusFeat(Feat.PrestigeDefensiveAwareness2!)),
            KiFocus.KiFocus3 => Effect.LinkEffects(Effect.Immunity(ImmunityType.DamageImmunityDecrease),
                Effect.Immunity(ImmunityType.AcDecrease), Effect.BonusFeat(Feat.PrestigeDefensiveAwareness2!),
                Effect.Immunity(ImmunityType.Knockdown)),
            _ => Effect.Immunity(ImmunityType.DamageImmunityDecrease)
        };
        immunities.SubType = EffectSubType.Extraordinary;

        monk.ApplyEffect(EffectDuration.Temporary, immunities, NwTimeSpan.FromRounds(1));
    }

    /// <summary>
    /// Ki Barrier grants 6/- physical damage resistance, with each Ki Focus increasing it by 3,
    /// to a maximum of 15/- physical damage resistance.
    /// </summary>
    private static void AugmentKiBarrier(NwCreature monk)
    {
        KiBarrier.DoKiBarrier(monk);

        byte monkLevel = monk.GetClassInfo(ClassType.Monk)?.Level ?? 0;

        byte resistanceAmount = MonkUtils.GetKiFocus(monk) switch
        {
            KiFocus.KiFocus1 => 9,
            KiFocus.KiFocus2 => 12,
            KiFocus.KiFocus3 => 15,
            _ => 6
        };

        Effect kiBarrierEffect = Effect.LinkEffects(
            Effect.DamageResistance(DamageType.Bludgeoning, resistanceAmount),
            Effect.DamageResistance(DamageType.Slashing, resistanceAmount),
            Effect.DamageResistance(DamageType.Piercing, resistanceAmount), Effect.VisualEffect(VfxType.DurCessatePositive)
        );

        monk.ApplyEffect(EffectDuration.Temporary, kiBarrierEffect, NwTimeSpan.FromTurns(monkLevel));
    }

    /// <summary>
    /// Wholeness of Body heals for 20 extra hit points and grants overheal as temporary hit points.
    /// Each Ki Focus increases the amount of extra hit points healed by 20, to a maximum of 80 extra hit points.
    /// </summary>
    private static void AugmentWholenessOfBody(NwCreature monk)
    {
        int healAmount = CalculateHealAmount(monk);
        int overhealAmount = Math.Max(0, healAmount - (monk.MaxHP - monk.HP));

        Effect wholenessEffect = Effect.Heal(healAmount);
        Effect wholenessVfx = Effect.VisualEffect(VfxType.ImpHealingL, false, 0.7f);

        monk.ApplyEffect(EffectDuration.Instant, wholenessEffect);
        monk.ApplyEffect(EffectDuration.Instant, wholenessVfx);

        ApplyOverheal(monk, overhealAmount);
    }

    private static void ApplyOverheal(NwCreature monk, int overhealAmount)
    {
        if (overhealAmount <= 0) return;

        Effect? overHealEffect = monk.ActiveEffects.FirstOrDefault(effect => effect.Tag == IroncladWholenessTag);
        if (overHealEffect != null) monk.RemoveEffect(overHealEffect);

        overHealEffect = Effect.LinkEffects(
            Effect.TemporaryHitpoints(overhealAmount),
            Effect.VisualEffect(VfxType.DurProtGreaterStoneskin)
        );

        overHealEffect.SubType = EffectSubType.Extraordinary;
        overHealEffect.Tag = IroncladWholenessTag;

        monk.ApplyEffect(EffectDuration.Permanent, overHealEffect);
    }

    private static int CalculateHealAmount(NwCreature monk)
    {
        byte monkLevel = monk.GetClassInfo(ClassType.Monk)?.Level ?? 0;

        byte extraHeal = MonkUtils.GetKiFocus(monk) switch
        {
            KiFocus.KiFocus1 => 40,
            KiFocus.KiFocus2 => 60,
            KiFocus.KiFocus3 => 80,
            _ => 20
        };

        return monkLevel * 2 + extraHeal;
    }

    /// <summary>
    /// Quivering Palm binds the target with Stonehold for one round if they fail a reflex saving throw.
    /// Each Ki Focus increases the duration by one round, to a maximum of four rounds.
    /// </summary>
    private static void AugmentQuiveringPalm(NwCreature monk, OnSpellCast castData)
    {
        TouchAttackResult touchAttackResult = QuiveringPalm.DoQuiveringPalm(monk, castData);

        if (castData.TargetObject is not NwCreature targetCreature) return;
        if (touchAttackResult is TouchAttackResult.Miss) return;
        if (targetCreature.IsImmuneTo(ImmunityType.Paralysis)) return;

        int dc = MonkUtils.CalculateMonkDc(monk);

        SavingThrowResult savingThrowResult =
            targetCreature.RollSavingThrow(SavingThrow.Reflex, dc, SavingThrowType.Paralysis, monk);

        if (savingThrowResult is SavingThrowResult.Success)
        {
            targetCreature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpReflexSaveThrowUse));
            return;
        }

        Effect quiveringEffect = Effect.LinkEffects(
            Effect.Paralyze(),
            Effect.VisualEffect(VfxType.DurStonehold)
        );

        // Base game paralysis is stopped by mind immunity, so we do our own freedom check
        quiveringEffect.IgnoreImmunity = true;
        quiveringEffect.SubType = EffectSubType.Extraordinary;

        TimeSpan quiveringDuration = MonkUtils.GetKiFocus(monk) switch
        {
            KiFocus.KiFocus1 => NwTimeSpan.FromRounds(2),
            KiFocus.KiFocus2 => NwTimeSpan.FromRounds(3),
            KiFocus.KiFocus3 => NwTimeSpan.FromRounds(4),
            _ => NwTimeSpan.FromRounds(1)
        };

        targetCreature.ApplyEffect(EffectDuration.Temporary, quiveringEffect, quiveringDuration);
    }
}
