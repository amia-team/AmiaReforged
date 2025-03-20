// Called from the body technique handler when the technique is cast

using AmiaReforged.Classes.Monk.Augmentations;
using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.Classes.Monk.Techniques.Body;

public static class WholenessOfBody
{
    public static void CastWholenessOfBody(OnSpellCast castData)
    {
        NwCreature monk = (NwCreature)castData.Caster;
        PathType? path = MonkUtils.GetMonkPath(monk);
        const TechniqueType technique = TechniqueType.Wholeness;

        if (path != null)
        {
            AugmentationApplier.ApplyAugmentations(path, technique, castData);
            return;
        }

        DoWholenessOfBody(castData);
    }
    
    /// <summary>
    ///     The monk can heal damage equal to twice their class level. Each use depletes a Body Ki Point.
    /// </summary>
    public static void DoWholenessOfBody(OnSpellCast castData)
    {
        NwCreature monk = (NwCreature)castData.Caster;
        int monkLevel = monk.GetClassInfo(ClassType.Monk)!.Level;
        int healAmount = monkLevel * 2;
        Effect wholenessEffect = Effect.Heal(healAmount);
        Effect wholenessVfx = Effect.VisualEffect(VfxType.ImpHealingL, false, 0.7f);

        monk.ApplyEffect(EffectDuration.Instant, wholenessEffect);
        monk.ApplyEffect(EffectDuration.Instant, wholenessVfx);
    }
}