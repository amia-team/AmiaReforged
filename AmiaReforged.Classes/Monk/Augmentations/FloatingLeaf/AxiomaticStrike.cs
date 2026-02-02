using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Monk.Augmentations.FloatingLeaf;

[ServiceBinding(typeof(IAugmentation))]
public class AxiomaticStrike : IAugmentation.IAttackAugment
{
    public PathType Path => PathType.FloatingLeaf;
    public TechniqueType Technique => TechniqueType.AxiomaticStrike;
    public void ApplyAttackAugmentation(NwCreature monk, OnCreatureAttack attackData, BaseTechniqueCallback baseTechnique)
    {
        baseTechnique();
        AugmentAxiomaticStrike(monk, attackData);
    }

    /// <summary>
    /// Axiomatic Strike deals +1 bonus positive damage, increased by an additional +1 for every Ki Focus to a maximum
    /// of +4 bonus positive damage.
    /// </summary>
    private static void AugmentAxiomaticStrike(NwCreature monk, OnCreatureAttack attackData)
    {
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
