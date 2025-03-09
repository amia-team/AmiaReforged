using AmiaReforged.Classes.Monk.Constants;
using AmiaReforged.Classes.Monk.Techniques.Body;
using AmiaReforged.Classes.Monk.Techniques.Martial;
using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.Classes.Monk.Augmentations;

public static class CrackedVessel
{
    public static void ApplyAugmentations(TechniqueType technique, OnSpellCast? castData = null, OnUseFeat? 
            wholenessData = null, OnCreatureAttack? attackData = null)
    {
        switch (technique)
        {
            case TechniqueType.Axiomatic:
                AugmentAxiomatic(attackData);
                break;
            case TechniqueType.KiShout:
                AugmentKiShout(castData);
                break;
            case TechniqueType.Quivering:
                AugmentQuivering(castData);
                break;
            case TechniqueType.Wholeness:
                AugmentWholeness(wholenessData);
                break;
            case TechniqueType.Stunning:
                StunningStrike.DoStunningStrike(attackData);
                break;
            case TechniqueType.Eagle:
                EagleStrike.DoEagleStrike(attackData);
                break;
            case TechniqueType.EmptyBody:
                EmptyBody.DoEmptyBody(castData);
                break;
            case TechniqueType.KiBarrier:
                KiBarrier.DoKiBarrier(castData);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(technique), technique, null);
        }
    }
    
    /// <summary>
    /// Axiomatic Strike deals 1d2 bonus negative energy damage when the monk is injured, 1d4 when badly wounded,
    /// and 1d6 when near death. In addition, every three killing blows made with this attack against living enemies
    /// regenerates a Body Ki Point. Each Ki Focus multiplies the damage bonus,
    /// to a maximum of 4d2, 4d4, and 4d6 bonus negative energy damage.
    /// </summary>
    private static void AugmentAxiomatic(OnCreatureAttack attackData)
    {
        AxiomaticStrike.DoAxiomaticStrike(attackData);

        NwCreature monk = attackData.Attacker;
        int monkLevel = monk.GetClassInfo(ClassType.Monk)!.Level;
        int damageDice = monkLevel switch
        {
            >= MonkLevel.KiFocusI and < MonkLevel.KiFocusIi => 2,
            >= MonkLevel.KiFocusIi and < MonkLevel.KiFocusIii => 3,
            MonkLevel.KiFocusIii => 4,
            _ => 1
        };
        int damageSides = IsInjured(monk) ? 2 : IsBadlyWounded(monk) ? 4 : IsNearDeath(monk) ? 6 : 0;
        int damageAmount = Random.Shared.Roll(damageSides, damageDice);
        
        DamageData<short> damageData = attackData.DamageData;
        short negativeDamage = damageData.GetDamageByType(DamageType.Negative);
        
        negativeDamage += (short)damageAmount;
        damageData.SetDamageByType(DamageType.Negative, negativeDamage);
    }

    private static void AugmentKiShout(OnSpellCast castData)
    {
    }

    private static void AugmentQuivering(OnSpellCast castData)
    {
    }

    private static void AugmentWholeness(OnUseFeat wholenessData)
    {
    }
    
    private static bool IsInjured(NwCreature monk)
    {
        return monk.HP < monk.MaxHP * 0.75;
    }

    private static bool IsBadlyWounded(NwCreature monk)
    {
        return monk.HP < monk.MaxHP * 0.50;
    }
    
    private static bool IsNearDeath(NwCreature monk)
    {
        return monk.HP < monk.MaxHP * 0.25;
    }
}