using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Monk.Augmentations.EchoingValley;

[ServiceBinding(typeof(IAugmentation))]
public class AugmentAxiomaticStrike : IAugmentation.IAttackAugment
{
    public PathType Path => PathType.EchoingValley;
    public TechniqueType Technique => TechniqueType.AxiomaticStrike;

    /// <summary>
    /// Deals +1 sonic damage. Each Ki Focus adds +1.
    /// </summary>
    public void ApplyAttackAugmentation(NwCreature monk, OnCreatureAttack attackData, BaseTechniqueCallback baseTechnique)
    {
        baseTechnique();

        DamageData<short> damageData = attackData.DamageData;
        short sonicDamage = damageData.GetDamageByType(DamageType.Sonic);

        int bonusDamage = MonkUtils.GetKiFocus(monk) switch
        {
            KiFocus.KiFocus1 => 2,
            KiFocus.KiFocus2 => 3,
            KiFocus.KiFocus3 => 4,
            _ => 1
        };

        if (attackData.AttackResult == AttackResult.CriticalHit)
            bonusDamage *= MonkUtils.GetCritMultiplier(attackData, monk);

        if (sonicDamage == -1) bonusDamage++;

        sonicDamage += (short)bonusDamage;
        damageData.SetDamageByType(DamageType.Sonic, sonicDamage);
    }
}
