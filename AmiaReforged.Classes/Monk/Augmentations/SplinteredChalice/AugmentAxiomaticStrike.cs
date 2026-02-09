using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Monk.Augmentations.SplinteredChalice;

[ServiceBinding(typeof(IAugmentation))]
public class AugmentAxiomaticStrike : IAugmentation.IAttackAugment
{
    public PathType Path => PathType.SplinteredChalice;
    public TechniqueType Technique => TechniqueType.AxiomaticStrike;

    /// <summary>
    /// Axiomatic Strike deals +1 negative damage, with extra +1 damage per Ki Focus.
    /// Overflow: Deals +2 divine damage, with extra +2 damage per Ki Focus.
    /// </summary>
    public void ApplyAttackAugmentation(NwCreature monk, OnCreatureAttack attackData, BaseTechniqueCallback baseTechnique)
    {
        baseTechnique();

        bool hasOverflow = Overflow.HasOverflow(monk);
        KiFocus? kiFocus = MonkUtils.GetKiFocus(monk);

        int bonusDamage = hasOverflow switch
        {
            true => kiFocus switch
            {
                KiFocus.KiFocus1 => 4,
                KiFocus.KiFocus2 => 6,
                KiFocus.KiFocus3 => 8,
                _ => 2
            },
            false => kiFocus switch
            {
                KiFocus.KiFocus1 => 2,
                KiFocus.KiFocus2 => 3,
                KiFocus.KiFocus3 => 4,
                _ => 1
            }
        };

        DamageType bonusDamageType = hasOverflow switch
        {
            true => DamageType.Divine,
            false => DamageType.Negative
        };

        if (attackData.AttackResult == AttackResult.CriticalHit)
            bonusDamage *= MonkUtils.GetCritMultiplier(attackData, monk);

        DamageData<short> damageData = attackData.DamageData;
        short baseDamage = damageData.GetDamageByType(bonusDamageType);

        if (baseDamage == -1) bonusDamage++;

        baseDamage += (short)bonusDamage;
        damageData.SetDamageByType(bonusDamageType, baseDamage);
    }
}
