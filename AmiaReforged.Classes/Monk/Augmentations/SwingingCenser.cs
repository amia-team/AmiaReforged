using AmiaReforged.Classes.Monk.Techniques.Body;
using AmiaReforged.Classes.Monk.Techniques.Martial;
using AmiaReforged.Classes.Monk.Techniques.Spirit;
using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Monk.Augmentations;

[ServiceBinding(typeof(IAugmentation))]
public sealed class SwingingCenser(ScriptHandleFactory scriptHandleFactory) : IAugmentation
{
    public PathType PathType => PathType.SwingingCenser;

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
            case TechniqueType.EmptyBody:
                AugmentEmptyBody(monk);
                break;
            case TechniqueType.KiShout:
                AugmentKiShout(monk);
                break;
            case TechniqueType.KiBarrier:
                KiBarrier.DoKiBarrier(monk);
                break;
            case TechniqueType.QuiveringPalm:
                QuiveringPalm.DoQuiveringPalm(monk, castData);
                break;
        }
    }

    /// <summary>
    /// Stunning Strike heals the monk or a nearby ally for 1d6 damage. Each Ki Focus heals for an additional 1d6, to a maximum of 4d6 damage.
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

        // Find the most wounded ally to heal
        NwCreature? mostWoundedAlly = monk.Location?.GetObjectsInShapeByType<NwCreature>(Shape.Sphere, RadiusSize.Medium, true)
            .Where(creature => monk.IsReactionTypeFriendly(creature) && creature.HP < creature.MaxHP)
            .MaxBy(creature => creature.MaxHP - creature.HP);

        mostWoundedAlly?.ApplyEffect(EffectDuration.Instant, Effect.Heal(healAmount));
        mostWoundedAlly?.ApplyEffect(EffectDuration.Instant, healVfx);
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

        Effect abBonusVfx = Effect.VisualEffect(VfxType.ImpHeadHoly);

        foreach (NwGameObject nwObject in monk.Location.GetObjectsInShape(Shape.Sphere, RadiusSize.Colossal, false))
        {
            NwCreature creatureInShape = (NwCreature)nwObject;

            if (!monk.IsReactionTypeFriendly(creatureInShape)) continue;

            creatureInShape.ApplyEffect(EffectDuration.Temporary, abBonusEffect, NwTimeSpan.FromRounds(3));
            creatureInShape.ApplyEffect(EffectDuration.Instant, abBonusVfx);
        }
    }

    private const string WholenessPulseTag = "wholenesspulse";

    /// <summary>
    /// Wholeness of Body pulses in a large area around the monk, healing allies.
    /// Each Ki Focus adds a pulse to the heal, to a maximum of four pulses.
    /// </summary>
    private void AugmentWholenessOfBody(NwCreature monk)
    {
        if (monk.ActiveEffects.Any(e => e.Tag == WholenessPulseTag))
            return;

        byte monkLevel = monk.GetClassInfo(ClassType.Monk)?.Level ?? 0;
        int healAmount = monkLevel * 2;

        int pulseAmount = MonkUtils.GetKiFocus(monk) switch
        {
            KiFocus.KiFocus1 => 2,
            KiFocus.KiFocus2 => 3,
            KiFocus.KiFocus3 => 4,
            _ => 1
        };

        TimeSpan duration = TimeSpan.FromSeconds((pulseAmount - 1) * 3);
        TimeSpan pulseInterval = TimeSpan.FromSeconds(3);

        Effect wholenessEffect = Effect.LinkEffects(Effect.Heal(healAmount),
            Effect.VisualEffect(VfxType.ImpHealingL, false, 0.7f));

        ScriptCallbackHandle doPulse
            = scriptHandleFactory.CreateUniqueHandler(_ => PulseHeal(monk, wholenessEffect));

        Effect wholenessPulse = Effect.RunAction(doPulse, doPulse, doPulse, pulseInterval);
        wholenessPulse.Tag = WholenessPulseTag;

        monk.ApplyEffect(EffectDuration.Temporary, wholenessPulse, duration);
    }

    private static ScriptHandleResult PulseHeal(NwCreature monk, Effect wholenessEffect)
    {
        if (monk.IsDead || !monk.IsValid || monk.Location == null) return ScriptHandleResult.True;

        monk.ApplyEffect(EffectDuration.Instant, MonkUtils.ResizedVfx(VfxType.ImpPulseHoly, RadiusSize.Large));

        foreach (NwCreature creatureInShape in monk.Location.GetObjectsInShapeByType<NwCreature>(Shape.Sphere, RadiusSize.Large,false))
        {
            if (!monk.IsReactionTypeFriendly(creatureInShape)) continue;

            creatureInShape.ApplyEffect(EffectDuration.Instant, wholenessEffect);
        }

        return ScriptHandleResult.True;
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


