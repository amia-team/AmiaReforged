using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.Classes.Monk.Effects;

public static class GolemPathEffects
{
    public static void ApplyGolemPathEffects(TechniqueType technique, OnSpellCast? castData = null, OnCreatureAttack? attackData = null)
    {
        switch (technique)
        {
            case TechniqueType.Eagle : ApplyEffectsToEagle(attackData);
                break;
            case TechniqueType.KiBarrier : ApplyEffectsToKiBarrier(castData);
                break;
            case TechniqueType.EmptyBody : ApplyEffectsToEmptyBody(castData);
                break;
            case TechniqueType.Wholeness : ApplyEffectsToWholeness(castData);
                break;
            case TechniqueType.KiShout : ApplyEffectsToKiShout(castData);
                break;
                
        }
    }
    private static void ApplyEffectsToEagle(OnCreatureAttack attackData)
    {
       
    }
    private static void ApplyEffectsToKiBarrier(OnSpellCast castData)
    {
       
    }
    private static void ApplyEffectsToEmptyBody(OnSpellCast castData)
    {
        
    }
    private static void ApplyEffectsToWholeness(OnSpellCast castData)
    {
       
    }
    private static void ApplyEffectsToKiShout(OnSpellCast castData)
    {
       
    }
    
}