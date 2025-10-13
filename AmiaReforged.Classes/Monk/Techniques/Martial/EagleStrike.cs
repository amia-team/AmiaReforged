using AmiaReforged.Classes.Monk.Augmentations;
using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Monk.Techniques.Martial;

[ServiceBinding(typeof(ITechnique))]
public class EagleStrike(AugmentationFactory augmentationFactory) : ITechnique
{
    private const string EagleEffectTag = nameof(TechniqueType.EagleStrike);
    public TechniqueType TechniqueType => TechniqueType.EagleStrike;

    public void HandleAttackTechnique(NwCreature monk, OnCreatureDamage attackData)
    {
        PathType? path = MonkUtils.GetMonkPath(monk);

        IAugmentation? augmentation = path.HasValue ? augmentationFactory.GetAugmentation(path.Value) : null;

        if (augmentation != null)
            augmentation.ApplyAttackAugmentation(monk, TechniqueType, attackData);
        else
            DoEagleStrike(monk, attackData);
    }

    /// <summary>
    /// On two successful hits per round against an enemy creature, the target must succeed at a reflex save or suffer
    /// a penalty of -2 to their armor class for two rounds.
    /// </summary>
    public static SavingThrowResult DoEagleStrike(NwCreature monk, OnCreatureDamage attackData)
    {
        if (attackData.Target is not NwCreature targetCreature)
            return SavingThrowResult.Immune;

        int dc = MonkUtils.CalculateMonkDc(monk);

        SavingThrowResult savingThrowResult =
            targetCreature.RollSavingThrow(SavingThrow.Reflex, dc, SavingThrowType.None, monk);

        switch (savingThrowResult)
        {
            case SavingThrowResult.Immune:
                break;
            case SavingThrowResult.Success:
                targetCreature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpReflexSaveThrowUse));
                break;
            case SavingThrowResult.Failure:
                ApplyEagleStrike(targetCreature);
                break;
        }

        return savingThrowResult;
    }

    private static void ApplyEagleStrike(NwCreature targetCreature)
    {
        Effect? eagleStrikeEffect = targetCreature.ActiveEffects.FirstOrDefault(e => e.Tag == EagleEffectTag);

        if (eagleStrikeEffect != null)
            targetCreature.RemoveEffect(eagleStrikeEffect);

        eagleStrikeEffect = Effect.LinkEffects(
            Effect.ACDecrease(2),
            Effect.VisualEffect(VfxType.DurCessateNegative)
        );

        eagleStrikeEffect.Tag = EagleEffectTag;
        eagleStrikeEffect.SubType = EffectSubType.Extraordinary;

        Effect eagleStrikeVfx = Effect.VisualEffect(VfxType.ImpStarburstRed, false, 0.7f);

        targetCreature.ApplyEffect(EffectDuration.Temporary, eagleStrikeEffect, NwTimeSpan.FromRounds(2));
        targetCreature.ApplyEffect(EffectDuration.Instant, eagleStrikeVfx);
    }

    public void HandleCastTechnique(NwCreature monk, OnSpellCast castData) {}
}
