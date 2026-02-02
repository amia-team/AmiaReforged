using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Monk.Augmentations.IroncladBull;

[ServiceBinding(typeof(IAugmentation))]
public class KiBarrier : IAugmentation.ICastAugment
{
    public PathType Path => PathType.IroncladBull;
    public TechniqueType Technique => TechniqueType.KiBarrier;
    public void ApplyCastAugmentation(NwCreature monk, OnSpellCast castData, BaseTechniqueCallback baseTechnique)
    {
        baseTechnique();
        AugmentKiBarrier(monk);
    }

    /// <summary>
    /// Ki Barrier grants 6/- physical damage resistance, with each Ki Focus increasing it by 3,
    /// to a maximum of 15/- physical damage resistance.
    /// </summary>
    private void AugmentKiBarrier(NwCreature monk)
    {
        byte monkLevel = monk.GetClassInfo(ClassType.Monk)?.Level ?? 0;

        byte resistanceAmount = MonkUtils.GetKiFocus(monk) switch
        {
            KiFocus.KiFocus1 => 9,
            KiFocus.KiFocus2 => 12,
            KiFocus.KiFocus3 => 15,
            _ => 6
        };

        Effect kiBarrierEffect = Effect.LinkEffects
        (
            Effect.DamageResistance(DamageType.Bludgeoning, resistanceAmount),
            Effect.DamageResistance(DamageType.Slashing, resistanceAmount),
            Effect.DamageResistance(DamageType.Piercing, resistanceAmount), Effect.VisualEffect(VfxType.DurCessatePositive)
        );

        monk.ApplyEffect(EffectDuration.Temporary, kiBarrierEffect, NwTimeSpan.FromTurns(monkLevel));
    }
}
