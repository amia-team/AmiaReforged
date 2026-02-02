using AmiaReforged.Classes.Monk.Augmentations;
using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Monk.Techniques.Cast;

[ServiceBinding(typeof(ITechnique))]
public class EmptyBody(AugmentationFactory augmentationFactory) : ICastTechnique
{
    public TechniqueType Technique => TechniqueType.EmptyBody;

    public void HandleCastTechnique(NwCreature monk, OnSpellCast castData)
    {
        PathType? path = MonkUtils.GetMonkPath(monk);

        IAugmentation? augmentation = path.HasValue
            ? augmentationFactory.GetAugmentation(path.Value, Technique)
            : null;

        if (augmentation is IAugmentation.ICastAugment castAugment)
        {
            castAugment.ApplyCastAugmentation(monk, castData, BaseTechnique);
        }
        else
        {
            BaseTechnique();
        }

        return;

        void BaseTechnique() => DoEmptyBody(monk);
    }

    /// <summary>
    ///     The monk is given 50% concealment for rounds per monk level. Each use depletes a Body Ki Point.
    /// </summary>
    private static void DoEmptyBody(NwCreature monk)
    {
        byte monkLevel = monk.GetClassInfo(ClassType.Monk)?.Level ?? 0;

        Effect emptyBodyEffect = Effect.LinkEffects(
            Effect.Concealment(50),
            Effect.VisualEffect(VfxType.DurInvisibility),
            Effect.VisualEffect(VfxType.DurCessatePositive));

        emptyBodyEffect.SubType = EffectSubType.Supernatural;
        TimeSpan effectDuration = NwTimeSpan.FromRounds(monkLevel);

        monk.ApplyEffect(EffectDuration.Temporary, emptyBodyEffect, effectDuration);
    }
}
