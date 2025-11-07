using AmiaReforged.Classes.Monk.Augmentations;
using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Monk.Techniques.Attack;

[ServiceBinding(typeof(ITechnique))]
public class EagleStrike(AugmentationFactory augmentationFactory) : ITechnique
{
    private const string EagleEffectTag = nameof(TechniqueType.EagleStrike);
    public TechniqueType TechniqueType => TechniqueType.EagleStrike;

    public void HandleDamageTechnique(NwCreature monk, OnCreatureDamage damageData)
    {
        PathType? path = MonkUtils.GetMonkPath(monk);

        IAugmentation? augmentation = path.HasValue ? augmentationFactory.GetAugmentation(path.Value) : null;

        if (augmentation != null)
            augmentation.ApplyDamageAugmentation(monk, TechniqueType, damageData);
        else
            DoEagleStrike(monk, damageData);
    }

    /// <summary>
    /// On two successful hits per round against an enemy creature, the target must succeed at a reflex save or suffer
    /// a penalty of -2 to their armor class for two rounds.
    /// </summary>
    public static SavingThrowResult DoEagleStrike(NwCreature monk, OnCreatureDamage damageData)
    {
        if (damageData.Target is not NwCreature targetCreature)
            return SavingThrowResult.Immune;

        int dc = MonkUtils.CalculateMonkDc(monk);

        SavingThrowResult savingThrowResult =
            targetCreature.RollSavingThrow(SavingThrow.Reflex, dc, SavingThrowType.None, monk);

        if (savingThrowResult == SavingThrowResult.Success)
            ApplyEagleStrike(targetCreature);

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

        Effect eagleStrikeVfx = Effect.VisualEffect(VfxType.ImpStarburstRed);

        targetCreature.ApplyEffect(EffectDuration.Temporary, eagleStrikeEffect, NwTimeSpan.FromRounds(2));
        targetCreature.ApplyEffect(EffectDuration.Instant, eagleStrikeVfx);
    }

    public void HandleCastTechnique(NwCreature monk, OnSpellCast castData) { }
    public void HandleAttackTechnique(NwCreature monk, OnCreatureAttack attackData) { }
}
