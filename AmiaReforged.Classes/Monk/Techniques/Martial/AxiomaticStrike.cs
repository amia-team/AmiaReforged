// The ability script called by the MartialTechniqueService

using AmiaReforged.Classes.Monk.Augmentations;
using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.Classes.Monk.Techniques.Martial;

public static class AxiomaticStrike
{
    public static void ApplyAxiomaticStrike(OnCreatureAttack attackData)
    {
        NwCreature monk = attackData.Attacker;
        PathType? path = MonkUtilFunctions.GetMonkPath(monk);
        const TechniqueType technique = TechniqueType.Axiomatic;

        if (path != null)
        {
            AugmentationApplier.ApplyAugmentations(path, technique, null, attackData);
            return;
        }
        
        DoAxiomaticStrike(attackData);
    }

    public static void DoAxiomaticStrike(OnCreatureAttack attackData)
    {
        NwCreature monk = attackData.Attacker;
        DamageData<short> damageData = attackData.DamageData;
        int monkLevel = monk.GetClassInfo(ClassType.Monk)!.Level;
        short bludgeoningDamage = damageData.GetDamageByType(DamageType.Bludgeoning);
        short bonusDamageAxiomatic = monkLevel switch
        {
            >= 10 and <= 19 => 2,
            >= 20 and <= 29 => 3,
            30 => 4,
            _ => 1
        };

        // Apply Axiomatic's bonus damage
        bludgeoningDamage += bonusDamageAxiomatic;
        damageData.SetDamageByType(DamageType.Bludgeoning, bludgeoningDamage);
    }
}
