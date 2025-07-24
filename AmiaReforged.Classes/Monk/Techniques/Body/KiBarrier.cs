// Called from the body technique handler when the technique is cast

using AmiaReforged.Classes.Monk.Augmentations;
using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.Classes.Monk.Techniques.Body;

public static class KiBarrier
{
    public static void CastKiBarrier(OnSpellCast castData)
    {
        NwCreature monk = (NwCreature)castData.Caster;
        PathType? path = MonkUtils.GetMonkPath(monk);
        TechniqueType technique = TechniqueType.KiBarrier;

        if (path != null)
        {
            AugmentationApplier.ApplyAugmentations(path, technique, castData);
            return;
        }

        DoKiBarrier(castData);
    }

    /// <summary>
    /// The monk gains a +1 wisdom bonus. Each Ki Focus increases the bonus by +1, to a maximum of +4 at level 30 monk.
    /// </summary>
    public static void DoKiBarrier(OnSpellCast castData)
    {
        NwCreature monk = (NwCreature)castData.Caster;
        int monkLevel = monk.GetClassInfo(ClassType.Monk)?.Level ?? 0;

        int bonusWis = MonkUtils.GetKiFocus(monk) switch
        {
            KiFocus.KiFocus1 => 2,
            KiFocus.KiFocus2 => 3,
            KiFocus.KiFocus3 => 4,
            _ => 1
        };

        Effect kiBarrier = Effect.AbilityIncrease(Ability.Wisdom, bonusWis);
        Effect kiBarrierVfx = Effect.VisualEffect(VfxType.ImpDeathWard, false, 0.7f);

        TimeSpan effectDuration = NwTimeSpan.FromTurns(monkLevel);

        monk.ApplyEffect(EffectDuration.Temporary, kiBarrier, effectDuration);
        monk.ApplyEffect(EffectDuration.Instant, kiBarrierVfx);
    }
}
