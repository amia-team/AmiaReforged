using AmiaReforged.Classes.Monk.Constants;
using AmiaReforged.Classes.Monk.Techniques.Body;
using AmiaReforged.Classes.Monk.Techniques.Martial;
using AmiaReforged.Classes.Monk.Techniques.Spirit;
using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.Classes.Monk.Augmentations;

public static class SwingingCenser
{
    public const string HealingCounter = "censor_healing_counter";
    public static void ApplyAugmentations(TechniqueType technique, OnSpellCast? castData = null,
        OnCreatureAttack? attackData = null)
    {
        switch (technique)
        {
            case TechniqueType.Stunning:
                AugmentStunning(attackData);
                break;
            case TechniqueType.Wholeness:
                AugmentWholeness(castData);
                break;
            case TechniqueType.EmptyBody:
                AugmentEmptyBody(castData);
                break;
            case TechniqueType.KiShout:
                AugmentKiShout(castData);
                break;
            case TechniqueType.Eagle:
                EagleStrike.DoEagleStrike(attackData);
                break;
            case TechniqueType.Axiomatic:
                AxiomaticStrike.DoAxiomaticStrike(attackData);
                break;
            case TechniqueType.KiBarrier:
                KiBarrier.DoKiBarrier(castData);
                break;
            case TechniqueType.Quivering:
                QuiveringPalm.DoQuiveringPalm(castData);
                break;
            default:
                return;
        }
    }

    private static void AugmentStunning(OnCreatureAttack attackData)
    {
        StunningStrike.DoStunningStrike(attackData);
        NwCreature monk = attackData.Attacker;
        int monkLevel = monk.GetClassInfo(ClassType.Monk)!.Level;
        int monkHealth = monk.HP;
        int monkMaxHP = monk.MaxHP;
        var rand = new Random();

        int diceHealing = monkLevel switch
        {
            >= MonkLevel.KiFocusI and < MonkLevel.KiFocusIi => 2,
            >= MonkLevel.KiFocusIi and < MonkLevel.KiFocusIii => 3,
            MonkLevel.KiFocusIii => 4,
            _ => 1
        };
        
        // Roll 1d6s
        int randomRoll = rand.Roll(6, diceHealing);
        int healRemaining = randomRoll;
        Effect healVfx = Effect.VisualEffect(VfxType.ImpHealingS, false, 0.7f);
        // Get healing  counter
        LocalVariableInt healCounter = monk.GetObjectVariable<LocalVariableInt>(HealingCounter);
        int hpDiff;
        // Check to make sure they are injured
        if (monkMaxHP > monkHealth)
        {
            hpDiff = monkMaxHP - monkHealth;
            // Use all your heal on the monk
            if (hpDiff >= randomRoll)
            {
                monk.ApplyEffect(EffectDuration.Instant,Effect.Heal(randomRoll));
                monk.ApplyEffect(EffectDuration.Instant,healVfx);
                healCounter.Value = healCounter.Value + randomRoll;
            }
            else if (hpDiff < randomRoll)
            {
                healRemaining = healRemaining - hpDiff;
                monk.ApplyEffect(EffectDuration.Instant,Effect.Heal(hpDiff));
                monk.ApplyEffect(EffectDuration.Instant,healVfx);
                healCounter.Value = healCounter.Value + hpDiff;
                HealAlly();
            }
        }
        else
        {
            HealAlly();
        }

        // Regenerate Ki Body Point
        if (healCounter.Value >= 100)
        {
            NwFeat bodyKiPointFeat = NwFeat.FromFeatId(MonkFeat.BodyKiPoint)!;
            int bodyUses = monk.GetFeatRemainingUses(bodyKiPointFeat);
            // Making sure they don't get more uses than their maximum
            if (((monkLevel >= MonkLevel.BodyKiPointsVi) && (bodyUses<6)) || ((monkLevel >= MonkLevel.BodyKiPointsV) && (bodyUses<5)) ||
                ((monkLevel >= MonkLevel.BodyKiPointsIv) && (bodyUses<4)) || ((monkLevel >= MonkLevel.BodyKiPointsIii) && (bodyUses<3)) || 
                ((monkLevel >= MonkLevel.BodyKiPointsIi) && (bodyUses<2)) || ((monkLevel >= MonkLevel.BodyKiPointsI) && (bodyUses<1)))
            {
                monk.SetFeatRemainingUses(bodyKiPointFeat,(byte)(bodyUses+1));
            }

            // Reset Local Variable
            healCounter.Delete(); 
        }
        
        return;
        
        // We will use the healRemaining variable for this
        void HealAlly()
        {
            int firstCreature=0;
            NwCreature lowestPercentHp = monk;
            
            foreach (NwGameObject nwObject in monk.Location!.GetObjectsInShape(Shape.Sphere, RadiusSize.Medium,
                         false))
            {
                NwCreature creatureInShape = (NwCreature)nwObject;

                if (!monk.IsReactionTypeFriendly(creatureInShape)) continue;

                if (firstCreature==0)
                {
                    lowestPercentHp = creatureInShape;
                    firstCreature = 1; 
                }
                else
                {   // Check their percent missing HP. Lowest is always set to the lowestPercentHp variable
                    if (((creatureInShape.MaxHP - creatureInShape.HP)/creatureInShape.MaxHP) > ((lowestPercentHp.MaxHP - lowestPercentHp.HP)/lowestPercentHp.MaxHP))
                    {
                        lowestPercentHp = creatureInShape;
                    }
                }
            }

            // If there are no friendlies then return
            if(lowestPercentHp == monk) return;
            
            int lowestHpDiff = lowestPercentHp.MaxHP - lowestPercentHp.HP;
            
            // Track how to apply the remaining heal
            if (lowestHpDiff >= healRemaining)
            { 
                lowestPercentHp.ApplyEffect(EffectDuration.Instant,Effect.Heal(healRemaining));
                lowestPercentHp.ApplyEffect(EffectDuration.Instant,healVfx);
                healCounter.Value = healCounter.Value + healRemaining;
            }
            else
            {
                lowestPercentHp.ApplyEffect(EffectDuration.Instant,Effect.Heal(lowestHpDiff));
                lowestPercentHp.ApplyEffect(EffectDuration.Instant,healVfx);
                healCounter.Value = healCounter.Value + lowestHpDiff;
            }
            
        }
        
    }

    private static void AugmentKiShout(OnSpellCast castData)
    {
    }

    private static void AugmentWholeness(OnSpellCast castData)
    {
        NwCreature monk = (NwCreature)castData.Caster;
        int monkLevel = monk.GetClassInfo(ClassType.Monk)!.Level;
        int pulseAmount = 1;
        int healAmount = monkLevel * 2;
        double level30Heal = healAmount * 1.5;

        // Wholeness is gained at 7
        if (monkLevel == MonkLevel.KiFocusIii)
        {
            pulseAmount = 3;
            healAmount = (int)level30Heal;
        }
        else if (monkLevel >= MonkLevel.KiFocusIi)
        {
            pulseAmount = 3;
        }
        else if (monkLevel >= MonkLevel.KiFocusI)
        {
            pulseAmount = 2;
        }
        else if (monkLevel >= MonkLevel.PathOfEnlightenment)
        {
            pulseAmount = 1;
        }

        Effect wholenessEffect = Effect.Heal(healAmount);
        Effect wholenessVfx = Effect.VisualEffect(VfxType.ImpHealingL, false, 0.7f);
        Effect wholenessLink = Effect.LinkEffects(wholenessEffect, wholenessVfx);
        Effect aoeVfx = Effect.VisualEffect(VfxType.ImpPulseHoly);


        WholenessPulse();
        return;

        async void WholenessPulse()
        {
            for (int i = 0; i < pulseAmount; i++) // Pulse amount
            {
                // If monk is dead or monk isn't valid, return
                if (monk.IsDead || !monk.IsValid) return;

                // AoE Holy Pulse fire at the base of the Monk  
                monk.ApplyEffect(EffectDuration.Instant, aoeVfx);

                foreach (NwGameObject nwObject in monk.Location!.GetObjectsInShape(Shape.Sphere, RadiusSize.Large,
                             false))
                {
                    NwCreature creatureInShape = (NwCreature)nwObject;

                    if (!monk.IsReactionTypeFriendly(creatureInShape)) continue;

                    creatureInShape.ApplyEffect(EffectDuration.Instant, wholenessLink);
                }

                await NwTask.Delay(TimeSpan.FromSeconds(3));
            }
        }
    }
    
    private static void AugmentEmptyBody(OnSpellCast castData)
    {
        NwCreature monk = (NwCreature)castData.Caster;
        int monkLevel = monk.GetClassInfo(ClassType.Monk)!.Level;
        int concealment = 50;
        TimeSpan regenTime = TimeSpan.FromSeconds(6);

        // Adjust as appropriate. 1 round per monk level.
        TimeSpan effectTime = TimeSpan.FromSeconds(monkLevel * 6);
        
        int regen = monkLevel switch
        {
            >= MonkLevel.KiFocusI and < MonkLevel.KiFocusIi => 4,
            >= MonkLevel.KiFocusIi and < MonkLevel.KiFocusIii => 6,
            MonkLevel.KiFocusIii => 8,
            _ => 2
        };

        Effect emptyBodyRegen = Effect.Regenerate(regen, regenTime);
        Effect emptyBodyConcealment = Effect.Concealment(concealment);
        // Stand in VFX, change as appropriate
        Effect emptyBodyVfx = Effect.VisualEffect(VfxType.DurBlur);
        Effect emptyLink = Effect.LinkEffects(emptyBodyConcealment, emptyBodyRegen, emptyBodyVfx);
        // Tag for later tracking
        emptyLink.Tag = "EmptyBody2";
        
        foreach (NwGameObject nwObject in monk.Location!.GetObjectsInShape(Shape.Sphere, RadiusSize.Large,
                     false))
        {
            NwCreature creatureInShape = (NwCreature)nwObject;

            if (!monk.IsReactionTypeFriendly(creatureInShape)) continue;

            creatureInShape.ApplyEffect(EffectDuration.Temporary, emptyLink, effectTime);
        }
    }
}