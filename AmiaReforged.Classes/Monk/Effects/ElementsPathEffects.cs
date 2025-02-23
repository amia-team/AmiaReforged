using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.Classes.Monk.Effects;

public static class ElementsPathEffects
{
    public static void ApplyElementsPathEffects(TechniqueType technique, OnSpellCast? castData = null, OnCreatureAttack? attackData = null)
    {
        switch (technique)
        {
            case TechniqueType.Stunning : ApplyEffectsToStunning(attackData);
                break;
            case TechniqueType.Quivering : ApplyEffectsToQuivering(castData);
                break;
            case TechniqueType.Axiomatic : ApplyEffectsToAxiomatic(attackData);
                break;
            case TechniqueType.KiBarrier : ApplyEffectsToKiBarrier(castData);
                break;
            case TechniqueType.KiShout : ApplyEffectsToKiShout(castData);
                break;
                
        }
    }
    private static void ApplyEffectsToStunning(OnCreatureAttack attackData)
    {
    }
    private static void ApplyEffectsToQuivering(OnSpellCast castData)
    {
    }
    private static void ApplyEffectsToAxiomatic(OnCreatureAttack attackData)
    {
        NwCreature monk = attackData.Attacker;
        DamageType elementalType = MonkUtilFunctions.GetElementalType(monk);
        DamageData<short> damageData = attackData.DamageData;
        int monkLevel = monk.GetClassInfo(ClassType.Monk)!.Level;
        short elementalDamage = damageData.GetDamageByType(elementalType);
        short bludgeoningDamage = damageData.GetDamageByType(DamageType.Bludgeoning); 
        short bonusDamageElemental = monkLevel switch
        {
            >= 10 and <= 19 => 2,
            >= 20 and <= 29 => 3,
            30 => 4,
            _ => 1
        };
        short bonusDamageAxiomatic = monkLevel switch
        {
            >= 10 and <= 19 => 2,
            >= 20 and <= 29 => 3,
            30 => 4,
            _ => 1
        };
        
        // Apply elemental and axiomatic damage
        elementalDamage += bonusDamageElemental;
        damageData.SetDamageByType(elementalType, elementalDamage);
        bludgeoningDamage += bonusDamageAxiomatic;
        damageData.SetDamageByType(DamageType.Bludgeoning, bludgeoningDamage);
    }
    private static void ApplyEffectsToKiBarrier(OnSpellCast castData)
    {
        NwCreature monk = (NwCreature)castData.Caster;
        DamageType elementalType = MonkUtilFunctions.GetElementalType(monk);
        int monkLevel = monk.GetClassInfo(ClassType.Monk)!.Level;
        int damageReductionAmount = 5;
        int totalAbsorb = monkLevel / 2 * 10;

        Effect kiBarrierEffect = Effect.LinkEffects(Effect.DamageReduction(damageReductionAmount, DamagePower.Plus20, totalAbsorb),
            Effect.DamageResistance(elementalType, 10), Effect.VisualEffect(VfxType.DurCessatePositive));
        kiBarrierEffect.SubType = EffectSubType.Supernatural;
        Effect kiBarrierVfx = elementalType switch
        {
            DamageType.Fire => Effect.VisualEffect(VfxType.ImpFlameM),
            DamageType.Cold => Effect.VisualEffect(VfxType.ImpFrostL),
            DamageType.Electrical => Effect.VisualEffect(VfxType.FnfElectricExplosion, false, 0.3f),
            DamageType.Acid => Effect.VisualEffect(VfxType.FnfGasExplosionAcid),
            _ => Effect.VisualEffect(VfxType.ImpFlameM)
        };
        TimeSpan effectDuration = NwTimeSpan.FromTurns(monkLevel);

        monk.ApplyEffect(EffectDuration.Temporary, kiBarrierEffect, effectDuration);
        monk.ApplyEffect(EffectDuration.Instant, kiBarrierVfx);
    }
    private static void ApplyEffectsToKiShout(OnSpellCast castData)
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