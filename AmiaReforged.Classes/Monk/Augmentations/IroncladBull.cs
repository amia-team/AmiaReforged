using AmiaReforged.Classes.Monk.Techniques.Martial;
using AmiaReforged.Classes.Monk.Techniques.Spirit;
using AmiaReforged.Classes.Monk.Types;
using Anvil.API.Events;

namespace AmiaReforged.Classes.Monk.Augmentations;

public static class IroncladBull
{
    public static void ApplyAugmentations(TechniqueType technique, OnSpellCast? castData = null, OnSpellAction? 
            wholenessData = null, OnCreatureAttack? attackData = null)
    {
        switch (technique)
        {
            case TechniqueType.Eagle:
                AugmentEagle(attackData);
                break;
            case TechniqueType.KiBarrier:
                AugmentKiBarrier(castData);
                break;
            case TechniqueType.EmptyBody:
                AugmentEmptyBody(castData);
                break;
            case TechniqueType.Wholeness:
                AugmentWholeness(wholenessData);
                break;
            case TechniqueType.KiShout:
                AugmentKiShout(castData);
                break;
            case TechniqueType.Stunning:
                StunningStrike.DoStunningStrike(attackData);
                break;
            case TechniqueType.Axiomatic:
                AxiomaticStrike.DoAxiomaticStrike(attackData);
                break;
            case TechniqueType.Quivering:
                QuiveringPalm.DoQuiveringPalm(castData);
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

    private static void AugmentWholeness(OnSpellAction wholenessData)
    {
    }

    private static void AugmentKiShout(OnSpellCast castData)
    {
    }
}