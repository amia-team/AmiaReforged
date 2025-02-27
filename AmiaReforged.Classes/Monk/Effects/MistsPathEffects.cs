using AmiaReforged.Classes.Monk.Techniques.Body;
using AmiaReforged.Classes.Monk.Techniques.Martial;
using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.Classes.Monk.Effects;

public static class MistsPathEffects
{
    
    public static void ApplyMistPathEffects(TechniqueType technique, OnSpellCast? castData = null, OnCreatureAttack? attackData = null)
    {
        switch (technique)
        {
            case TechniqueType.Axiomatic: AugmentAxiomatic(attackData);
                break;
            case TechniqueType.Stunning: AugmentStunning(attackData);
                break;
            case TechniqueType.KiShout : AugmentKiShout(castData);
                break;
            case TechniqueType.EmptyBody : AugmentEmptyBody(castData);
                break;
            case TechniqueType.Quivering : AugmentQuivering(castData);
                break;
            case TechniqueType.Eagle: EagleStrike.DoEagleStrike(attackData);
                break;
            case TechniqueType.Wholeness: WholenessOfBody.DoWholenessOfBody(castData);
                break;
            case TechniqueType.KiBarrier: KiBarrier.DoKiBarrier(castData);
                break;
        }
    }
    private static void AugmentAxiomatic(OnCreatureAttack attackData)
    {
       
    }
    private static void AugmentStunning(OnCreatureAttack attackData)
    {
       
    }
    private static void AugmentKiShout(OnSpellCast castData)
    {
       
    }
    private static void AugmentEmptyBody(OnSpellCast castData)
    {
        
    }
    private static void AugmentQuivering(OnSpellCast castData)
    {
        
    }
    
}