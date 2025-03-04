using AmiaReforged.Classes.Monk.Constants;
using AmiaReforged.Classes.Monk.Techniques.Body;
using AmiaReforged.Classes.Monk.Techniques.Martial;
using AmiaReforged.Classes.Monk.Techniques.Spirit;
using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;
using NLog.Targets;

namespace AmiaReforged.Classes.Monk.Augmentations;

public static class CrashingMeteor
{
    public static void ApplyAugmentations(TechniqueType technique, OnSpellCast? castData = null, OnCreatureAttack? attackData = null)
    {
        switch (technique)
        {
            case TechniqueType.Stunning : AugmentStunning(attackData);
                break;
            case TechniqueType.Axiomatic : AugmentAxiomatic(attackData);
                break;
            case TechniqueType.KiShout : AugmentKiShout(castData);
                break;
            case TechniqueType.Wholeness: AugmentWholeness(castData);
                break;
            case TechniqueType.KiBarrier: KiBarrier.DoKiBarrier(castData);
                break;
            case TechniqueType.Eagle: EagleStrike.DoEagleStrike(attackData);
                break;
            case TechniqueType.EmptyBody: EmptyBody.DoEmptyBody(castData);
                break;
            case TechniqueType.Quivering : QuiveringPalm.DoQuiveringPalm(castData);
                break;
        }
    }
    /// <summary>
    /// Stunning Strike deals 2d6 elemental damage in a medium area around the target. Every Ki Focus increases the
    /// damage by an additional 2d6 to a maximum of 8d6 elemental damage. The damage isn’t multiplied by critical hits
    /// and a successful reflex save halves the damage.
    /// </summary>
    private static void AugmentStunning(OnCreatureAttack attackData)
    {
        StunningStrike.DoStunningStrike(attackData);

        NwCreature monk = attackData.Attacker;
        DamageType elementalType = MonkUtilFunctions.GetElementalType(monk);
        int monkLevel  = monk.GetClassInfo(ClassType.Monk)!.Level;
        int dc = MonkUtilFunctions.CalculateMonkDc(monk);
        int diceAmount = monkLevel switch
        {
            >= MonkLevel.KiFocusI and < MonkLevel.KiFocusII => 4,
            >= MonkLevel.KiFocusII and < MonkLevel.KiFocusIII => 6,
            MonkLevel.KiFocusIII => 8,
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

    private static void AugmentAxiomatic(OnCreatureAttack attackData)
    {
        // First do Axiomatic, then add the path stuff
        AxiomaticStrike.DoAxiomaticStrike(attackData);
        
        NwCreature monk = attackData.Attacker;
        DamageType elementalType = MonkUtilFunctions.GetElementalType(monk);
        DamageData<short> damageData = attackData.DamageData;
        int monkLevel = monk.GetClassInfo(ClassType.Monk)!.Level;
        short elementalDamage = damageData.GetDamageByType(elementalType);
        short bonusDamageElemental = monkLevel switch
        {
            >= MonkLevel.PathOfEnlightenment and <= MonkLevel.KiFocusI => 1,
            >= MonkLevel.KiFocusI and <= MonkLevel.KiFocusII => 2,
            30 => 4,
            _ => 1
        };
        // Apply elemental and axiomatic damage
        elementalDamage += bonusDamageElemental;
        damageData.SetDamageByType(elementalType, elementalDamage);
    }
    private static void AugmentWholeness(OnSpellCast castData)
    {
        
    }
    private static void AugmentKiShout(OnSpellCast castData)
    {
        NwCreature monk = (NwCreature)castData.Caster;
        DamageType elementalType = MonkUtilFunctions.GetElementalType(monk);
        VfxType elementalVfx = elementalType switch
        {
            DamageType.Fire => VfxType.DurAuraPulseOrangeBlack,
            DamageType.Cold => VfxType.DurAuraPulseCyanBlack,
            DamageType.Electrical => VfxType.DurAuraPulseGreyBlack,
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

        int monkLevel = monk.GetClassInfo(ClassType.Monk)!.Level;
        int dc = MonkUtilFunctions.CalculateMonkDc(monk);

        // Regular ki shout effect
        Effect kiShoutEffect = Effect.LinkEffects(Effect.Stunned(), Effect.VisualEffect(VfxType.DurCessateNegative));
        TimeSpan effectDuration = NwTimeSpan.FromRounds(3);

        // elements path effect
        Effect elementsEffect = Effect.LinkEffects(Effect.DamageImmunityDecrease(elementalType, 20),
            Effect.VisualEffect(elementalVfx), Effect.VisualEffect(VfxType.DurCessateNegative));
        kiShoutEffect.SubType = EffectSubType.Supernatural;
        Effect kiShoutVfx = MonkUtilFunctions.ResizedVfx(VfxType.FnfHowlMind, RadiusSize.Large);

        monk.ApplyEffect(EffectDuration.Instant, kiShoutVfx);
        foreach (NwGameObject nwObject in monk.Location!.GetObjectsInShape(Shape.Sphere, RadiusSize.Large, false))
        {
            NwCreature creatureInShape = (NwCreature)nwObject;
            if (!monk.IsReactionTypeHostile(creatureInShape)) continue;

            CreatureEvents.OnSpellCastAt.Signal(monk, creatureInShape, NwSpell.FromSpellType(Spell.AbilityQuiveringPalm)!);

            int damageAmount = Random.Shared.Roll(4, monkLevel);
            Effect damageEffect = Effect.LinkEffects(Effect.Damage(damageAmount, elementalType), 
                Effect.VisualEffect(elementalDamageVfx));

            creatureInShape.ApplyEffect(EffectDuration.Temporary, elementsEffect, effectDuration);
            creatureInShape.ApplyEffect(EffectDuration.Instant, damageEffect);

            SavingThrowResult savingThrowResult = 
                creatureInShape.RollSavingThrow(SavingThrow.Will, dc, SavingThrowType.MindSpells, monk);
            
            if (savingThrowResult is SavingThrowResult.Success)
                creatureInShape.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpWillSavingThrowUse));
            
            if (savingThrowResult is SavingThrowResult.Failure)
                creatureInShape.ApplyEffect(EffectDuration.Temporary, kiShoutEffect, effectDuration);
        
        }
    }
}