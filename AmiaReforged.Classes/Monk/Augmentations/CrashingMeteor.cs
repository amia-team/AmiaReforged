using AmiaReforged.Classes.Monk.Techniques.Body;
using AmiaReforged.Classes.Monk.Techniques.Martial;
using AmiaReforged.Classes.Monk.Techniques.Spirit;
using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.Classes.Monk.Augmentations;

public static class CrashingMeteor
{
    public static void ApplyAugmentations(TechniqueType technique, OnSpellCast? castData = null, OnCreatureAttack? attackData = null)
    {
        switch (technique)
        {
            case TechniqueType.Stunning:
                if (attackData != null) AugmentStunningStrike(attackData);
                break;
            case TechniqueType.Axiomatic:
                if (attackData != null) AugmentAxiomaticStrike(attackData);
                break;
            case TechniqueType.KiShout:
                if (castData != null) AugmentKiShout(castData);
                break;
            case TechniqueType.Wholeness:
                if (castData != null) AugmentWholenessOfBody(castData);
                break;
            case TechniqueType.KiBarrier:
                if (castData != null) KiBarrier.DoKiBarrier(castData);
                break;
            case TechniqueType.Eagle:
                if (attackData != null) EagleStrike.DoEagleStrike(attackData);
                break;
            case TechniqueType.EmptyBody:
                if (castData != null) EmptyBody.DoEmptyBody(castData);
                break;
            case TechniqueType.Quivering:
                if (castData != null) QuiveringPalm.DoQuiveringPalm(castData);
                break;
        }
    }

    /// <summary>
    ///     Stunning Strike deals 2d6 elemental damage in a large area around the target. The damage isn’t multiplied by
    ///     critical hits and a successful reflex save halves the damage. Each Ki Focus adds 2d6 to a maximum of 8d6 elemental
    ///     damage.
    /// </summary>
    private static void AugmentStunningStrike(OnCreatureAttack attackData)
    {
        StunningStrike.DoStunningStrike(attackData);

        NwCreature monk = attackData.Attacker;
        DamageType elementalType = MonkUtils.GetElementalType(monk);
        int dc = MonkUtils.CalculateMonkDc(monk);
        int diceAmount = MonkUtils.GetKiFocus(monk) switch
        {
            KiFocus.KiFocus1 => 4,
            KiFocus.KiFocus2 => 6,
            KiFocus.KiFocus3 => 8,
            _ => 2
        };
        Effect elementalAoeVfx = elementalType switch
        {
            DamageType.Fire => MonkUtils.ResizedVfx(VfxType.FnfFireball, RadiusSize.Large),
            DamageType.Cold => MonkUtils.ResizedVfx(VfxType.ImpFrostL, RadiusSize.Large),
            DamageType.Electrical => MonkUtils.ResizedVfx(VfxType.FnfElectricExplosion, RadiusSize.Large),
            DamageType.Acid => MonkUtils.ResizedVfx(VfxType.ImpAcidS, RadiusSize.Large),
            _ => MonkUtils.ResizedVfx(VfxType.FnfFireball, RadiusSize.Large)
        };
        Effect elementalDamageVfx = elementalType switch
        {
            DamageType.Fire => Effect.VisualEffect(VfxType.ImpFlameS),
            DamageType.Cold => Effect.VisualEffect(VfxType.ImpFrostS),
            DamageType.Electrical => Effect.VisualEffect(VfxType.ComHitElectrical),
            DamageType.Acid => Effect.VisualEffect(VfxType.ImpAcidS),
            _ => Effect.VisualEffect(VfxType.ImpFlameS)
        };
        SavingThrowType elementalSaveType = elementalType switch
        {
            DamageType.Fire => SavingThrowType.Fire,
            DamageType.Cold => SavingThrowType.Cold,
            DamageType.Electrical => SavingThrowType.Electricity,
            DamageType.Acid => SavingThrowType.Acid,
            _ => SavingThrowType.Fire
        };

        attackData.Target.ApplyEffect(EffectDuration.Instant, elementalAoeVfx);
        foreach (NwGameObject nwObject in monk.Location!.GetObjectsInShape(Shape.Sphere, RadiusSize.Large, true,
                     ObjectTypes.Creature | ObjectTypes.Door | ObjectTypes.Placeable))
        {
            NwCreature creatureInShape = (NwCreature)nwObject;
            if (monk.IsReactionTypeFriendly(creatureInShape)) continue;

            CreatureEvents.OnSpellCastAt.Signal(monk, creatureInShape, NwSpell.FromSpellType(Spell.Fireball)!);

            bool hasEvasion = creatureInShape.KnowsFeat(NwFeat.FromFeatType(Feat.Evasion)!);
            bool hasImprovedEvasion = creatureInShape.KnowsFeat(NwFeat.FromFeatType(Feat.ImprovedEvasion)!);

            SavingThrowResult savingThrowResult =
                creatureInShape.RollSavingThrow(SavingThrow.Reflex, dc, elementalSaveType, monk);

            int damageAmount = Random.Shared.Roll(6, diceAmount);

            if (hasImprovedEvasion || savingThrowResult == SavingThrowResult.Success)
                damageAmount /= 2;

            Effect damageEffect = Effect.LinkEffects(Effect.Damage(damageAmount, elementalType), elementalDamageVfx);

            if (savingThrowResult == SavingThrowResult.Failure)
            {
                creatureInShape.ApplyEffect(EffectDuration.Instant, damageEffect);
                continue;
            }

            if (hasEvasion || hasImprovedEvasion)
            {
                creatureInShape.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpReflexSaveThrowUse));
                continue;
            }

            creatureInShape.ApplyEffect(EffectDuration.Instant, damageEffect);
            creatureInShape.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpReflexSaveThrowUse));
        }
    }

    /// <summary>
    ///     Axiomatic Strike deals +1 bonus elemental damage to the target, with an additional +1 for every Ki Focus,
    ///     to a maximum of +4 elemental damage.
    /// </summary>
    private static void AugmentAxiomaticStrike(OnCreatureAttack attackData)
    {
        AxiomaticStrike.DoAxiomaticStrike(attackData);

        NwCreature monk = attackData.Attacker;
        DamageType elementalType = MonkUtils.GetElementalType(monk);
        DamageData<short> damageData = attackData.DamageData;
        short elementalDamage = damageData.GetDamageByType(elementalType);
        short bonusDamageElemental = MonkUtils.GetKiFocus(monk) switch
        {
            KiFocus.KiFocus1 => 2,
            KiFocus.KiFocus2 => 3,
            KiFocus.KiFocus3 => 4,
            _ => 1
        };

        elementalDamage += bonusDamageElemental;
        damageData.SetDamageByType(elementalType, elementalDamage);
    }

    /// <summary>
    ///     Wholeness of Body deals 2d6 elemental damage in a large area round the monk, with a successful reflex save
    ///     halving the damage. Each Ki Focus adds 2d6 damage to a maximum of 8d6 elemental damage.
    /// </summary>
    private static void AugmentWholenessOfBody(OnSpellCast castData)
    {
        WholenessOfBody.DoWholenessOfBody(castData);

        NwCreature monk = (NwCreature)castData.Caster;
        DamageType elementalType = MonkUtils.GetElementalType(monk);
        int dc = MonkUtils.CalculateMonkDc(monk);
        int diceAmount = MonkUtils.GetKiFocus(monk) switch
        {
            KiFocus.KiFocus1 => 4,
            KiFocus.KiFocus2 => 6,
            KiFocus.KiFocus3 => 8,
            _ => 2
        };
        Effect elementalAoeVfx = elementalType switch
        {
            DamageType.Fire => MonkUtils.ResizedVfx(VfxType.FnfFirestorm, RadiusSize.Large),
            DamageType.Cold => MonkUtils.ResizedVfx(VfxType.ImpFrostL, RadiusSize.Large),
            DamageType.Electrical => MonkUtils.ResizedVfx(VfxType.FnfElectricExplosion, RadiusSize.Large),
            DamageType.Acid => MonkUtils.ResizedVfx(VfxType.ImpAcidS, RadiusSize.Large),
            _ => MonkUtils.ResizedVfx(VfxType.FnfFireball, RadiusSize.Large)
        };
        Effect elementalDamageVfx = elementalType switch
        {
            DamageType.Fire => Effect.VisualEffect(VfxType.ImpFlameS),
            DamageType.Cold => Effect.VisualEffect(VfxType.ImpFrostS),
            DamageType.Electrical => Effect.VisualEffect(VfxType.ComHitElectrical),
            DamageType.Acid => Effect.VisualEffect(VfxType.ImpAcidS),
            _ => Effect.VisualEffect(VfxType.ImpFlameS)
        };
        SavingThrowType elementalSaveType = elementalType switch
        {
            DamageType.Fire => SavingThrowType.Fire,
            DamageType.Cold => SavingThrowType.Cold,
            DamageType.Electrical => SavingThrowType.Electricity,
            DamageType.Acid => SavingThrowType.Acid,
            _ => SavingThrowType.Fire
        };

        monk.ApplyEffect(EffectDuration.Instant, elementalAoeVfx);
        foreach (NwGameObject nwObject in monk.Location!.GetObjectsInShape(Shape.Sphere, RadiusSize.Large, true,
                     ObjectTypes.Creature | ObjectTypes.Door | ObjectTypes.Placeable))
        {
            NwCreature creatureInShape = (NwCreature)nwObject;
            if (monk.IsReactionTypeFriendly(creatureInShape)) continue;

            CreatureEvents.OnSpellCastAt.Signal(monk, creatureInShape, NwSpell.FromSpellType(Spell.Fireball)!);

            bool hasEvasion = creatureInShape.KnowsFeat(NwFeat.FromFeatType(Feat.Evasion)!);
            bool hasImprovedEvasion = creatureInShape.KnowsFeat(NwFeat.FromFeatType(Feat.ImprovedEvasion)!);

            SavingThrowResult savingThrowResult =
                creatureInShape.RollSavingThrow(SavingThrow.Reflex, dc, elementalSaveType, monk);

            int damageAmount = Random.Shared.Roll(6, diceAmount);

            if (hasImprovedEvasion || savingThrowResult == SavingThrowResult.Success)
                damageAmount /= 2;

            Effect damageEffect = Effect.LinkEffects(Effect.Damage(damageAmount, elementalType), elementalDamageVfx);

            if (savingThrowResult == SavingThrowResult.Failure)
            {
                creatureInShape.ApplyEffect(EffectDuration.Instant, damageEffect);
                continue;
            }

            if (hasEvasion || hasImprovedEvasion)
            {
                creatureInShape.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpReflexSaveThrowUse));
                continue;
            }

            creatureInShape.ApplyEffect(EffectDuration.Instant, damageEffect);
            creatureInShape.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpReflexSaveThrowUse));
        }
    }

    /// <summary>
    ///     Ki Shout changes the damage from sonic to the chosen element. In addition, all enemies receive 10% vulnerability
    ///     to the element for three rounds, with every Ki Focus increasing it by 10%, to a maximum of 40% elemental damage
    ///     vulnerability.
    /// </summary>
    private static void AugmentKiShout(OnSpellCast castData)
    {
        NwCreature monk = (NwCreature)castData.Caster;
        DamageType elementalType = MonkUtils.GetElementalType(monk);
        VfxType elementalDamageVfx = elementalType switch
        {
            DamageType.Fire => VfxType.ImpFlameS,
            DamageType.Cold => VfxType.ImpFrostS,
            DamageType.Electrical => VfxType.ComHitElectrical,
            DamageType.Acid => VfxType.ImpAcidS,
            _ => VfxType.ImpFlameS
        };

        KiShout.DoKiShout(castData, elementalType, elementalDamageVfx);

        int vulnerabilityPct = MonkUtils.GetKiFocus(monk) switch
        {
            KiFocus.KiFocus1 => 20,
            KiFocus.KiFocus2 => 30,
            KiFocus.KiFocus3 => 40,
            _ => 10
        };
        VfxType elementalVfx = elementalType switch
        {
            DamageType.Fire => VfxType.DurAuraPulseOrangeBlack,
            DamageType.Cold => VfxType.DurAuraPulseCyanBlack,
            DamageType.Electrical => VfxType.DurAuraPulseBlueBlack,
            DamageType.Acid => VfxType.DurAuraPulseGreenBlack,
            _ => VfxType.DurAuraPulseOrangeBlack
        };


        // Elemental effect
        Effect elementalEffect = Effect.LinkEffects(Effect.DamageImmunityDecrease(elementalType, vulnerabilityPct),
            Effect.VisualEffect(elementalVfx));

        foreach (NwGameObject nwObject in monk.Location!.GetObjectsInShape(Shape.Sphere, RadiusSize.Colossal, false))
        {
            NwCreature creatureInShape = (NwCreature)nwObject;
            if (!monk.IsReactionTypeHostile(creatureInShape)) continue;

            creatureInShape.ApplyEffect(EffectDuration.Temporary, elementalEffect, NwTimeSpan.FromRounds(3));
        }
    }
}
