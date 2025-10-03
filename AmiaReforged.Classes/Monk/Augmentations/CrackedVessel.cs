using AmiaReforged.Classes.Monk.Techniques.Body;
using AmiaReforged.Classes.Monk.Techniques.Martial;
using AmiaReforged.Classes.Monk.Techniques.Spirit;
using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Monk.Augmentations;

[ServiceBinding(typeof(IAugmentation))]
public sealed class CrackedVessel : IAugmentation
{
    private const string CrackedQuiveringTag = "crackedvessel_quiveringpalm";
    private const string CrackedEmptyBodyTag = "crackedvessel_emptybody";

    public PathType PathType => PathType.CrackedVessel;
    public void ApplyAttackAugmentation(NwCreature monk, TechniqueType technique, OnCreatureAttack attackData)
    {
        MonkCondition condition = GetMonkCondition(monk);

        switch (technique)
        {
            case TechniqueType.Axiomatic:
                AugmentAxiomaticStrike(monk, attackData, condition);
                break;
            case TechniqueType.Stunning:
                StunningStrike.DoStunningStrike(attackData);
                break;
            case TechniqueType.Eagle:
                EagleStrike.DoEagleStrike(monk, attackData);
                break;
        }

    }
    public void ApplyCastAugmentation(NwCreature monk, TechniqueType technique, OnSpellCast castData)
    {
        MonkCondition condition = GetMonkCondition(monk);

        switch (technique)
        {
            case TechniqueType.Wholeness:
                AugmentWholenessOfBody(monk, condition);
                break;
            case TechniqueType.EmptyBody:
                AugmentEmptyBody(monk, condition);
                break;
            case TechniqueType.Quivering:
                AugmentQuiveringPalm(monk, castData, condition);
                break;
            case TechniqueType.KiBarrier:
                KiBarrier.DoKiBarrier(monk);
                break;
            case TechniqueType.KiShout:
                KiShout.DoKiShout(monk);
                break;
        }
    }

    private enum MonkCondition
    {
        Healthy,
        Injured,
        BadlyWounded,
        NearDeath
    }

    private static MonkCondition GetMonkCondition(NwCreature monk)
    {
        double hpPercentage = (double)monk.HP / monk.MaxHP;

        return hpPercentage switch
        {
            < 0.25 => MonkCondition.NearDeath,
            < 0.50 => MonkCondition.BadlyWounded,
            < 0.75 => MonkCondition.Injured,
            _ => MonkCondition.Healthy
        };
    }

    /// <summary>
    /// Axiomatic Strike deals 1d2 bonus negative energy damage when the monk is injured, 1d4 when badly wounded,
    /// and 1d6 when near death. In addition, every 100 damage made with this attack against living enemies
    /// regenerates a Body Ki Point. Each Ki Focus multiplies the damage bonus,
    /// to a maximum of 4d2, 4d4, and 4d6 bonus negative energy damage.
    /// </summary>
    private static void AugmentAxiomaticStrike(NwCreature monk, OnCreatureAttack attackData, MonkCondition condition)
    {
        AxiomaticStrike.DoAxiomaticStrike(attackData);

        if (attackData.Target is not NwCreature targetCreature) return;
        if (condition == MonkCondition.Healthy) return;

        int damageSides = condition switch
        {
            MonkCondition.Injured => 2,
            MonkCondition.BadlyWounded => 4,
            MonkCondition.NearDeath => 6,
            _ => 0
        };

        int damageDice = MonkUtils.GetKiFocus(monk) switch
        {
            KiFocus.KiFocus1 => 2,
            KiFocus.KiFocus2 => 3,
            KiFocus.KiFocus3 => 4,
            _ => 1
        };

        int bonusNegativeDamage = Random.Shared.Roll(damageSides, damageDice);

        DamageData<short> damageData = attackData.DamageData;
        short negativeDamage = damageData.GetDamageByType(DamageType.Negative);

        int negativeDamageBefore = negativeDamage;

        negativeDamage += (short)bonusNegativeDamage;
        damageData.SetDamageByType(DamageType.Negative, negativeDamage);

        byte bonusDamageDealt = CalculateBonusDamageDealt(targetCreature, bonusNegativeDamage, negativeDamageBefore);

        UpdateBodyKiCounter(monk, bonusDamageDealt);
    }

    private static byte CalculateBonusDamageDealt(NwCreature targetCreature, float bonusNegativeDamage, float negativeDamageBefore)
    {
        int highestNegativeResistance = targetCreature.ActiveEffects
            .Where(e => e.EffectType == EffectType.DamageResistance && e.IntParams[0] == (int)DamageType.Negative)
            .Select(e => e.IntParams[1])
            .DefaultIfEmpty(0)
            .Max();

        int negativeVulnerability = targetCreature.ActiveEffects
            .Where(e => e.EffectType == EffectType.DamageImmunityDecrease && e.IntParams[0] == (int)DamageType.Negative)
            .Select(e => e.IntParams[1])
            .DefaultIfEmpty(0)
            .Max();

        int negativeImmunity = targetCreature.ActiveEffects
            .Where(e => e.EffectType == EffectType.DamageImmunityIncrease && e.IntParams[0] == (int)DamageType.Negative)
            .Select(e => e.IntParams[1])
            .DefaultIfEmpty(0)
            .Max();

        float negativeDamageModifier = 1f + (negativeImmunity - negativeVulnerability) / 100f;

        bonusNegativeDamage *= negativeDamageModifier;

        negativeDamageBefore *= negativeDamageModifier;

        float resistanceLeft = Math.Max(0, highestNegativeResistance - negativeDamageBefore);

        bonusNegativeDamage -= resistanceLeft;

        if (bonusNegativeDamage < 0)
            return 0;

        return (byte)bonusNegativeDamage;
    }

    private static void UpdateBodyKiCounter(NwCreature monk, int bonusDamageDealt)
    {
        if (bonusDamageDealt <= 0) return;

        LocalVariableInt damageCounter = monk.GetObjectVariable<LocalVariableInt>("crackedvessel_damagecounter");

        damageCounter.Value += bonusDamageDealt;

        if (damageCounter.Value < 100) return;

        MonkUtils.RegenerateBodyKi(monk);
    }

    /// <summary>
    /// Wholeness of Body unleashes 2d6 negative energy and physical damage in a large radius when the monk is injured,
    /// 2d8 when badly wounded, and 2d10 when near death. Fortitude saving throw halves the damage. Each Ki Focus
    /// multiplies the damage bonus, to a maximum of 8d6, 8d8, and 8d10 negative energy and physical damage.
    /// </summary>
    private static void AugmentWholenessOfBody(NwCreature monk, MonkCondition condition)
    {
        WholenessOfBody.DoWholenessOfBody(monk);

        if (condition == MonkCondition.Healthy) return;
        if (monk.Location == null) return;

        int dc = MonkUtils.CalculateMonkDc(monk);

        int damageSides = condition switch
        {
            MonkCondition.Injured => 6,
            MonkCondition.BadlyWounded => 8,
            MonkCondition.NearDeath => 10,
            _ => 0
        };

        int damageDice = MonkUtils.GetKiFocus(monk) switch
        {
            KiFocus.KiFocus1 => 4,
            KiFocus.KiFocus2 => 6,
            KiFocus.KiFocus3 => 8,
            _ => 2
        };

        Effect aoeVfx = MonkUtils.ResizedVfx(VfxType.FnfLosEvil30, RadiusSize.Large);

        monk.ApplyEffect(EffectDuration.Instant, aoeVfx);
        foreach (NwGameObject obj in monk.Location.GetObjectsInShape(Shape.Sphere, RadiusSize.Large, false))
        {
            if (obj is not NwCreature hostileCreature || !monk.IsReactionTypeHostile(hostileCreature)) continue;

            CreatureEvents.OnSpellCastAt.Signal(monk, hostileCreature, NwSpell.FromSpellType(Spell.NegativeEnergyBurst)!);

            int damageAmount = Random.Shared.Roll(damageSides, damageDice);

            SavingThrowResult savingThrowResult =
                hostileCreature.RollSavingThrow(SavingThrow.Fortitude, dc, SavingThrowType.Negative, monk);

            if (savingThrowResult == SavingThrowResult.Success)
            {
                damageAmount /= 2;
                hostileCreature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpFortitudeSavingThrowUse));
            }

            Effect wholenessEffect = Effect.LinkEffects(
                Effect.Damage(damageAmount, DamageType.Negative),
                Effect.Damage(damageAmount, DamageType.Piercing),
                Effect.VisualEffect(VfxType.ImpNegativeEnergy)
            );

            hostileCreature.ApplyEffect(EffectDuration.Instant, wholenessEffect);
        }
    }

    /// <summary>
    /// Empty Body grants 5% physical damage immunity when the monk is injured, 10% when badly wounded, and 15% when
    /// near death. This effect is only granted while the monk is in combat. Each Ki Focus grants 5% more physical
    /// damage immunity, to a maximum of 20%, 25%, and 30% physical damage immunity.
    /// </summary>
    private static void AugmentEmptyBody(NwCreature monk, MonkCondition condition)
    {
        EmptyBody.DoEmptyBody(monk);

        if (!monk.IsInCombat || condition == MonkCondition.Healthy) return;

        int monkLevel = monk.GetClassInfo(ClassType.Monk)?.Level ?? 0;

        int pctImmunityBase = condition switch
        {
            MonkCondition.Injured => 5,
            MonkCondition.BadlyWounded => 10,
            MonkCondition.NearDeath => 15,
            _ => 0
        };

        int pctImmunityBonus = MonkUtils.GetKiFocus(monk) switch
        {
            KiFocus.KiFocus1 => 5,
            KiFocus.KiFocus2 => 10,
            KiFocus.KiFocus3 => 15,
            _ => 0
        };

        int pctImmunityTotal = pctImmunityBase + pctImmunityBonus;

        Effect? emptyBodyEffect = monk.ActiveEffects.FirstOrDefault(e => e.Tag == CrackedEmptyBodyTag);
        if (emptyBodyEffect != null)
            monk.RemoveEffect(emptyBodyEffect);

        emptyBodyEffect = Effect.LinkEffects(
            Effect.DamageIncrease(pctImmunityTotal, DamageType.Piercing),
            Effect.DamageIncrease(pctImmunityTotal, DamageType.Slashing),
            Effect.DamageIncrease(pctImmunityTotal, DamageType.Bludgeoning));

        emptyBodyEffect.SubType = EffectSubType.Extraordinary;
        emptyBodyEffect.Tag = CrackedEmptyBodyTag;

        monk.ApplyEffect(EffectDuration.Temporary, emptyBodyEffect, NwTimeSpan.FromRounds(monkLevel));
    }

    /// <summary>
    /// Quivering Palm inflicts 5% negative energy and physical damage vulnerability for three rounds.
    /// Each Ki Focus adds 5% to a maximum of 20%.
    /// </summary>
    private static void AugmentQuiveringPalm(NwCreature monk, OnSpellCast castData, MonkCondition condition)
    {
        TouchAttackResult touchAttackResult = QuiveringPalm.DoQuiveringPalm(monk, castData);

        if (castData.TargetObject is not NwCreature targetCreature) return;
        if (touchAttackResult is TouchAttackResult.Miss) return;

        if (condition == MonkCondition.Healthy) return;

        int pctVulnerability = MonkUtils.GetKiFocus(monk) switch
        {
            KiFocus.KiFocus1 => 10,
            KiFocus.KiFocus2 => 15,
            KiFocus.KiFocus3 => 20,
            _ => 5
        };

        Effect? quiveringEffect = targetCreature.ActiveEffects.FirstOrDefault(e => e.Tag == CrackedQuiveringTag);
        if (quiveringEffect != null)
            targetCreature.RemoveEffect(quiveringEffect);


        quiveringEffect = Effect.LinkEffects(
            Effect.DamageImmunityDecrease(DamageType.Negative, pctVulnerability),
            Effect.DamageImmunityDecrease(DamageType.Piercing, pctVulnerability),
            Effect.DamageImmunityDecrease(DamageType.Slashing, pctVulnerability),
            Effect.DamageImmunityDecrease(DamageType.Bludgeoning, pctVulnerability),
            Effect.VisualEffect(VfxType.DurCessateNegative)
        );

        quiveringEffect.Tag = CrackedQuiveringTag;

        Effect quiveringVfx = Effect.VisualEffect(VfxType.ImpNegativeEnergy, false, 0.7f);

        targetCreature.ApplyEffect(EffectDuration.Temporary, quiveringEffect, NwTimeSpan.FromRounds(3));
        targetCreature.ApplyEffect(EffectDuration.Instant, quiveringVfx);
    }
}
