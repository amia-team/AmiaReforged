using AmiaReforged.Classes.Monk.Augmentations;
using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Monk.Techniques.Attack;

[ServiceBinding(typeof(ITechnique))]
public class BindingStrike(AugmentationFactory augmentationFactory) : IDamageTechnique
{
    public TechniqueType Technique => TechniqueType.BindingStrike;

    public void HandleDamageTechnique(NwCreature monk, OnCreatureDamage damageData)
    {
        PathType? path = MonkUtils.GetMonkPath(monk);

        IAugmentation? augmentation = path.HasValue ? augmentationFactory.GetAugmentation(path.Value, Technique) : null;

        if (augmentation is IAugmentation.IDamageAugment damageAugment)
        {
            damageAugment.ApplyDamageAugmentation(monk, damageData, BaseTechnique);
        }
        else
        {
            BaseTechnique();
        }

        return;

        void BaseTechnique() => DoBindingStrike(monk, damageData);

    }

    public static SavingThrowResult DoBindingStrike(NwCreature monk, OnCreatureDamage damageData)
    {
        if (damageData.Target is not NwCreature targetCreature)
            return SavingThrowResult.Immune;

        Effect bindingStrikeEffect = Effect.LinkEffects(
            Effect.Paralyze(),
            Effect.VisualEffect(VfxType.DurParalyzeHold),
            Effect.VisualEffect(VfxType.DurFreezeAnimation)
        );
        bindingStrikeEffect.IgnoreImmunity = true; // default paralysis fails against mind immune
        bindingStrikeEffect.SubType = EffectSubType.Extraordinary;

        int dc = MonkUtils.CalculateMonkDc(monk);

        // since we bypass the mind immunity by ignoring immunity, check again here for paralysis immunity
        if (targetCreature.IsImmuneTo(ImmunityType.Paralysis))
            return SavingThrowResult.Immune;

        SavingThrowResult savingThrowResult =
            targetCreature.RollSavingThrow(SavingThrow.Fortitude, dc, SavingThrowType.None, monk);

        switch (savingThrowResult)
        {
            case SavingThrowResult.Failure:
                targetCreature.ApplyEffect(EffectDuration.Temporary, bindingStrikeEffect, NwTimeSpan.FromRounds(1));
                break;
            case SavingThrowResult.Success:
                targetCreature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpFortitudeSavingThrowUse));
                break;
        }

        return savingThrowResult;
    }
}
