using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Monk.Augmentations.FloatingLeaf;

[ServiceBinding(typeof(IAugmentation))]
public class AugmentAxiomaticStrike : IAugmentation.IAttackAugment
{
    public PathType Path => PathType.FloatingLeaf;
    public TechniqueType Technique => TechniqueType.AxiomaticStrike;

    /// <summary>
    /// Deals +1 positive damage. Each Ki Focus adds +1.
    /// </summary>
    public void ApplyAttackAugmentation(NwCreature monk, OnCreatureAttack attackData, BaseTechniqueCallback baseTechnique)
    {
        baseTechnique();

        DamageData<short> damageData = attackData.DamageData;
        short positiveDamage = damageData.GetDamageByType(DamageType.Positive);

        int bonusDamage = MonkUtils.GetKiFocus(monk) switch
        {
            KiFocus.KiFocus1 => 2,
            KiFocus.KiFocus2 => 3,
            KiFocus.KiFocus3 => 4,
            _ => 1
        };

        if (attackData.AttackResult == AttackResult.CriticalHit)
            bonusDamage *= MonkUtils.GetCritMultiplier(attackData, monk);

        if (positiveDamage == -1) bonusDamage++;

        positiveDamage += (short)bonusDamage;
        damageData.SetDamageByType(DamageType.Positive, positiveDamage);
    }
}
