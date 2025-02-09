using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.Test.Classes.Monk.Effects;

public static class MistsPathEffects
{
    
    public static void ApplyMistPathEffects(TechniqueType technique, OnSpellCast? castData = null, OnCreatureAttack? attackData = null)
    {
        switch (technique)
        {
            case TechniqueType.Axiomatic: ApplyEffectsToAxiomatic(attackData);
                break;
            case TechniqueType.Stunning: ApplyEffectsToStunning(attackData);
                break;
            case TechniqueType.KiShout : ApplyEffectsToKiShout(castData);
                break;
            case TechniqueType.EmptyBody : ApplyEffectsToEmptyBody(castData);
                break;
            case TechniqueType.Quivering : ApplyEffectsToQuivering(castData);
                break;
                
        }
    }
    private static void ApplyEffectsToAxiomatic(OnCreatureAttack attackData)
    {
       
    }
    private static void ApplyEffectsToStunning(OnCreatureAttack attackData)
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
    
}