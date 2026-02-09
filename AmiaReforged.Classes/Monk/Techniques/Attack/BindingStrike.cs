using AmiaReforged.Classes.Monk.Augmentations;
using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Monk.Techniques.Attack;

[ServiceBinding(typeof(ITechnique))]
public class BindingStrike(AugmentationFactory augmentationFactory) : IAttackTechnique
{
    public TechniqueType Technique => TechniqueType.BindingStrike;
    public void HandleAttackTechnique(NwCreature monk, OnCreatureAttack attackData)
    {
        PathType? path = MonkUtils.GetMonkPath(monk);

        IAugmentation? augmentation = path.HasValue ? augmentationFactory.GetAugmentation(path.Value, Technique) : null;

        if (augmentation is IAugmentation.IAttackAugment attackAugment)
        {
            attackAugment.ApplyAttackAugmentation(monk, attackData, BaseTechnique);
        }
        else
        {
            BaseTechnique();
        }

        return;

        void BaseTechnique() => DoBindingStrike(monk, attackData);
    }

    public static SavingThrowResult DoBindingStrike(NwCreature monk, OnCreatureAttack attackData)
    {
        if (attackData.Target is not NwCreature targetCreature)
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
        {
            monk.ControllingPlayer?.SendServerMessage($"{targetCreature.Name} : Immune to Paralysis.");
            return SavingThrowResult.Immune;
        }

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
