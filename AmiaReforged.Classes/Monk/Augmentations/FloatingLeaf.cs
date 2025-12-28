using AmiaReforged.Classes.Monk.Techniques.Cast;
using AmiaReforged.Classes.Monk.Techniques.Attack;
using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NWN.Core;

namespace AmiaReforged.Classes.Monk.Augmentations;

[ServiceBinding(typeof(IAugmentation))]
public class FloatingLeaf : IAugmentation
{
    private const string FloatingEagleStrikeTag = nameof(PathType.FloatingLeaf) +  nameof(TechniqueType.EagleStrike);
    private const string FloatingEmptyBodyTag = nameof(PathType.FloatingLeaf) + nameof(TechniqueType.EmptyBody);
    public PathType PathType => PathType.FloatingLeaf;

    public void ApplyAttackAugmentation(NwCreature monk, OnCreatureAttack attackData)
    {
        AugmentAxiomaticStrike(monk, attackData);
    }

    public void ApplyDamageAugmentation(NwCreature monk, TechniqueType technique, OnCreatureDamage damageData)
    {
        switch (technique)
        {
            case TechniqueType.StunningStrike:
                AugmentStunningStrike(damageData);
                break;
            case TechniqueType.EagleStrike:
                AugmentEagleStrike(monk, damageData);
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
    private static void AugmentStunningStrike(OnCreatureDamage damageData)
    {
        SavingThrowResult stunningStrikeResult = StunningStrike.DoStunningStrike(damageData);

        if (damageData.Target is not NwCreature targetCreature ||
            damageData.DamagedBy is not NwCreature monk || stunningStrikeResult != SavingThrowResult.Immune)
            return;

        Effect? stunningEffect = MonkUtils.GetKiFocus(monk) switch
        {
            KiFocus.KiFocus1  => Effect.Pacified(),
            KiFocus.KiFocus2 => Effect.Dazed(),
            KiFocus.KiFocus3 => Effect.Stunned(),
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
    private static void AugmentEagleStrike(NwCreature monk, OnCreatureDamage damageData)
    {
        SavingThrowResult stunningStrikeResult = EagleStrike.DoEagleStrike(monk, damageData);

        if (damageData.Target is not NwCreature targetCreature || stunningStrikeResult != SavingThrowResult.Failure)
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
    private static void AugmentAxiomaticStrike(NwCreature monk, OnCreatureAttack attackData)
    {
        AxiomaticStrike.DoAxiomaticStrike(monk, attackData);

        DamageData<short> damageData = attackData.DamageData;
        short positiveDamage = damageData.GetDamageByType(DamageType.Positive);

        int bonusDamage = MonkUtils.GetKiFocus(monk) switch
        {
            KiFocus.KiFocus1 => 2,
            KiFocus.KiFocus2 => 3,
            KiFocus.KiFocus3 => 4,
            _ => 1
        };

        if (attackData.AttackResult == AttackResult.CriticalHit)
            bonusDamage *= MonkUtils.GetCritMultiplier(attackData, monk);

        if (positiveDamage == -1) bonusDamage++;

        positiveDamage += (short)bonusDamage;
        damageData.SetDamageByType(DamageType.Positive, positiveDamage);
    }

    /// <summary>
    /// While affected by Empty Body, Ki Focus I grants the ability to take weightless leaps.
    /// Ki Focus II grants haste. Ki Focus III grants Epic Dodge.
    /// </summary>
    private static void AugmentEmptyBody(NwCreature monk)
    {
        EmptyBody.DoEmptyBody(monk);

        KiFocus? kiFocus = MonkUtils.GetKiFocus(monk);
        if (kiFocus == null) return;

        byte monkLevel = monk.GetClassInfo(ClassType.Monk)?.Level ?? 0;

        Effect? emptyBodyEffect = null;

        switch (kiFocus)
        {
            case KiFocus.KiFocus1:
                emptyBodyEffect = Effect.VisualEffect(VfxType.None);
                break;
            case KiFocus.KiFocus2:
                emptyBodyEffect = Effect.Haste();
                break;
            case KiFocus.KiFocus3:
                emptyBodyEffect = Effect.LinkEffects(Effect.Haste(), Effect.BonusFeat(Feat.EpicDodge!));
                break;
        }

        if (emptyBodyEffect == null) return;
        emptyBodyEffect.Tag = FloatingEmptyBodyTag;

        monk.ApplyEffect(EffectDuration.Temporary, emptyBodyEffect, NwTimeSpan.FromRounds(monkLevel));
    }

    /// <summary>
    /// Does the weightless leap for Floating Leaf monk, intercepts the CastServiceTechnique
    /// </summary>
    /// <param name="monk"></param>
    /// <returns>True if the leap was successful, false if not</returns>
    public static bool TryWeightlessLeap(NwCreature monk)
    {
        bool hasEmptyBody = monk.ActiveEffects.Any(e => e.Tag == FloatingEmptyBodyTag);
        if (!hasEmptyBody || IsNoFlyArea(monk)) return false;

        monk.ControllingPlayer?.EnterTargetMode(Leap, new TargetModeSettings
        {
            ValidTargets = ObjectTypes.Placeable | ObjectTypes.Creature | ObjectTypes.Door | ObjectTypes.Tile
        });

        return true;

        void Leap(ModuleEvents.OnPlayerTarget targetingData)
        {
            if (monk.Area == null || monk.Location == null) return;

            Location targetLocation = Location.Create(monk.Area, targetingData.TargetPosition, monk.Location.Rotation);

            if (monk.Location.Distance(targetLocation) > 20)
            {
                monk.ControllingPlayer?.FloatingTextString("You can only jump within 20 meters", false, false);
                return;
            }

            Effect flyEffect = Effect.DisappearAppear(targetLocation);
            monk.ApplyEffect(EffectDuration.Temporary, flyEffect, TimeSpan.FromSeconds(4));
        }
    }

    private static bool IsNoFlyArea(NwCreature monk)
    {
        if (monk.Area != null && monk.Area.GetObjectVariable<LocalVariableInt>("CS_NO_FLY").Value != 1) return false;

        monk.ControllingPlayer?.FloatingTextString("- You are unable to fly in this area! -",
            false, false);

        return true;
    }
}
