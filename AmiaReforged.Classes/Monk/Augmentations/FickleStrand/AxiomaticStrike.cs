using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Monk.Augmentations.FickleStrand;

[ServiceBinding(typeof(IAugmentation))]
public class AxiomaticStrike : IAugmentation.IAttackAugment
{
    public PathType Path => PathType.FickleStrand;
    public TechniqueType Technique => TechniqueType.AxiomaticStrike;
    public void ApplyAttackAugmentation(NwCreature monk, OnCreatureAttack attackData, BaseTechniqueCallback baseTechnique)
    {
        baseTechnique();
        AugmentAxiomaticStrike(monk, attackData);
    }

    /// <summary>
    /// Axiomatic Strike deals +1 bonus magical damage. Each Ki Focus increases the damage by 1,
    /// to a maximum of +4 bonus magical damage.
    /// </summary>
    private void AugmentAxiomaticStrike(NwCreature monk, OnCreatureAttack attackData)
    {
        int bonusDamage = MonkUtils.GetKiFocus(monk) switch
        {
            KiFocus.KiFocus1 => 2,
            KiFocus.KiFocus2 => 3,
            KiFocus.KiFocus3 => 4,
            _ => 1
        };

        DamageData<short> damageData = attackData.DamageData;
        short magicalDamage = damageData.GetDamageByType(DamageType.Magical);

        if (attackData.AttackResult == AttackResult.CriticalHit)
            bonusDamage *= MonkUtils.GetCritMultiplier(attackData, monk);

        if (magicalDamage == -1) bonusDamage++;

        magicalDamage += (short)bonusDamage;
        damageData.SetDamageByType(DamageType.Magical, magicalDamage);
    }
}
