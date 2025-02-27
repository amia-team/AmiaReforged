using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.Classes.Monk.Effects;

public static class MantlePathEffects
{
    public static void ApplyMantlePathEffects(TechniqueType technique, OnSpellCast? castData = null, OnCreatureAttack? attackData = null)
    {
        switch (technique)
        {
            case TechniqueType.Stunning : AugmentStunning(attackData);
                break;
            case TechniqueType.Axiomatic: AugmentAxiomatic(attackData);
                break;
            case TechniqueType.KiBarrier : AugmentKiBarrier(castData);
                break;
            case TechniqueType.KiShout : AugmentKiShout(castData);
                break;
            case TechniqueType.EmptyBody : AugmentEmptyBody(castData);
                break;
                
        }
    }
    private static void AugmentStunning(OnCreatureAttack attackData)
    {
       
    }
    private static void AugmentAxiomatic(OnCreatureAttack attackData)
    {
       
    }
    private static void AugmentKiBarrier(OnSpellCast castData)
    {
       
    }
    private static void AugmentKiShout(OnSpellCast castData)
    {
       
    }
    private static void AugmentEmptyBody(OnSpellCast castData)
    {
        
    }
}