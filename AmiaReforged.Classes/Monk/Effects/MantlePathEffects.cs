using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.Test.Classes.Monk.Effects;

public static class MantlePathEffects
{
    public static void ApplyMantlePathEffects(TechniqueType technique, OnSpellCast? castData = null, OnCreatureAttack? attackData = null)
    {
        switch (technique)
        {
            case TechniqueType.Stunning : ApplyEffectsToStunning(attackData);
                break;
            case TechniqueType.Axiomatic: ApplyEffectsToAxiomatic(attackData);
                break;
            case TechniqueType.KiBarrier : ApplyEffectsToKiBarrier(castData);
                break;
            case TechniqueType.KiShout : ApplyEffectsToKiShout(castData);
                break;
            case TechniqueType.EmptyBody : ApplyEffectsToEmptyBody(castData);
                break;
                
        }
    }
    private static void ApplyEffectsToStunning(OnCreatureAttack attackData)
    {
       
    }
    private static void ApplyEffectsToAxiomatic(OnCreatureAttack attackData)
    {
       
    }
    private static void ApplyEffectsToKiBarrier(OnSpellCast castData)
    {
       
    }
    private static void ApplyEffectsToKiShout(OnSpellCast castData)
    {
       
    }
    private static void ApplyEffectsToEmptyBody(OnSpellCast castData)
    {
        
    }
}