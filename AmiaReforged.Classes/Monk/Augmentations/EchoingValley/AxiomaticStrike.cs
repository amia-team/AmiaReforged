using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Monk.Augmentations.EchoingValley;

[ServiceBinding(typeof(IAugmentation))]
public class AxiomaticStrike : IAugmentation.IAttackAugment
{
    public PathType Path => PathType.EchoingValley;
    public TechniqueType Technique => TechniqueType.AxiomaticStrike;

    public void ApplyAttackAugmentation(NwCreature monk, OnCreatureAttack attackData, BaseTechniqueCallback baseTechnique)
    {
        baseTechnique();
        AugmentAxiomaticStrike(monk, attackData);
    }

    /// <summary>
    /// Axiomatic Strike deals +1 bonus sonic damage for each Echo the monk has.
    /// </summary>
    private void AugmentAxiomaticStrike(NwCreature monk, OnCreatureAttack attackData)
    {
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
