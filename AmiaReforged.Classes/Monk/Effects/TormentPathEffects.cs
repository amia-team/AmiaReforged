using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.Test.Classes.Monk.Effects;

public static class TormentPathEffects
{  
    public static void ApplyTormentPathEffects(TechniqueType technique, OnSpellCast? castData = null, OnCreatureAttack? attackData = null)
    {
        switch (technique)
        {
            case TechniqueType.Axiomatic: ApplyEffectsToAxiomatic(attackData);
                break;
            case TechniqueType.KiShout : ApplyEffectsToKiShout(castData);
                break;
            case TechniqueType.EmptyBody : ApplyEffectsToEmptyBody(castData);
                break;
            case TechniqueType.Quivering : ApplyEffectsToQuivering(castData);
                break;
            case TechniqueType.Wholeness : ApplyEffectsToWholeness(castData);
                break;
                
        }
    }
    private static void ApplyEffectsToAxiomatic(OnCreatureAttack attackData)
    {
       
    }
    private static void ApplyEffectsToKiShout(OnSpellCast castData)
    {
       
    }
    private static void ApplyEffectsToEmptyBody(OnSpellCast castData)
    {
        
    }
    private static void ApplyEffectsToQuivering(OnSpellCast castData)
    {
    }
    private static void ApplyEffectsToWholeness(OnSpellCast castData)
    {
       
    }
    
}