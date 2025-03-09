using AmiaReforged.Classes.Monk.Constants;
using AmiaReforged.Classes.Monk.Techniques.Body;
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
            case TechniqueType.EmptyBody:
                EmptyBody.DoEmptyBody(castData);
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
    
    /// <summary>
    /// 
    /// </summary>
    private static void AugmentKiBarrier(OnSpellCast castData)
    {
    }
    
    /// <summary>
    /// Wholeness of Body grants 20 temporary hit points until removed. Each Ki Focus increases the amount of temporary
    /// hit points by 20, to a maximum of 80 temporary hit points.
    /// </summary>
    private static void AugmentWholeness(OnUseFeat wholenessData)
    {
        WholenessOfBody.DoWholenessOfBody(wholenessData);
        
        NwCreature monk = wholenessData.Creature;
        int monkLevel = monk.GetClassInfo(ClassType.Monk)!.Level;
        
        int tempHpAmount = monkLevel switch
        {
            >= MonkLevel.KiFocusI and < MonkLevel.KiFocusIi => 40,
            >= MonkLevel.KiFocusIi and < MonkLevel.KiFocusIii => 60,
            MonkLevel.KiFocusIii => 80,
            _ => 20
        };
        
        monk.ApplyEffect(EffectDuration.Permanent, Effect.TemporaryHitpoints(tempHpAmount));
    }

    private static void AugmentKiShout(OnSpellCast castData)
    {
    }
}