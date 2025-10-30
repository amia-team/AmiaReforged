using AmiaReforged.Classes.Monk.Augmentations;
using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Monk.Techniques.Body;

[ServiceBinding(typeof(ITechnique))]
public class EmptyBody(AugmentationFactory augmentationFactory) : ITechnique
{
    public TechniqueType TechniqueType => TechniqueType.EmptyBody;

    public void HandleCastTechnique(NwCreature monk, OnSpellCast castData)
    {
        PathType? path = MonkUtils.GetMonkPath(monk);

        IAugmentation? augmentation = path.HasValue ? augmentationFactory.GetAugmentation(path.Value) : null;

        if (augmentation != null)
            augmentation.ApplyCastAugmentation(monk, TechniqueType, castData);
        else
            DoEmptyBody(monk);
    }

    /// <summary>
    ///     The monk is given 50% concealment for rounds per monk level. Each use depletes a Body Ki Point.
    /// </summary>
    public static void DoEmptyBody(NwCreature monk)
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

    public void HandleAttackTechnique(NwCreature monk, OnCreatureAttack attackData) { }
    public void HandleDamageTechnique(NwCreature monk, OnCreatureDamage damageData) { }
}
