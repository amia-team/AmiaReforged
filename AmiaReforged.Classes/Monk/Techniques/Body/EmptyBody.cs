// Called from the body technique handler when the technique is cast

using AmiaReforged.Classes.Monk.Augmentations;
using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.Classes.Monk.Techniques.Body;

/// <summary>
///     The monk is given 50% concealment for rounds per monk level. Using this technique spends one body ki point.
/// </summary>
public static class EmptyBody
{
    public static void CastEmptyBody(OnSpellCast castData)
    {
        NwCreature monk = (NwCreature)castData.Caster;
        PathType? path = MonkUtilFunctions.GetMonkPath(monk);
        const TechniqueType technique = TechniqueType.EmptyBody;

        if (path != null)
        {
            AugmentationApplier.ApplyAugmentations(path, technique, castData);
            return;
        }

        DoEmptyBody(castData);
    }

    public static void DoEmptyBody(OnSpellCast castData)
    {
        NwCreature monk = (NwCreature)castData.Caster;
        int monkLevel = monk.GetClassInfo(ClassType.Monk)!.Level;
        Effect emptyBodyEffect = Effect.LinkEffects(Effect.Concealment(50),
            Effect.VisualEffect(VfxType.DurInvisibility), Effect.VisualEffect(VfxType.DurCessatePositive));
        emptyBodyEffect.SubType = EffectSubType.Supernatural;
        TimeSpan effectDuration = NwTimeSpan.FromRounds(monkLevel);

        monk.ApplyEffect(EffectDuration.Temporary, emptyBodyEffect, effectDuration);
    }
}