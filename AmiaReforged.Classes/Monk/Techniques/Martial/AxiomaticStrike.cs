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
        PathType? path = MonkUtils.GetMonkPath(monk);
        const TechniqueType technique = TechniqueType.Axiomatic;

        if (path != null)
        {
            AugmentationApplier.ApplyAugmentations(path, technique, attackData: attackData);
            return;
        }

        DoAxiomaticStrike(attackData);
    }

    /// <summary>
    /// Each successful hit deals +1 bonus physical damage. Every 10 monk levels increases the damage by +1.
    /// </summary>
    public static void DoAxiomaticStrike(OnCreatureAttack attackData)
    {
        NwCreature monk = attackData.Attacker;
        DamageData<short> damageData = attackData.DamageData;

        short bludgeoningDamage = damageData.GetDamageByType(DamageType.Bludgeoning);
        short bonusDamage = MonkUtils.GetKiFocus(monk) switch
        {
            KiFocus.KiFocus1 => 2,
            KiFocus.KiFocus2 => 3,
            KiFocus.KiFocus3 => 4,
            _ => 1
        };

        // Apply Axiomatic's bonus damage
        bludgeoningDamage += bonusDamage;
        damageData.SetDamageByType(DamageType.Bludgeoning, bludgeoningDamage);
    }
}
