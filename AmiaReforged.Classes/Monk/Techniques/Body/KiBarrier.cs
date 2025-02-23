// Called from the body technique handler when the technique is cast
using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;


namespace AmiaReforged.Classes.Monk.Techniques.Body;

public static class KiBarrier
{
    public static void CastKiBarrier(OnSpellCast castData)
    {
        NwCreature monk = (NwCreature)castData.Caster;
        PathType? path = MonkUtilFunctions.GetMonkPath(monk);
        TechniqueType technique = TechniqueType.KiBarrier;

        if (path != null)
        {
            PathEffectApplier.ApplyPathEffects(path, technique, castData);
            return;
        }

        int monkLevel = monk.GetClassInfo(ClassType.Monk)!.Level;
        int damageReductionAmount = 5;
        int totalAbsorb = monkLevel / 2 * 10;
        Effect kiBarrierEffect = Effect.LinkEffects(Effect.DamageReduction(damageReductionAmount, DamagePower.Plus20, totalAbsorb),
            Effect.VisualEffect(VfxType.DurCessatePositive));
        kiBarrierEffect.SubType = EffectSubType.Supernatural;
        Effect kiBarrierVfx = Effect.VisualEffect(VfxType.ImpDeathWard, false, 0.7f);
        TimeSpan effectDuration = NwTimeSpan.FromTurns(monkLevel);

        monk.ApplyEffect(EffectDuration.Temporary, kiBarrierEffect, effectDuration);
        monk.ApplyEffect(EffectDuration.Instant, kiBarrierVfx);
    }
}