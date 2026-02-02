using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Monk.Augmentations.SplinteredChalice;

[ServiceBinding(typeof(IAugmentation))]
public class AxiomaticStrike : IAugmentation.IAttackAugment
{
    public PathType Path => PathType.SplinteredChalice;
    public TechniqueType Technique => TechniqueType.AxiomaticStrike;
    public void ApplyAttackAugmentation(NwCreature monk, OnCreatureAttack attackData, BaseTechniqueCallback baseTechnique)
    {
        baseTechnique();
        AugmentAxiomaticStrike(monk, attackData);
    }

    /// <summary>
    /// Axiomatic Strike deals +1 negative damage, with extra +1 damage per Ki Focus.
    /// Overflow: Deals +2 divine damage, with extra +2 damage per Ki Focus.
    /// </summary>
    private static void AugmentAxiomaticStrike(NwCreature monk, OnCreatureAttack attackData)
    {
        DamageData<short> damageData = attackData.DamageData;

        DamageType bonusDamageType = DamageType.Negative;
        int bonusDamage = MonkUtils.GetKiFocus(monk) switch
        {
            KiFocus.KiFocus1 => 2,
            KiFocus.KiFocus2 => 3,
            KiFocus.KiFocus3 => 4,
            _ => 1
        };

        if (OverflowConstant.HasOverflow(monk))
        {
            bonusDamageType = DamageType.Divine;
            bonusDamage *= 2;
        }

        if (attackData.AttackResult == AttackResult.CriticalHit)
            bonusDamage *= MonkUtils.GetCritMultiplier(attackData, monk);

        short baseDamage = damageData.GetDamageByType(bonusDamageType);

        if (baseDamage == -1) bonusDamage++;

        baseDamage += (short)bonusDamage;
        damageData.SetDamageByType(bonusDamageType, baseDamage);
    }
}
