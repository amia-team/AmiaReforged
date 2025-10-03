using AmiaReforged.Classes.Monk.Constants;
using AmiaReforged.Classes.Monk.Techniques.Body;
using AmiaReforged.Classes.Monk.Techniques.Martial;
using AmiaReforged.Classes.Monk.Techniques.Spirit;
using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Monk.Augmentations;

[ServiceBinding(typeof(IAugmentation))]
public sealed class SwingingCenser : IAugmentation
{
    private static readonly NwFeat? BodyKiFeat = NwFeat.FromFeatId(MonkFeat.BodyKiPoint);
    public PathType PathType => PathType.SwingingCenser;

    public void ApplyAttackAugmentation(NwCreature monk, TechniqueType technique, OnCreatureAttack attackData)
    {
        switch (technique)
        {
            case TechniqueType.Stunning:
                AugmentStunningStrike(monk, attackData);
                break;
            case TechniqueType.Eagle:
                EagleStrike.DoEagleStrike(monk, attackData);
                break;
            case TechniqueType.Axiomatic:
                AxiomaticStrike.DoAxiomaticStrike(attackData);
                break;
        }
    }

    public void ApplyCastAugmentation(NwCreature monk, TechniqueType technique, OnSpellCast castData)
    {
        switch (technique)
        {
            case TechniqueType.Wholeness:
                AugmentWholenessOfBody(monk);
                break;
            case TechniqueType.EmptyBody:
                AugmentEmptyBody(monk);
                break;
            case TechniqueType.KiShout:
                AugmentKiShout(monk);
                break;
            case TechniqueType.KiBarrier:
                KiBarrier.DoKiBarrier(monk);
                break;
            case TechniqueType.Quivering:
                QuiveringPalm.DoQuiveringPalm(monk, castData);
                break;
        }
    }

    /// <summary>
    /// Stunning Strike heals the monk or a nearby ally for 1d6 damage. Healing 100 damage with this attack regenerates
    /// a Body Ki Point. Each Ki Focus heals for an additional 1d6, to a maximum of 4d6 damage.
    /// </summary>
    private static void AugmentStunningStrike(NwCreature monk, OnCreatureAttack attackData)
    {
        StunningStrike.DoStunningStrike(attackData);

        if (!monk.IsReactionTypeHostile((NwCreature)attackData.Target)) return;

        int healAmount = MonkUtils.GetKiFocus(monk) switch
        {
            KiFocus.KiFocus1 => Random.Shared.Roll(6, 2),
            KiFocus.KiFocus2 => Random.Shared.Roll(6, 3),
            KiFocus.KiFocus3 => Random.Shared.Roll(6, 4),
            _ => Random.Shared.Roll(6)
        };

        Effect healVfx = Effect.VisualEffect(VfxType.ImpHeadHeal, fScale: 0.7f);

        int totalHealedAmount = 0;

        HealResult selfHealResult = HealSelf(monk, healAmount, healVfx);
        totalHealedAmount += selfHealResult.HealedAmount;

        HealResult allyHealResult = HealAlly(monk, selfHealResult.HealRemainder, healVfx);
        totalHealedAmount += allyHealResult.HealedAmount;

        UpdateBodyKiCounter(monk, totalHealedAmount);
    }

    private class HealResult
    {
        public int HealedAmount { get; init; }
        public int HealRemainder { get; init; }
    }

    private static HealResult HealSelf(NwCreature monk, int healAmount, Effect healVfx)
    {
        int healedAmount = Math.Min(healAmount, monk.MaxHP - monk.HP);

        monk.ApplyEffect(EffectDuration.Instant, Effect.Heal(healedAmount));
        monk.ApplyEffect(EffectDuration.Instant, healVfx);

        int remainder = healAmount - healedAmount;

        return new HealResult
        {
            HealedAmount = healedAmount,
            HealRemainder = remainder
        };
    }

    private static HealResult HealAlly(NwCreature monk, int healRemainder, Effect healVfx)
    {
        if (healRemainder <= 0)
            return new HealResult { HealedAmount = 0, HealRemainder = 0 };

        // Find the most wounded ally to heal
        NwCreature? mostWoundedAlly = monk.Location?.GetObjectsInShape(Shape.Sphere, RadiusSize.Medium, true)
            .OfType<NwCreature>()
            .Where(creature => monk.IsReactionTypeFriendly(creature) && creature.HP < creature.MaxHP)
            .MaxBy(creature => creature.MaxHP - creature.HP);

        if (mostWoundedAlly == null)
            return new HealResult { HealedAmount = 0, HealRemainder = 0 };

        int missingHp = mostWoundedAlly.MaxHP - mostWoundedAlly.HP;
        int healedAmount = Math.Min(healRemainder, missingHp);
        int newRemainder = healRemainder - healedAmount;

        mostWoundedAlly.ApplyEffect(EffectDuration.Instant, Effect.Heal(healRemainder));
        mostWoundedAlly.ApplyEffect(EffectDuration.Instant, healVfx);

        return new HealResult
        {
            HealedAmount = healedAmount,
            HealRemainder = newRemainder
        };
    }

    private static void UpdateBodyKiCounter(NwCreature monk, int totalHealedAmount)
    {
        LocalVariableInt healCounter = monk.GetObjectVariable<LocalVariableInt>("swingingcenser_healcounter");
        healCounter.Value += totalHealedAmount;

        if (healCounter.Value < 100) return;

        MonkUtils.RegenerateBodyKi(monk);
    }

    /// <summary>
    /// Ki Shout exhorts allies with +1 bonus to attack rolls for one turn, with an additional +1 bonus for every Ki Focus.
    /// </summary>
    private static void AugmentKiShout(NwCreature monk)
    {
        KiShout.DoKiShout(monk);

        if (monk.Location == null) return;

        int abBonus = MonkUtils.GetKiFocus(monk) switch
        {
            KiFocus.KiFocus1 => 2,
            KiFocus.KiFocus2 => 3,
            KiFocus.KiFocus3 => 4,
            _ => 1
        };

        Effect abBonusEffect = Effect.LinkEffects(Effect.AttackIncrease(abBonus),
            Effect.VisualEffect(VfxType.DurCessatePositive));

        Effect abBonusVfx = Effect.VisualEffect(VfxType.ImpHeadSonic);

        foreach (NwGameObject nwObject in monk.Location.GetObjectsInShape(Shape.Sphere, RadiusSize.Colossal, false))
        {
            NwCreature creatureInShape = (NwCreature)nwObject;

            if (!monk.IsReactionTypeFriendly(creatureInShape)) continue;

            creatureInShape.ApplyEffect(EffectDuration.Temporary, abBonusEffect, NwTimeSpan.FromRounds(3));
            creatureInShape.ApplyEffect(EffectDuration.Instant, abBonusVfx);
        }
    }

    /// <summary>
    /// Wholeness of Body pulses in a large area around the monk, healing allies.
    /// Each Ki Focus adds a pulse to the heal, to a maximum of four pulses.
    /// </summary>
    private static readonly HashSet<NwCreature> WholenessCooldown = [];
    private static void AugmentWholenessOfBody(NwCreature monk)
    {
        if (OnCooldown(monk)) return;

        byte monkLevel = monk.GetClassInfo(ClassType.Monk)?.Level ?? 0;
        int healAmount = monkLevel * 2;

        int pulseAmount = MonkUtils.GetKiFocus(monk) switch
        {
            KiFocus.KiFocus1 => 2,
            KiFocus.KiFocus2 => 3,
            KiFocus.KiFocus3 => 4,
            _ => 1
        };

        Effect wholenessEffect = Effect.LinkEffects(
            Effect.Heal(healAmount),
            Effect.VisualEffect(VfxType.ImpHealingL, false, 0.7f));

        _ = WholenessPulse(monk, pulseAmount, wholenessEffect);
    }

    private static bool OnCooldown(NwCreature monk)
    {
        if (!WholenessCooldown.Contains(monk)) return false;

        if (monk.IsPlayerControlled(out NwPlayer? player))
        {
            player.FloatingTextString("Wholeness of Body is still active, wait for the effect to end.");
        }

        if (BodyKiFeat != null && monk.KnowsFeat(BodyKiFeat))
        {
            monk.IncrementRemainingFeatUses(BodyKiFeat);
        }

        return true;
    }

    private static async Task WholenessPulse(NwCreature monk, int pulseAmount, Effect wholenessEffect)
    {
        WholenessCooldown.Add(monk);

        try
        {
            for (int i = 0; i < pulseAmount; i++)
            {
                if (monk.IsDead || !monk.IsValid) break;

                monk.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpPulseHoly));

                ApplyAoeHeal(monk, wholenessEffect);

                await NwTask.Delay(TimeSpan.FromSeconds(3));
            }
        }
        finally
        {
            WholenessCooldown.Remove(monk);
        }
    }

    private static void ApplyAoeHeal(NwCreature monk, Effect wholenessEffect)
    {
        if (monk.Location == null) return;

        foreach (NwGameObject nwObject in monk.Location.GetObjectsInShape(Shape.Sphere, RadiusSize.Large,
                     false))
        {
            NwCreature creatureInShape = (NwCreature)nwObject;

            if (!monk.IsReactionTypeFriendly(creatureInShape)) continue;

            creatureInShape.ApplyEffect(EffectDuration.Instant, wholenessEffect);
        }
    }

    /// <summary>
    /// Empty Body creates a soothing winds in a large area around the monk, granting allies 50% concealment and
    /// 2 regeneration. Each Ki Focus increases the regeneration by 2, to a maximum of 8 regeneration.
    /// </summary>
    private static void AugmentEmptyBody(NwCreature monk)
    {
        byte monkLevel = monk.GetClassInfo(ClassType.Monk)?.Level ?? 0;
        const byte concealment = 50;

        int regen = MonkUtils.GetKiFocus(monk) switch
        {
            KiFocus.KiFocus1 => 4,
            KiFocus.KiFocus2 => 6,
            KiFocus.KiFocus3 => 8,
            _ => 2
        };

        Effect emptyBodyEffect = Effect.LinkEffects(
                Effect.Regenerate(regen, NwTimeSpan.FromRounds(1)),
                Effect.Concealment(concealment),
                Effect.VisualEffect(VfxType.DurInvisibility)
        );

        ApplyAoeEmptyBody(monk, emptyBodyEffect, NwTimeSpan.FromRounds(monkLevel));
    }

    private static void ApplyAoeEmptyBody(NwCreature monk, Effect emptyBodyEffect, TimeSpan effectDuration)
    {
        if (monk.Location == null) return;

        monk.Location.ApplyEffect(EffectDuration.Instant,
            Effect.VisualEffect(VfxType.FnfDispelGreater, false, 0.3f));

        foreach (NwGameObject nwObject in monk.Location.GetObjectsInShape(Shape.Sphere, RadiusSize.Large,
                     false))
        {
            NwCreature creatureInShape = (NwCreature)nwObject;

            if (!monk.IsReactionTypeFriendly(creatureInShape)) continue;

            creatureInShape.ApplyEffect(EffectDuration.Temporary, emptyBodyEffect, effectDuration);
        }
    }
}


