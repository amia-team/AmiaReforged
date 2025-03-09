using AmiaReforged.Classes.Monk.Constants;
using AmiaReforged.Classes.Monk.Techniques.Martial;
using AmiaReforged.Classes.Monk.Techniques.Spirit;
using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.Classes.Monk.Augmentations;

public static class IroncladBull
{
    public static void ApplyAugmentations(TechniqueType technique, OnSpellCast? castData = null, OnUseFeat? 
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
    
    /// <summary>
    /// Eagle Strike has a 1% chance to regenerate a Body Ki Point. Each Ki Focus increases the chance by 1%,
    /// to a maximum of 4% chance.
    /// </summary>
    private static void AugmentEagle(OnCreatureAttack attackData)
    {
        EagleStrike.DoEagleStrike(attackData);
        
        NwCreature monk = attackData.Attacker;
        
        // Target must be a hostile creature
        if (!monk.IsReactionTypeHostile((NwCreature)attackData.Target)) return;
        
        int monkLevel = monk.GetClassInfo(ClassType.Monk)!.Level;
        
        // The effect only affects Body Ki Point recharge, so duh
        if (monkLevel < MonkLevel.BodyKiPointsI) return;
        
        int kiBodyRegenChance = monkLevel switch
        {
            >= MonkLevel.KiFocusI and < MonkLevel.KiFocusIi => 2,
            >= MonkLevel.KiFocusIi and < MonkLevel.KiFocusIii => 3,
            MonkLevel.KiFocusIii => 4,
            _ => 1
        };

        int d100Roll = Random.Shared.Roll(100);
        
        if (d100Roll <= kiBodyRegenChance)
            monk.IncrementRemainingFeatUses(NwFeat.FromFeatId(MonkFeat.BodyKiPoint)!);
    }

    private static void AugmentKiBarrier(OnSpellCast castData)
    {
    }

    private static void AugmentEmptyBody(OnSpellCast castData)
    {
    }

    private static void AugmentWholeness(OnUseFeat wholenessData)
    {
    }

    private static void AugmentKiShout(OnSpellCast castData)
    {
    }
}