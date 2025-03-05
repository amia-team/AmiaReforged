using AmiaReforged.Classes.Monk.Techniques.Body;
using AmiaReforged.Classes.Monk.Techniques.Martial;
using AmiaReforged.Classes.Monk.Types;
using Anvil.API.Events;

namespace AmiaReforged.Classes.Monk.Augmentations;

public static class CrackedVessel
{
    public static void ApplyAugmentations(TechniqueType technique, OnSpellCast? castData = null,
        OnCreatureAttack? attackData = null)
    {
        switch (technique)
        {
            case TechniqueType.Axiomatic:
                AugmentAxiomatic(attackData);
                break;
            case TechniqueType.KiShout:
                AugmentKiShout(castData);
                break;
            case TechniqueType.EmptyBody:
                AugmentEmptyBody(castData);
                break;
            case TechniqueType.Quivering:
                AugmentQuivering(castData);
                break;
            case TechniqueType.Wholeness:
                AugmentWholeness(castData);
                break;
            case TechniqueType.Stunning:
                StunningStrike.DoStunningStrike(attackData);
                break;
            case TechniqueType.Eagle:
                EagleStrike.DoEagleStrike(attackData);
                break;
            case TechniqueType.KiBarrier:
                KiBarrier.DoKiBarrier(castData);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(technique), technique, null);
        }
    }

    private static void AugmentAxiomatic(OnCreatureAttack attackData)
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

    private static void AugmentWholeness(OnSpellCast castData)
    {
    }
}