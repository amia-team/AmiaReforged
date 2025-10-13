using AmiaReforged.Classes.Monk.Techniques.Body;
using AmiaReforged.Classes.Monk.Techniques.Martial;
using AmiaReforged.Classes.Monk.Techniques.Spirit;
using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Monk.Augmentations;

[ServiceBinding(typeof(IAugmentation))]
public class FloatingLeaf : IAugmentation
{
    private const string FloatingEagleStrikeTag = nameof(PathType.FloatingLeaf) +  nameof(TechniqueType.EagleStrike);
    public PathType PathType => PathType.FloatingLeaf;
    public void ApplyAttackAugmentation(NwCreature monk, TechniqueType technique, OnCreatureDamage attackData)
    {
        switch (technique)
        {
            case TechniqueType.StunningStrike:
                AugmentStunningStrike(monk, attackData);
                break;
            case TechniqueType.EagleStrike:
                AugmentEagleStrike(monk, attackData);
                break;
            case TechniqueType.AxiomaticStrike:
                AugmentAxiomaticStrike(monk, attackData);
                break;
        }
    }

    public void ApplyCastAugmentation(NwCreature monk, TechniqueType technique, OnSpellCast castData)
    {
        switch (technique)
        {
            case TechniqueType.EmptyBody:
                AugmentEmptyBody(monk);
                break;
            case TechniqueType.KiBarrier:
                KiBarrier.DoKiBarrier(monk);
                break;
            case TechniqueType.QuiveringPalm:
                QuiveringPalm.DoQuiveringPalm(monk, castData);
                break;
            case TechniqueType.KiShout:
                KiShout.DoKiShout(monk);
                break;
        }
    }

    /// <summary>
    /// Stunning Strike does weaker effects if the target is immune to stun. Ki Focus I pacifies (making the
    /// target unable to attack), Ki Focus II dazes, and Ki Focus III paralyzes the target.
    /// </summary>
    private static void AugmentStunningStrike(NwCreature monk, OnCreatureDamage attackData)
    {
        SavingThrowResult stunningStrikeResult = StunningStrike.DoStunningStrike(monk, attackData);

        if (attackData.Target is not NwCreature targetCreature || stunningStrikeResult != SavingThrowResult.Immune)
            return;

        Effect? stunningEffect = MonkUtils.GetKiFocus(monk) switch
        {
            KiFocus.KiFocus1  => Effect.Pacified(),
            KiFocus.KiFocus2 => Effect.Dazed(),
            KiFocus.KiFocus3 => Effect.Paralyze(),
            _ => null
        };

        if (stunningEffect is null) return;

        Effect stunningVfx = Effect.VisualEffect(VfxType.FnfHowlOdd, false, 0.06f);
        stunningEffect.IgnoreImmunity = true;

        targetCreature.ApplyEffect(EffectDuration.Temporary, stunningEffect, NwTimeSpan.FromRounds(1));
        targetCreature.ApplyEffect(EffectDuration.Instant, stunningVfx);
    }

    /// <summary>
    /// Eagle Strike with Ki Focus I incurs a -1 penalty to attack rolls, increased to -2 with Ki Focus II and -3 with Ki Focus III.
    /// </summary>
    private static void AugmentEagleStrike(NwCreature monk, OnCreatureDamage attackData)
    {
        SavingThrowResult stunningStrikeResult = EagleStrike.DoEagleStrike(monk, attackData);

        if (attackData.Target is not NwCreature targetCreature || stunningStrikeResult != SavingThrowResult.Failure)
            return;

        int abDecrease = MonkUtils.GetKiFocus(monk) switch
        {
            KiFocus.KiFocus1 => 1,
            KiFocus.KiFocus2 => 2,
            KiFocus.KiFocus3 => 3,
            _ => 0
        };

        if (abDecrease == 0) return;

        Effect? eagleEffect = targetCreature.ActiveEffects.FirstOrDefault(e => e.Tag == FloatingEagleStrikeTag);
        if (eagleEffect != null)
            targetCreature.RemoveEffect(eagleEffect);

        eagleEffect = Effect.AttackDecrease(abDecrease);
        eagleEffect.Tag = FloatingEagleStrikeTag;

        targetCreature.ApplyEffect(EffectDuration.Temporary, eagleEffect, NwTimeSpan.FromRounds(2));
    }

    /// <summary>
    /// Axiomatic Strike deals +1 bonus positive damage, increased by an additional +1 for every Ki Focus to a maximum
    /// of +4 bonus positive damage.
    /// </summary>
    private static void AugmentAxiomaticStrike(NwCreature monk, OnCreatureDamage attackData)
    {
        AxiomaticStrike.DoAxiomaticStrike(monk, attackData);

        DamageData<int> damageData = attackData.DamageData;
        int positiveDamage = damageData.GetDamageByType(DamageType.Positive);

        int bonusDamage = MonkUtils.GetKiFocus(monk) switch
        {
            KiFocus.KiFocus1 => 2,
            KiFocus.KiFocus2 => 3,
            KiFocus.KiFocus3 => 4,
            _ => 1
        };

        if (positiveDamage == -1) bonusDamage++;

        positiveDamage += bonusDamage;
        damageData.SetDamageByType(DamageType.Positive, positiveDamage);
    }

    /// <summary>
    /// Empty Body gives +2 to fortitude and reflex saving throws. Each Ki Focus gives an additional +2 to
    /// a maximum of +8 to fortitude and reflex saving throws.
    /// </summary>
    private static void AugmentEmptyBody(NwCreature monk)
    {
        EmptyBody.DoEmptyBody(monk);

        byte monkLevel = monk.GetClassInfo(ClassType.Monk)?.Level ?? 0;

        byte bonusAmount = MonkUtils.GetKiFocus(monk) switch
        {
            KiFocus.KiFocus1 => 4,
            KiFocus.KiFocus2 => 6,
            KiFocus.KiFocus3 => 8,
            _ => 2
        };

        Effect emptyBodyEffect = Effect.LinkEffects(
            Effect.SavingThrowIncrease(SavingThrow.Fortitude, bonusAmount),
            Effect.SavingThrowIncrease(SavingThrow.Reflex, bonusAmount)
        );

        monk.ApplyEffect(EffectDuration.Temporary, emptyBodyEffect, NwTimeSpan.FromRounds(monkLevel));
    }
}
