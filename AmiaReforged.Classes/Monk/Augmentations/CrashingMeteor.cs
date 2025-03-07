using AmiaReforged.Classes.Monk.Constants;
using AmiaReforged.Classes.Monk.Techniques.Body;
using AmiaReforged.Classes.Monk.Techniques.Martial;
using AmiaReforged.Classes.Monk.Techniques.Spirit;
using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.Classes.Monk.Augmentations;

public static class CrashingMeteor
{
    public static void ApplyAugmentations(TechniqueType technique, OnSpellCast? castData = null, OnSpellAction? 
            wholenessData = null, OnCreatureAttack? attackData = null)
    {
        switch (technique)
        {
            case TechniqueType.Stunning:
                AugmentStunning(attackData);
                break;
            case TechniqueType.Axiomatic:
                AugmentAxiomatic(attackData);
                break;
            case TechniqueType.KiShout:
                AugmentKiShout(castData);
                break;
            case TechniqueType.Wholeness:
                AugmentWholeness(wholenessData);
                break;
            case TechniqueType.KiBarrier:
                KiBarrier.DoKiBarrier(castData);
                break;
            case TechniqueType.Eagle:
                EagleStrike.DoEagleStrike(attackData);
                break;
            case TechniqueType.EmptyBody:
                EmptyBody.DoEmptyBody(castData);
                break;
            case TechniqueType.Quivering:
                QuiveringPalm.DoQuiveringPalm(castData);
                break;
        }
    }

    /// <summary>
    ///     Stunning Strike deals 2d6 elemental damage in a medium area around the target. The damage isn’t multiplied by
    ///     critical hits and a successful reflex save halves the damage. Each Ki Focus adds 2d6 to a maximum of 8d6 elemental
    ///     damage.
    /// </summary>
    private static void AugmentStunning(OnCreatureAttack attackData)
    {
        StunningStrike.DoStunningStrike(attackData);

        NwCreature monk = attackData.Attacker;
        DamageType elementalType = MonkUtilFunctions.GetElementalType(monk);
        int monkLevel = monk.GetClassInfo(ClassType.Monk)!.Level;
        int dc = MonkUtilFunctions.CalculateMonkDc(monk);
        int diceAmount = monkLevel switch
        {
            >= MonkLevel.KiFocusI and < MonkLevel.KiFocusIi => 4,
            >= MonkLevel.KiFocusIi and < MonkLevel.KiFocusIii => 6,
            MonkLevel.KiFocusIii => 8,
            _ => 2
        };
        Effect elementalAoeVfx = elementalType switch
        {
            DamageType.Fire => MonkUtilFunctions.ResizedVfx(VfxType.FnfFireball, RadiusSize.Medium),
            DamageType.Cold => MonkUtilFunctions.ResizedVfx(VfxType.ImpFrostL, RadiusSize.Medium),
            DamageType.Electrical => MonkUtilFunctions.ResizedVfx(VfxType.FnfElectricExplosion, RadiusSize.Medium),
            DamageType.Acid => MonkUtilFunctions.ResizedVfx(VfxType.ImpAcidS, RadiusSize.Medium),
            _ => MonkUtilFunctions.ResizedVfx(VfxType.FnfFireball, RadiusSize.Medium)
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
        foreach (NwGameObject nwObject in monk.Location!.GetObjectsInShape(Shape.Sphere, RadiusSize.Medium, true,
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
    private static void AugmentAxiomatic(OnCreatureAttack attackData)
    {
        AxiomaticStrike.DoAxiomaticStrike(attackData);

        NwCreature monk = attackData.Attacker;
        DamageType elementalType = MonkUtilFunctions.GetElementalType(monk);
        DamageData<short> damageData = attackData.DamageData;
        int monkLevel = monk.GetClassInfo(ClassType.Monk)!.Level;
        short elementalDamage = damageData.GetDamageByType(elementalType);
        short bonusDamageElemental = monkLevel switch
        {
            >= MonkLevel.KiFocusI and < MonkLevel.KiFocusIi => 2,
            >= MonkLevel.KiFocusIi and < MonkLevel.KiFocusIii => 3,
            MonkLevel.KiFocusIii => 4,
            _ => 1
        };

        elementalDamage += bonusDamageElemental;
        damageData.SetDamageByType(elementalType, elementalDamage);
    }

    /// <summary>
    ///     Wholeness of Body deals 2d6 elemental damage in a medium area round the monk, with a successful reflex save
    ///     halving the damage. Each Ki Focus adds 2d6 damage to a maximum of 8d6 elemental damage.
    /// </summary>
    private static void AugmentWholeness(OnSpellAction wholenessData)
    {
        WholenessOfBody.DoWholenessOfBody(wholenessData);

        NwCreature monk = wholenessData.Caster;
        DamageType elementalType = MonkUtilFunctions.GetElementalType(monk);
        int monkLevel = monk.GetClassInfo(ClassType.Monk)!.Level;
        int dc = MonkUtilFunctions.CalculateMonkDc(monk);
        int diceAmount = monkLevel switch
        {
            >= MonkLevel.KiFocusI and < MonkLevel.KiFocusIi => 4,
            >= MonkLevel.KiFocusIi and < MonkLevel.KiFocusIii => 6,
            MonkLevel.KiFocusIii => 8,
            _ => 2
        };
        Effect elementalAoeVfx = elementalType switch
        {
            DamageType.Fire => MonkUtilFunctions.ResizedVfx(VfxType.FnfFireball, RadiusSize.Medium),
            DamageType.Cold => MonkUtilFunctions.ResizedVfx(VfxType.ImpFrostL, RadiusSize.Medium),
            DamageType.Electrical => MonkUtilFunctions.ResizedVfx(VfxType.FnfElectricExplosion, RadiusSize.Medium),
            DamageType.Acid => MonkUtilFunctions.ResizedVfx(VfxType.ImpAcidS, RadiusSize.Medium),
            _ => MonkUtilFunctions.ResizedVfx(VfxType.FnfFireball, RadiusSize.Medium)
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
        foreach (NwGameObject nwObject in monk.Location!.GetObjectsInShape(Shape.Sphere, RadiusSize.Medium, true,
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
        DamageType elementalType = MonkUtilFunctions.GetElementalType(monk);
        int monkLevel = monk.GetClassInfo(ClassType.Monk)!.Level;
        int dc = MonkUtilFunctions.CalculateMonkDc(monk);
        int vulnerabilityPct = monkLevel switch
        {
            >= MonkLevel.KiFocusI and < MonkLevel.KiFocusIi => 20,
            >= MonkLevel.KiFocusIi and < MonkLevel.KiFocusIii => 30,
            MonkLevel.KiFocusIii => 40,
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
        VfxType elementalDamageVfx = elementalType switch
        {
            DamageType.Fire => VfxType.ImpFlameS,
            DamageType.Cold => VfxType.ImpFrostS,
            DamageType.Electrical => VfxType.ComHitElectrical,
            DamageType.Acid => VfxType.ImpAcidS,
            _ => VfxType.ImpFlameS
        };

        // Regular ki shout effect
        Effect kiShoutVfx = Effect.VisualEffect(VfxType.FnfHowlMind);
        Effect kiShoutEffect = Effect.Stunned();
        kiShoutEffect.SubType = EffectSubType.Supernatural;
        TimeSpan effectDuration = NwTimeSpan.FromRounds(3);

        // Elemental effect
        Effect elementalEffect = Effect.LinkEffects(Effect.DamageImmunityDecrease(elementalType, vulnerabilityPct),
            Effect.VisualEffect(elementalVfx));
        elementalEffect.SubType = EffectSubType.Supernatural;

        monk.ApplyEffect(EffectDuration.Instant, kiShoutVfx);
        foreach (NwGameObject nwObject in monk.Location!.GetObjectsInShape(Shape.Sphere, RadiusSize.Colossal, false))
        {
            NwCreature creatureInShape = (NwCreature)nwObject;
            if (!monk.IsReactionTypeHostile(creatureInShape)) continue;

            CreatureEvents.OnSpellCastAt.Signal(monk, creatureInShape, NwSpell.FromSpellType(Spell.AbilityHowlSonic)!);

            int damageAmount = Random.Shared.Roll(4, monkLevel);
            Effect damageEffect = Effect.LinkEffects(Effect.Damage(damageAmount, elementalType),
                Effect.VisualEffect(elementalDamageVfx));

            creatureInShape.ApplyEffect(EffectDuration.Temporary, elementalEffect, effectDuration);
            creatureInShape.ApplyEffect(EffectDuration.Instant, damageEffect);

            SavingThrowResult savingThrowResult =
                creatureInShape.RollSavingThrow(SavingThrow.Will, dc, SavingThrowType.MindSpells, monk);

            if (savingThrowResult is SavingThrowResult.Failure)
            {
                creatureInShape.ApplyEffect(EffectDuration.Temporary, kiShoutEffect, effectDuration);
                continue;
            }

            creatureInShape.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpWillSavingThrowUse));
        }
    }
}