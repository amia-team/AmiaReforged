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
    public static void ApplyAugmentations(TechniqueType technique, OnSpellCast? castData = null, OnUseFeat? 
            wholenessData = null, OnCreatureAttack? attackData = null)
    {
        switch (technique)
        {
            case TechniqueType.Stunning:
                AugmentStunning(attackData);
                break;
            case TechniqueType.Wholeness:
                AugmentWholeness(wholenessData);
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
    
    /// <summary>
    /// Stunning Fist heals the monk or a nearby ally for 1d6 damage. Healing 100 damage with this attack regenerates
    /// a Body Ki Point. Each Ki Focus heals for an additional 1d6, to a maximum of 4d6 damage.
    /// </summary>
    private static void AugmentStunning(OnCreatureAttack attackData)
    {
        StunningStrike.DoStunningStrike(attackData);
        
        NwCreature monk = attackData.Attacker;
        
        // Target must be a hostile creature
        if (!monk.IsReactionTypeHostile((NwCreature)attackData.Target)) return;
        
        int monkLevel = monk.GetClassInfo(ClassType.Monk)!.Level;
        int healAmount = monkLevel switch
        {
            >= MonkLevel.KiFocusI and < MonkLevel.KiFocusIi => Random.Shared.Roll(6, 2),
            >= MonkLevel.KiFocusIi and < MonkLevel.KiFocusIii => Random.Shared.Roll(6, 3),
            MonkLevel.KiFocusIii => Random.Shared.Roll(6, 4),
            _ => Random.Shared.Roll(6)
        };
        
        Effect healVfx = Effect.VisualEffect(VfxType.ImpHealingS, false, 0.7f);
        
        // If monk's injured, heal monk
        int monkMissingHp = monk.MaxHP - monk.HP;
        
        if (monkMissingHp > 0)
        {
            monk.ApplyEffect(EffectDuration.Instant,Effect.Heal(healAmount));
            monk.ApplyEffect(EffectDuration.Instant,healVfx);
        }
        
        // If all heal is used to heal the monk's missing HP, we know there's no remainder and we can return code early
        int healRemainder = healAmount > monkMissingHp ? healAmount - monkMissingHp : 0;
        
        if (healRemainder == 0)
        {
            CheckHealCounter(healAmount);
            return;
        }
        
        // If the code wasn't returned, we know there's heal left and we use it on the ally
        // We use HealAlly to return an int with the amount that the ally was healed for
        int allyHealAmount = HealAlly();
        
        // If there was any heal remainder left to bounce to the ally, we know that the amount the monk was healed
        // must be equal to the monk's missing HP; we add that with the ally heal amount and check the heal counter
        CheckHealCounter(monkMissingHp + allyHealAmount);
        
        return;
        
        int HealAlly()
        {
            // This is like a list, but it allows two variables per element
            Dictionary<string, int> alliesHp = new();
            
            // Look for hurt friends in a medium area
            foreach (NwGameObject nwObject in monk.Location!.GetObjectsInShape(Shape.Sphere, RadiusSize.Medium, true))
            {
                NwCreature monkAlly = (NwCreature)nwObject;

                // Creature must be ally, injured, and alive
                if (!(monk.IsReactionTypeFriendly(monkAlly) && monkAlly.HP < monkAlly.MaxHP && monkAlly.HP > -9)) 
                    continue;

                // name and missing HP are added to the list
                alliesHp.Add(monkAlly.Name, monkAlly.MaxHP - monk.HP);
            }

            // If no hurt allies were found, return
            if (alliesHp.Count == 0) return 0;
            
            // If hurt allies were found, we use the name of the lowest HP ally to get that ally in shape
            string lowestHpName = alliesHp.MinBy(hp => hp.Value).Key;
            int missingHp = alliesHp.Min().Value;
            
            NwGameObject allyToHeal = monk.Location.GetObjectsInShape(Shape.Sphere, RadiusSize.Medium, true).
                    First(ally => ally.Name == lowestHpName);
            
            allyToHeal.ApplyEffect(EffectDuration.Instant, Effect.Heal(healRemainder));
            allyToHeal.ApplyEffect(EffectDuration.Instant, healVfx);
            
            // We know that either all the heal is used or that it only heals up to the ally's missing HP,
            // so if the heal remainder is greater than missing HP, we return just the missing HP amount
            // but if the missing HP is greater than the heal remainder, all heal is used and we return the whole remainder
            return healRemainder > missingHp ? missingHp : healRemainder;
        }
        
        // If monk has Body Ki Points, mount up the heal counter to regenerate a body ki point
        void CheckHealCounter(int amountToCheck)
        {
            if (monkLevel < MonkLevel.BodyKiPointsI) return;
            
            LocalVariableInt healCounter = monk.GetObjectVariable<LocalVariableInt>("swingingcenser_healcounter");
            healCounter.Value += amountToCheck;

            if (healCounter.Value < 100) return;
            
            monk.IncrementRemainingFeatUses(NwFeat.FromFeatId(MonkFeat.BodyKiPoint)!);
            healCounter.Delete();
        }
    }
    /// <summary>
    /// Ki Shout exhorts allies with +1 bonus to attack rolls for one turn, with an additional +1 bonus for every Ki Focus.
    /// </summary>
    private static void AugmentKiShout(OnSpellCast castData)
    {
        KiShout.DoKiShout(castData);
        
        NwCreature monk = (NwCreature)castData.Caster;
        int monkLevel = monk.GetClassInfo(ClassType.Monk)!.Level;
        
        int abBonus = monkLevel switch
        {
            >= MonkLevel.KiFocusI and < MonkLevel.KiFocusIi => 2,
            >= MonkLevel.KiFocusIi and < MonkLevel.KiFocusIii => 3,
            MonkLevel.KiFocusIii => 4,
            _ => 1
        };
        Effect abBonusEffect = Effect.LinkEffects(Effect.AttackIncrease(abBonus), 
            Effect.VisualEffect(VfxType.DurCessatePositive));
        Effect abBonusVfx = Effect.VisualEffect(VfxType.ImpHeadSonic);
        TimeSpan effectDuration = NwTimeSpan.FromTurns(1);
        
        foreach (NwGameObject nwObject in monk.Location!.GetObjectsInShape(Shape.Sphere, RadiusSize.Colossal,
                     false))
        {
            NwCreature creatureInShape = (NwCreature)nwObject;

            if (!monk.IsReactionTypeFriendly(creatureInShape)) continue;
            
            creatureInShape.ApplyEffect(EffectDuration.Temporary, abBonusEffect, effectDuration);
            creatureInShape.ApplyEffect(EffectDuration.Instant, abBonusVfx);
        }
    }
    
    /// <summary>
    /// Wholeness of Body pulses in a large area around the monk, healing allies.
    /// Each Ki Focus adds a pulse to the heal, to a maximum of four pulses.
    /// </summary>
    private static void AugmentWholeness(OnUseFeat wholenessData)
    {
        NwCreature monk = wholenessData.Creature;
        int monkLevel = monk.GetClassInfo(ClassType.Monk)!.Level;
        int healAmount = monkLevel * 2;

        int pulseAmount = monkLevel switch
        {
            // Wholeness is gained at 7
            MonkLevel.KiFocusIii => 4,
            >= MonkLevel.KiFocusIi => 3,
            >= MonkLevel.KiFocusI => 2,
            _ => 1
        };

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