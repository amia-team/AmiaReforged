using AmiaReforged.Classes.Monk.Techniques.Body;
using AmiaReforged.Classes.Monk.Techniques.Martial;
using AmiaReforged.Classes.Monk.Techniques.Spirit;
using AmiaReforged.Classes.Monk.Types;
using Anvil.API.Events;

namespace AmiaReforged.Classes.Monk.Augmentations;

public static class ChardalynSand
{
    public static void ApplyAugmentations(TechniqueType technique, OnSpellCast? castData = null,
        OnCreatureAttack? attackData = null)
    {
        switch (technique)
        {
            case TechniqueType.Stunning:
                AugmentStunning(attackData);
                break;
            case TechniqueType.Axiomatic:
                AugmentAxiomatic(attackData);
                break;
            case TechniqueType.KiBarrier:
                AugmentKiBarrier(castData);
                break;
            case TechniqueType.KiShout:
                AugmentKiShout(castData);
                break;
            case TechniqueType.EmptyBody:
                AugmentEmptyBody(castData);
                break;
            case TechniqueType.Eagle:
                EagleStrike.DoEagleStrike(attackData);
                break;
            case TechniqueType.Wholeness:
                WholenessOfBody.DoWholenessOfBody(castData);
                break;
            case TechniqueType.Quivering:
                QuiveringPalm.DoQuiveringPalm(castData);
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