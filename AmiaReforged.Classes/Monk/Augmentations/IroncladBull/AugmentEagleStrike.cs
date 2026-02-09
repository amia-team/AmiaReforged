using AmiaReforged.Classes.Monk.Techniques.Attack;
using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Monk.Augmentations.IroncladBull;

[ServiceBinding(typeof(IAugmentation))]
public class AugmentEagleStrike : IAugmentation.IAttackAugment
{
    private const string IroncladEagleTag = nameof(PathType.IroncladBull) + nameof(TechniqueType.EagleStrike);
    public PathType Path => PathType.IroncladBull;
    public TechniqueType Technique => TechniqueType.EagleStrike;

    /// <summary>
    /// Inflicts -1 physical damage penalty on the enemy. Each Ki Focus adds -1.
    /// </summary>
    public void ApplyAttackAugmentation(NwCreature monk, OnCreatureAttack attackData, BaseTechniqueCallback baseTechnique)
    {
        SavingThrowResult savingThrowResult = EagleStrike.DoEagleStrike(monk, attackData);

        if (savingThrowResult != SavingThrowResult.Failure) return;

        Effect? existingEffect = attackData.Target.ActiveEffects.FirstOrDefault(e => e.Tag == IroncladEagleTag);
        if (existingEffect != null)
            attackData.Target.RemoveEffect(existingEffect);

        int damageDecrease = MonkUtils.GetKiFocus(monk) switch
        {
            KiFocus.KiFocus1 => 2,
            KiFocus.KiFocus2 => 3,
            KiFocus.KiFocus3 => 4,
            _ => 1
        };

        Effect eagleDamageDecrease = Effect.DamageDecrease(damageDecrease, DamageType.BaseWeapon);
        eagleDamageDecrease.SubType = EffectSubType.Extraordinary;
        eagleDamageDecrease.Tag = IroncladEagleTag;

        attackData.Target.ApplyEffect(EffectDuration.Temporary, eagleDamageDecrease, NwTimeSpan.FromRounds(2));
    }
}
