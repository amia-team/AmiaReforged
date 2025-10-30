using AmiaReforged.Classes.Monk.Techniques.Body;
using AmiaReforged.Classes.Monk.Techniques.Martial;
using AmiaReforged.Classes.Monk.Techniques.Spirit;
using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Monk.Augmentations;

[ServiceBinding(typeof(IAugmentation))]
public sealed class SplinteredChalice : IAugmentation
{
    private const string SplinteredEmptyBodyTag = nameof(PathType.SplinteredChalice) + nameof(TechniqueType.EmptyBody);

    public PathType PathType => PathType.SplinteredChalice;

    public void ApplyAttackAugmentation(NwCreature monk, OnCreatureAttack attackData)
    {
        MonkCondition condition = GetMonkCondition(monk);
        AugmentAxiomaticStrike(monk, attackData, condition);
    }

    public void ApplyDamageAugmentation(NwCreature monk, TechniqueType technique, OnCreatureDamage damageData)
    {
        switch (technique)
        {
            case TechniqueType.StunningStrike:
                StunningStrike.DoStunningStrike(damageData);
                break;
            case TechniqueType.EagleStrike:
                EagleStrike.DoEagleStrike(monk, damageData);
                break;
        }
    }

    public void ApplyCastAugmentation(NwCreature monk, TechniqueType technique, OnSpellCast castData)
    {
        MonkCondition condition = GetMonkCondition(monk);

        switch (technique)
        {
            case TechniqueType.WholenessOfBody:
                AugmentWholenessOfBody(monk, condition);
                break;
            case TechniqueType.EmptyBody:
                AugmentEmptyBody(monk, condition);
                break;
            case TechniqueType.QuiveringPalm:
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
        Healthy = 0,
        Injured = 1,
        BadlyWounded = 2,
        NearDeath = 3
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
    /// Axiomatic Strike deals Xd1 bonus negative energy. Ki Focus I increases this to Xd2, Ki Focus II to Xd3,
    /// and Ki Focus III to Xd4.
    /// </summary>
    private static void AugmentAxiomaticStrike(NwCreature monk, OnCreatureAttack attackData, MonkCondition condition)
    {
        AxiomaticStrike.DoAxiomaticStrike(monk, attackData);

        if (condition == MonkCondition.Healthy) return;

        int damageSides = MonkUtils.GetKiFocus(monk) switch
        {
            KiFocus.KiFocus1 => 2,
            KiFocus.KiFocus2 => 3,
            KiFocus.KiFocus3 => 4,
            _ => 1
        };

        int bonusDamage = Random.Shared.Roll((int)condition, damageSides);

        DamageData<short> damageData = attackData.DamageData;
        short negativeDamage = damageData.GetDamageByType(DamageType.Negative);

        if (attackData.AttackResult == AttackResult.CriticalHit)
            bonusDamage *= MonkUtils.GetCritMultiplier(attackData, monk);

        if (negativeDamage == -1) bonusDamage++;

        negativeDamage += (short)bonusDamage;
        damageData.SetDamageByType(DamageType.Negative, negativeDamage);
    }


    /// <summary>
    /// Wholeness of Body unleashes Xd6 negative and piercing damage in a large area around the monk, with a successful
    /// fortitude save halving the damage. Ki Focus I increases this to Xd8, Ki Focus II to Xd10, and Ki Focus III to Xd12 damage.
    /// </summary>
    private static void AugmentWholenessOfBody(NwCreature monk, MonkCondition condition)
    {
        if (condition == MonkCondition.Healthy) return;

        WholenessAoe(monk, condition);

        WholenessOfBody.DoWholenessOfBody(monk);
    }

    private static void WholenessAoe(NwCreature monk, MonkCondition condition)
    {
        if (monk.Location == null) return;

        int diceAmount = MonkUtils.GetKiFocus(monk) switch
        {
            KiFocus.KiFocus1 => 20,
            KiFocus.KiFocus2 => 15,
            KiFocus.KiFocus3 => 10,
            _ => 5
        };

        int dc = MonkUtils.CalculateMonkDc(monk);

        Effect aoeVfx = MonkUtils.ResizedVfx(VfxType.FnfLosEvil30, RadiusSize.Large);

        monk.ApplyEffect(EffectDuration.Instant, aoeVfx);
        foreach (NwCreature hostileCreature in monk.Location.GetObjectsInShapeByType<NwCreature>(Shape.Sphere, RadiusSize.Large, false))
        {
            if (!monk.IsReactionTypeHostile(hostileCreature)) continue;

            CreatureEvents.OnSpellCastAt.Signal(monk, hostileCreature, NwSpell.FromSpellType(Spell.NegativeEnergyBurst)!);

            int damageAmount = Random.Shared.Roll((int)condition, diceAmount);

            SavingThrowResult savingThrowResult =
                hostileCreature.RollSavingThrow(SavingThrow.Fortitude, dc, SavingThrowType.Negative, monk);

            if (savingThrowResult == SavingThrowResult.Success)
            {
                damageAmount /= 2;
                hostileCreature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpFortitudeSavingThrowUse));
            }

            _ = ApplyWholenessDamage(hostileCreature, monk, damageAmount);
        }
    }

    private static async Task ApplyWholenessDamage(NwCreature hostileCreature, NwCreature monk, int damageAmount)
    {
        await monk.WaitForObjectContext();
        Effect wholenessEffect = Effect.LinkEffects(
            Effect.Damage(damageAmount, DamageType.Negative),
            Effect.Damage(damageAmount, DamageType.Piercing),
            Effect.VisualEffect(VfxType.ImpNegativeEnergy)
        );

        hostileCreature.ApplyEffect(EffectDuration.Instant, wholenessEffect);
    }

    /// <summary>
    /// While in combat, Empty Body grants X times 5 % physical damage immunity. Each Ki Focus adds a further 5 % immunity.
    /// </summary>
    private static void AugmentEmptyBody(NwCreature monk, MonkCondition condition)
    {
        EmptyBody.DoEmptyBody(monk);

        if (!monk.IsInCombat || condition == MonkCondition.Healthy) return;

        int monkLevel = monk.GetClassInfo(ClassType.Monk)?.Level ?? 0;

        int pctImmunityBase = 5 * (int)condition;

        int pctImmunityBonus = MonkUtils.GetKiFocus(monk) switch
        {
            KiFocus.KiFocus1 => 5,
            KiFocus.KiFocus2 => 10,
            KiFocus.KiFocus3 => 15,
            _ => 0
        };

        int pctImmunityTotal = pctImmunityBase + pctImmunityBonus;

        Effect? existingEffect = monk.ActiveEffects.FirstOrDefault(e => e.Tag == SplinteredEmptyBodyTag);
        if (existingEffect != null)
            monk.RemoveEffect(existingEffect);

        Effect emptyBodyEffect = Effect.LinkEffects(
            Effect.DamageImmunityIncrease(DamageType.Piercing, pctImmunityTotal),
            Effect.DamageImmunityIncrease(DamageType.Slashing, pctImmunityTotal),
            Effect.DamageImmunityIncrease(DamageType.Bludgeoning, pctImmunityTotal));

        emptyBodyEffect.SubType = EffectSubType.Extraordinary;
        emptyBodyEffect.Tag = SplinteredEmptyBodyTag;

        monk.ApplyEffect(EffectDuration.Temporary, emptyBodyEffect, NwTimeSpan.FromRounds(monkLevel));
    }

    /// <summary>
    /// Quivering Palm inflicts an additional 30dX negative damage.
    /// Each Ki Focus inflicts 30 % negative energy vulnerability against attack.
    /// </summary>
    private static void AugmentQuiveringPalm(NwCreature monk, OnSpellCast castData, MonkCondition condition)
    {
        TouchAttackResult touchAttackResult = QuiveringPalm.DoQuiveringPalm(monk, castData);

        if (castData.TargetObject is not NwCreature targetCreature || touchAttackResult is TouchAttackResult.Miss)
            return;

        if (condition == MonkCondition.Healthy) return;

        int vulnerabilityPct = MonkUtils.GetKiFocus(monk) switch
        {
            KiFocus.KiFocus1 => 30,
            KiFocus.KiFocus2 => 60,
            KiFocus.KiFocus3 => 90,
            _ => 0
        };

        targetCreature.ApplyEffect(EffectDuration.Temporary, Effect.DamageImmunityDecrease(DamageType.Negative,
            vulnerabilityPct), TimeSpan.FromSeconds(0.5f));


        int damage = Random.Shared.Roll((int)condition, 30);

        _ = ApplyQuivering(targetCreature, monk, damage);
    }

    private static async Task ApplyQuivering(NwCreature targetCreature, NwCreature monk, int damage)
    {
        await monk.WaitForObjectContext();
        Effect quiveringEffect = Effect.LinkEffects(Effect.Damage(damage, DamageType.Negative),
            Effect.VisualEffect(VfxType.ImpNegativeEnergy));

        targetCreature.ApplyEffect(EffectDuration.Instant, quiveringEffect);
    }
}
