using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;
using System.Numerics;
using AmiaReforged.Classes.Monk.Techniques.Martial;
using AmiaReforged.Classes.Monk.Techniques.Spirit;

namespace AmiaReforged.Classes.Monk.Augmentations;

public static class SwingingCenser
{
    
    
    public static void ApplyAugmentations(TechniqueType technique, OnSpellCast? castData = null, OnCreatureAttack? attackData = null)
    {
        switch (technique)
        {
            case TechniqueType.Eagle : AugmentEagle(attackData);
                break;
            case TechniqueType.KiBarrier : AugmentKiBarrier(castData);
                break;
            case TechniqueType.KiShout : AugmentKiShout(castData);
                break;
            case TechniqueType.Wholeness : AugmentWholeness(castData);
                break;
            case TechniqueType.EmptyBody : AugmentEmptyBody(castData);
                break;
            case TechniqueType.Stunning: StunningStrike.DoStunningStrike(attackData);
                break;
            case TechniqueType.Axiomatic: AxiomaticStrike.DoAxiomaticStrike(attackData);
                break;
            case TechniqueType.Quivering: QuiveringPalm.DoQuiveringPalm(castData);
                break;
        }
    }
    private static void AugmentEagle(OnCreatureAttack attackData)
    {
       
    }
    private static void AugmentKiBarrier(OnSpellCast castData)
    {
       
    }
    private static void AugmentKiShout(OnSpellCast castData)
    {
        
    }
    private static void AugmentWholeness(OnSpellCast castData)
    {   
        NwCreature monk = (NwCreature)castData.Caster;
        Location monkLocation = monk.Location;
        int monkLevel = monk.GetClassInfo(ClassType.Monk)!.Level;
        int pulseAmount = 1;
        int healAmount = monkLevel*2;
        double level30Heal = healAmount * 1.5;
        TimeSpan regenTime = TimeSpan.FromSeconds(6);
        TimeSpan effectTime = TimeSpan.FromSeconds(monkLevel*6); // Adjust as appropriate. 1 round per monk level.
        
        // Wholeness is gained at 7
        if (monkLevel == 30) 
        {
            pulseAmount = 3;
            healAmount = (int)level30Heal;
        }
        else if (monkLevel >= 24)
        {
            pulseAmount = 3; 
        }
        else if (monkLevel >= 18)
        {
            pulseAmount = 2; 
        }
        else if (monkLevel >= 12)
        {
            pulseAmount = 1; 
        }

        Effect wholenessEffect = Effect.Heal(healAmount);
        Effect wholenessVfx = Effect.VisualEffect(VfxType.ImpHealingL, false, 0.7f);
        Effect wholenessLink = Effect.LinkEffects(wholenessEffect, wholenessVfx);
        Effect aoeVfx = Effect.VisualEffect(VfxType.ImpPulseHoly, false, 1.0f);

        
        WholenessPulse();
        return;
        
        async void WholenessPulse()
        {
            for (int i = 0; i < pulseAmount; i++) // Pulse amount
            { 
            
                // If monk is dead or monk isn't valid, return
                if (monk.IsDead || !monk.IsValid) return;
                
                monk.ApplyEffect(EffectDuration.Instant,aoeVfx); // AoE Holy Pulse fire at the base of the Monk  
                
                foreach (NwGameObject nwObject in monk.Location!.GetObjectsInShape(Shape.Sphere, RadiusSize.Large, false))
                {
                    NwCreature creatureInShape = (NwCreature)nwObject;
                
                    if (monk.IsReactionTypeFriendly(creatureInShape)) continue;
                
                    creatureInShape.ApplyEffect(EffectDuration.Instant, wholenessLink);
                }
            
                await NwTask.Delay(TimeSpan.FromSeconds(3));
            }
        }

    }

    private static void AugmentEmptyBody(OnSpellCast castData)
    { 
        
        // WIP 
        
        NwCreature monk = (NwCreature)castData.Caster;
        Location monkLocation = monk.Location;
        int monkLevel = monk.GetClassInfo(ClassType.Monk)!.Level;
        int regen = 1;
        int concealment = 25;
        TimeSpan regenTime = TimeSpan.FromSeconds(6);
        TimeSpan effectTime = TimeSpan.FromSeconds(monkLevel*6); // Adjust as appropriate. 1 round per monk level.
        
        // Wholeness is gained at 7
        if (monkLevel == 30) 
        {
            regen = 7;
            concealment = 55;
        }
        else if (monkLevel >= 24)
        {
            regen = 5;
            concealment = 45;
        }
        else if (monkLevel >= 18)
        {
            regen = 4;
            concealment = 35;
        }
        else if (monkLevel >= 12)
        {
            regen = 2;
            concealment = 25;
        }
        Effect wholenessRegen = Effect.Regenerate(regen,regenTime);
        Effect wholenessConcealment = Effect.Concealment(concealment, MissChanceType.Normal);
       
    }
    
}