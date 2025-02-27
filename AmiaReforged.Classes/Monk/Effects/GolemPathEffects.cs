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
            case TechniqueType.Eagle : AugmentEagle(attackData);
                break;
            case TechniqueType.KiBarrier : AugmentKiBarrier(castData);
                break;
            case TechniqueType.EmptyBody : AugmentEmptyBody(castData);
                break;
            case TechniqueType.Wholeness : AugmentWholeness(castData);
                break;
            case TechniqueType.KiShout : AugmentKiShout(castData);
                break;
                
        }
    }
    private static void AugmentEagle(OnCreatureAttack attackData)
    {
       
    }
    private static void AugmentKiBarrier(OnSpellCast castData)
    {
       
    }
    private static void AugmentEmptyBody(OnSpellCast castData)
    {
        
    }
    private static void AugmentWholeness(OnSpellCast castData)
    {
       
    }
    private static void AugmentKiShout(OnSpellCast castData)
    {
       
    }
    
}