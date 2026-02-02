using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using static AmiaReforged.Classes.Monk.Augmentations.CrashingMeteor.CrashingMeteorData;

namespace AmiaReforged.Classes.Monk.Augmentations.CrashingMeteor;

[ServiceBinding(typeof(IAugmentation))]
public class AxiomaticStrike : IAugmentation.IAttackAugment
{
    public PathType Path => PathType.CrashingMeteor;
    public TechniqueType Technique => TechniqueType.AxiomaticStrike;

    public void ApplyAttackAugmentation(NwCreature monk, OnCreatureAttack attackData, BaseTechniqueCallback baseTechnique)
    {
        baseTechnique();
        AugmentAxiomaticStrike(monk, attackData);
    }

    /// <summary>
    ///     Axiomatic Strike deals +1 bonus elemental damage, with an additional +1 for every Ki Focus,
    ///     to a maximum of +4 elemental damage.
    /// </summary>
    private void AugmentAxiomaticStrike(NwCreature monk, OnCreatureAttack attackData)
    {
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
