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
    /// The monk is given a damage reduction of 5/-. The barrier absorbs up to 10 points of physical damage for
    /// every two monk levels and lasts for turns per monk level. Each use depletes a Body Ki Point.
    /// </summary>
    public static void DoKiBarrier(OnSpellCast castData)
    {
        NwCreature monk = (NwCreature)castData.Caster;
        int monkLevel = monk.GetClassInfo(ClassType.Monk)!.Level;
        int totalAbsorb = monkLevel / 2 * 10;
        Effect kiBarrierEffect = Effect.LinkEffects(
            Effect.DamageReduction(5, DamagePower.Plus20, totalAbsorb),
            Effect.VisualEffect(VfxType.DurCessatePositive));
        kiBarrierEffect.SubType = EffectSubType.Supernatural;
        Effect kiBarrierVfx = Effect.VisualEffect(VfxType.ImpDeathWard, false, 0.7f);
        TimeSpan effectDuration = NwTimeSpan.FromTurns(monkLevel);

        monk.ApplyEffect(EffectDuration.Temporary, kiBarrierEffect, effectDuration);
        monk.ApplyEffect(EffectDuration.Instant, kiBarrierVfx);
    }
}