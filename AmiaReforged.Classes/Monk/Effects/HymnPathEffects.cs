using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;
using System.Numerics;

namespace AmiaReforged.Classes.Monk.Effects;

public static class HymnPathEffects
{
    
    
    public static void ApplyHymnsPathEffects(TechniqueType technique, OnSpellCast? castData = null, OnCreatureAttack? attackData = null)
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
        int regen = 1;
        int concealment = 25;
        TimeSpan regenTime = TimeSpan.FromSeconds(6);
        TimeSpan effectTime = TimeSpan.FromSeconds(monkLevel*6); // Adjust as appropriate. 1 round per monk level.
        
        // Wholeness is gained at 7
        if (monkLevel == 30)
        {
            pulseAmount = 3;
            healAmount = (int)level30Heal;
            regen = 7;
            concealment = 55;
        }
        else if (monkLevel >= 28)
        {
            pulseAmount = 3; 
            regen = 5;
            concealment = 50;
        }
        else if (monkLevel >= 24)
        {
            pulseAmount = 2; 
            regen = 4;
            concealment = 45;
        }
        else if (monkLevel >= 20)
        {
            pulseAmount = 2; 
            regen = 3;
            concealment = 40;
        }
        else if (monkLevel >= 16)
        {
            pulseAmount = 1; 
            regen = 2;
            concealment = 30;
        }
        else if (monkLevel >= 12)
        {
            // Set by default, here just in case
        }

        Effect wholenessRegen = Effect.Regenerate(regen,regenTime);
        Effect wholenessConcealment = Effect.Concealment(concealment, MissChanceType.Normal);
        Effect wholenessEffect = Effect.Heal(healAmount);
        Effect wholenessVfx = Effect.VisualEffect(VfxType.ImpHealingL, false, 0.7f);
        Effect aoeVfx = Effect.VisualEffect(VfxType.ImpPulseHoly, false, 1.0f);
        

        for (int i = 0; i < pulseAmount; i++) // Pulse amount
        { 
            monk.ApplyEffect(EffectDuration.Instant,aoeVfx); // AoE fire at the base of the Monk  
        }
            foreach (NwGameObject nwObject in monk.Location!.GetObjectsInShape(Shape.Sphere, RadiusSize.Large, false))
            {
                NwCreature creatureInShape = (NwCreature)nwObject;
                if (monk.IsReactionTypeFriendly(creatureInShape)) continue;
                creatureInShape.ApplyEffect(EffectDuration.Temporary,wholenessRegen,effectTime);
                creatureInShape.ApplyEffect(EffectDuration.Temporary,wholenessConcealment,effectTime);
                for (int e = 0; e < pulseAmount; e++) // Pulse amount
                {
                    creatureInShape.ApplyEffect(EffectDuration.Instant, wholenessEffect);
                    creatureInShape.ApplyEffect(EffectDuration.Instant, wholenessVfx);
                }
            }
        
    }
    private static void AugmentEmptyBody(OnSpellCast castData)
    {
       
    }
    
}