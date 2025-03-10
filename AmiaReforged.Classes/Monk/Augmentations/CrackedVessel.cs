using AmiaReforged.Classes.Monk.Constants;
using AmiaReforged.Classes.Monk.Techniques.Body;
using AmiaReforged.Classes.Monk.Techniques.Martial;
using AmiaReforged.Classes.Monk.Techniques.Spirit;
using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;
using NLog.Targets;

namespace AmiaReforged.Classes.Monk.Augmentations;

public static class CrackedVessel
{
    public static void ApplyAugmentations(TechniqueType technique, OnSpellCast? castData = null, OnCreatureAttack? attackData = null)
    {
        switch (technique)
        {
            case TechniqueType.Axiomatic:
                AugmentAxiomatic(attackData);
                break;
            case TechniqueType.Quivering:
                AugmentQuivering(castData);
                break;
            case TechniqueType.Wholeness:
                AugmentWholeness(castData);
                break;
            case TechniqueType.EmptyBody:
                AugmentEmptyBody(castData);
                break;
            case TechniqueType.Stunning:
                StunningStrike.DoStunningStrike(attackData);
                break;
            case TechniqueType.Eagle:
                EagleStrike.DoEagleStrike(attackData);
                break;
            case TechniqueType.KiBarrier:
                KiBarrier.DoKiBarrier(castData);
                break;
            case TechniqueType.KiShout:
                KiShout.DoKiShout(castData);
                break;
        }
    }

    /// <summary>
    /// Axiomatic Strike deals 1d2 bonus negative energy damage when the monk is injured, 1d4 when badly wounded,
    /// and 1d6 when near death. In addition, every three killing blows made with this attack against living enemies
    /// regenerates a Body Ki Point. Each Ki Focus multiplies the damage bonus,
    /// to a maximum of 4d2, 4d4, and 4d6 bonus negative energy damage.
    /// </summary>
    private static void AugmentAxiomatic(OnCreatureAttack attackData)
    {
        AxiomaticStrike.DoAxiomaticStrike(attackData);

        if (attackData.Target is not NwCreature) return;

        NwCreature monk = attackData.Attacker;
        int monkLevel = monk.GetClassInfo(ClassType.Monk)!.Level;
        int damageDice = monkLevel switch
        {
            >= MonkLevel.KiFocusI and < MonkLevel.KiFocusIi => 2,
            >= MonkLevel.KiFocusIi and < MonkLevel.KiFocusIii => 3,
            MonkLevel.KiFocusIii => 4,
            _ => 1
        };
        int damageSides = IsInjured(monk) ? 2 : IsBadlyWounded(monk) ? 4 : IsNearDeath(monk) ? 6 : 0;
        int damageAmount = Random.Shared.Roll(damageSides, damageDice);
        
        DamageData<short> damageData = attackData.DamageData;
        short negativeDamage = damageData.GetDamageByType(DamageType.Negative);
        
        negativeDamage += (short)damageAmount;
        damageData.SetDamageByType(DamageType.Negative, negativeDamage);
        
        if (monkLevel < MonkLevel.BodyKiPointsI || !attackData.KillingBlow) return;
        
        LocalVariableInt killCounter = monk.GetObjectVariable<LocalVariableInt>("crackedvessel_killcounter");
        killCounter.Value++;

        if (killCounter.Value < 3) return;
            
        monk.IncrementRemainingFeatUses(NwFeat.FromFeatId(MonkFeat.BodyKiPoint)!);
        killCounter.Delete();
    }
    
    /// <summary>
    /// Wholeness of Body unleashes 2d6 negative energy and physical damage in a large radius when the monk is injured,
    /// 2d8 when badly wounded, and 2d10 when near death. Fortitude saving throw halves the damage. Each Ki Focus
    /// multiplies the damage bonus, to a maximum of 8d6, 8d8, and 8d10 negative energy and physical damage.
    /// </summary>
    private static void AugmentWholeness(OnSpellCast castData)
    {
        WholenessOfBody.DoWholenessOfBody(castData);

        NwCreature monk = (NwCreature)castData.Caster;
        int monkLevel = monk.GetClassInfo(ClassType.Monk)!.Level;
        int dc = MonkUtilFunctions.CalculateMonkDc(monk);
        int damageDice = monkLevel switch
        {
            >= MonkLevel.KiFocusI and < MonkLevel.KiFocusIi => 4,
            >= MonkLevel.KiFocusIi and < MonkLevel.KiFocusIii => 6,
            MonkLevel.KiFocusIii => 8,
            _ => 2
        };
        int damageSides = IsInjured(monk) ? 6 : IsBadlyWounded(monk) ? 8 : IsNearDeath(monk) ? 10 : 0;
        Effect aoeVfx = MonkUtilFunctions.ResizedVfx(VfxType.FnfLosEvil30, RadiusSize.Large);
        
        monk.ApplyEffect(EffectDuration.Instant, aoeVfx);
        foreach (NwGameObject nwObject in monk.Location!.GetObjectsInShape(Shape.Sphere, RadiusSize.Large, false))
        {
            NwCreature creatureInShape = (NwCreature)nwObject;
            if (!monk.IsReactionTypeHostile(creatureInShape)) continue;
            
            CreatureEvents.OnSpellCastAt.Signal(monk, creatureInShape, NwSpell.FromSpellType(Spell.Fireball)!);
            
            SavingThrowResult savingThrowResult =
                creatureInShape.RollSavingThrow(SavingThrow.Fortitude, dc, SavingThrowType.Negative, monk);

            int damageAmount = Random.Shared.Roll(damageSides, damageDice);

            if (savingThrowResult == SavingThrowResult.Success)
            {
                damageAmount /= 2;
                creatureInShape.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpFortitudeSavingThrowUse));
            }

            Effect wholenessEffect = Effect.LinkEffects(Effect.Damage(damageAmount, DamageType.Negative),
                Effect.Damage(damageAmount, DamageType.Piercing), Effect.VisualEffect(VfxType.ImpNegativeEnergy));
            
            creatureInShape.ApplyEffect(EffectDuration.Instant, wholenessEffect);
        }
    }
    
    /// <summary>
    /// Empty Body grants 5% physical damage immunity when the monk is injured, 10% when badly wounded, and 15% when
    /// near death. This effect is only granted while the monk is in combat. Each Ki Focus grants 5% more physical
    /// damage immunity, to a maximum of 20%, 25%, and 30% physical damage immunity.
    /// </summary>
    private static void AugmentEmptyBody(OnSpellCast castData)
    {
        EmptyBody.DoEmptyBody(castData);

        NwCreature monk = (NwCreature)castData.Caster;

        if (!monk.IsInCombat) return;
        
        int monkLevel = monk.GetClassInfo(ClassType.Monk)!.Level;
        int dc = MonkUtilFunctions.CalculateMonkDc(monk);
        int pctImmunityBonus = monkLevel switch
        {
            >= MonkLevel.KiFocusI and < MonkLevel.KiFocusIi => 5,
            >= MonkLevel.KiFocusIi and < MonkLevel.KiFocusIii => 10,
            MonkLevel.KiFocusIii => 15,
            _ => 0
        };
        int pctImmunityBase = IsInjured(monk) ? 5 : IsBadlyWounded(monk) ? 10 : IsNearDeath(monk) ? 15 : 0;
        int pctImmunityTotal = pctImmunityBase + pctImmunityBonus;
        TimeSpan effectDuration = NwTimeSpan.FromRounds(monkLevel);
        Effect emptyBodyEffect = Effect.LinkEffects(Effect.DamageIncrease(pctImmunityTotal, DamageType.Piercing),
            Effect.DamageIncrease(pctImmunityTotal, DamageType.Slashing),
            Effect.DamageIncrease(pctImmunityTotal, DamageType.Bludgeoning));
        emptyBodyEffect.Tag = "crackedvessel_emptybody";
        

        foreach (Effect effect in monk.ActiveEffects)
            if (effect.Tag == "crackedvessel_emptybody") 
                monk.RemoveEffect(effect);
        
        monk.ApplyEffect(EffectDuration.Temporary, emptyBodyEffect,  effectDuration);
    }
    
    /// <summary>
    /// Quivering Palm inflicts 5% negative energy and physical damage vulnerability for three rounds.
    /// Each Ki Focus adds 5% to a maximum of 20%.
    /// </summary>
    private static void AugmentQuivering(OnSpellCast castData)
    {
        
    }
    
    
    private static bool IsInjured(NwCreature monk)
    {
        return monk.HP < monk.MaxHP * 0.75;
    }

    private static bool IsBadlyWounded(NwCreature monk)
    {
        return monk.HP < monk.MaxHP * 0.50;
    }
    
    private static bool IsNearDeath(NwCreature monk)
    {
        return monk.HP < monk.MaxHP * 0.25;
    }
}