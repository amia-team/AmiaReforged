using AmiaReforged.Classes.Monk.Augmentations;
using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Monk.Techniques.Attack;

[ServiceBinding(typeof(ITechnique))]
public class AxiomaticStrike(AugmentationFactory augmentationFactory) : IAttackTechnique
{
    public TechniqueType Technique => TechniqueType.AxiomaticStrike;
    public void HandleAttackTechnique(NwCreature monk, OnCreatureAttack attackData)
    {
        PathType? path = MonkUtils.GetMonkPath(monk);

        IAugmentation? augmentation = path.HasValue
            ? augmentationFactory.GetAugmentation(path.Value, Technique)
            : null;

        if (augmentation is IAugmentation.IAttackAugment damageAugment)
        {
            damageAugment.ApplyAttackAugmentation(monk, attackData, BaseTechnique);
        }
        else
        {
            BaseTechnique();
        }

        return;

        void BaseTechnique() => DoAxiomaticStrike(monk, attackData);
    }

    /// <summary>
    /// Each successful hit deals +1 bonus physical damage. Every 10 monk levels increases the damage by +1.
    /// </summary>
    private void DoAxiomaticStrike(NwCreature monk, OnCreatureAttack attackData)
    {
        DamageData<short> damageData = attackData.DamageData;

        short bludgeoningDamage = damageData.GetDamageByType(DamageType.Bludgeoning);
        int bonusDamage = MonkUtils.GetKiFocus(monk) switch
        {
            KiFocus.KiFocus1 => 2,
            KiFocus.KiFocus2 => 3,
            KiFocus.KiFocus3 => 4,
            _ => 1
        };

        if (attackData.AttackResult == AttackResult.CriticalHit)
            bonusDamage *= MonkUtils.GetCritMultiplier(attackData, monk);

        if (bludgeoningDamage == -1) bonusDamage++;

        bludgeoningDamage += (short)bonusDamage;
        damageData.SetDamageByType(DamageType.Bludgeoning, bludgeoningDamage);
    }
}
