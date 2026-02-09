using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using static AmiaReforged.Classes.Monk.Augmentations.CrashingMeteor.CrashingMeteorData;

namespace AmiaReforged.Classes.Monk.Augmentations.CrashingMeteor;

[ServiceBinding(typeof(IAugmentation))]
public class AugmentAxiomaticStrike : IAugmentation.IAttackAugment
{
    public PathType Path => PathType.CrashingMeteor;
    public TechniqueType Technique => TechniqueType.AxiomaticStrike;

    /// <summary>
    /// Deals +1 elemental damage. Each Ki Focus adds +1.
    /// </summary>
    public void ApplyAttackAugmentation(NwCreature monk, OnCreatureAttack attackData, BaseTechniqueCallback baseTechnique)
    {
        baseTechnique();

        CrashingMeteorData meteor = GetCrashingMeteorData(monk);

        DamageData<short> damageData = attackData.DamageData;
        short elementalDamage = damageData.GetDamageByType(meteor.DamageType);

        int bonusDamage = meteor.BonusDamage;

        if (attackData.AttackResult == AttackResult.CriticalHit)
            bonusDamage *= MonkUtils.GetCritMultiplier(attackData, monk);

        if (elementalDamage == -1) bonusDamage++;

        elementalDamage += (short)bonusDamage;
        damageData.SetDamageByType(meteor.DamageType, elementalDamage);
    }
}
