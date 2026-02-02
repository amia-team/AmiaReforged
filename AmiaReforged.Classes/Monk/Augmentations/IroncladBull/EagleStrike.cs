using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Monk.Augmentations.IroncladBull;

[ServiceBinding(typeof(IAugmentation))]
public class EagleStrike : IAugmentation.IDamageAugment
{
    private const string IroncladEagleTag = nameof(PathType.IroncladBull) + nameof(TechniqueType.EagleStrike);
    public PathType Path => PathType.IroncladBull;
    public TechniqueType Technique => TechniqueType.EagleStrike;
    public void ApplyDamageAugmentation(NwCreature monk, OnCreatureDamage damageData, BaseTechniqueCallback baseTechnique)
    {
        AugmentEagleStrike(monk, damageData);
    }

    /// <summary>
    /// Eagle Strike incurs a -1 physical damage penalty. Each Ki Focus increases this by 1 to a maximum of -4.
    /// </summary>
    private static void AugmentEagleStrike(NwCreature monk, OnCreatureDamage damageData)
    {
        SavingThrowResult savingThrowResult = Techniques.Attack.EagleStrike.DoEagleStrike(monk, damageData);

        if (savingThrowResult != SavingThrowResult.Failure) return;

        Effect? existingEffect = damageData.Target.ActiveEffects.FirstOrDefault(e => e.Tag == IroncladEagleTag);
        if (existingEffect != null)
            damageData.Target.RemoveEffect(existingEffect);

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

        damageData.Target.ApplyEffect(EffectDuration.Temporary, eagleDamageDecrease, NwTimeSpan.FromRounds(2));
    }
}
