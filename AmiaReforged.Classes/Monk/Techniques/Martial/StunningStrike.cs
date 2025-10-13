using AmiaReforged.Classes.Monk.Augmentations;
using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Monk.Techniques.Martial;

[ServiceBinding(typeof(ITechnique))]
public class StunningStrike(AugmentationFactory augmentationFactory) : ITechnique
{
    public TechniqueType TechniqueType => TechniqueType.StunningStrike;

    public void HandleAttackTechnique(NwCreature monk, OnCreatureAttack attackData)
    {
        PathType? path = MonkUtils.GetMonkPath(monk);

        IAugmentation? augmentation = path.HasValue ? augmentationFactory.GetAugmentation(path.Value) : null;

        if (augmentation != null)
            augmentation.ApplyAttackAugmentation(monk, TechniqueType, attackData);
        else
            DoStunningStrike(attackData);
    }

    public static SavingThrowResult DoStunningStrike(OnCreatureAttack attackData)
    {
        if (attackData.Target is not NwCreature targetCreature) return SavingThrowResult.Immune;

        NwCreature monk = attackData.Attacker;

        Effect stunningStrikeEffect = Effect.LinkEffects(
            Effect.Stunned(),
            Effect.VisualEffect(VfxType.DurCessateNegative)
        );

        stunningStrikeEffect.SubType = EffectSubType.Extraordinary;

        int dc = MonkUtils.CalculateMonkDc(monk);

        SavingThrowResult savingThrowResult =
            targetCreature.RollSavingThrow(SavingThrow.Fortitude, dc, SavingThrowType.None, monk);

        switch (savingThrowResult)
        {
            case SavingThrowResult.Immune:
                break;
            case SavingThrowResult.Failure:
                targetCreature.ApplyEffect(EffectDuration.Temporary, stunningStrikeEffect, NwTimeSpan.FromRounds(1));
                break;
            case SavingThrowResult.Success:
                targetCreature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpFortitudeSavingThrowUse));
                break;
        }

        return savingThrowResult;
    }

    public void HandleCastTechnique(NwCreature monk, OnSpellCast castData) {}
}
