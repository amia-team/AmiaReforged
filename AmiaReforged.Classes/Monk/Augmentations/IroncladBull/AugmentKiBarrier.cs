using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Monk.Augmentations.IroncladBull;

[ServiceBinding(typeof(IAugmentation))]
public class AugmentKiBarrier : IAugmentation.ICastAugment
{
    public PathType Path => PathType.IroncladBull;
    public TechniqueType Technique => TechniqueType.KiBarrier;

    /// <summary>
    /// Grants 6/- Physical Damage Resistance. Each Ki Focus adds +3.
    /// </summary>
    public void ApplyCastAugmentation(NwCreature monk, OnSpellCast castData, BaseTechniqueCallback baseTechnique)
    {
        baseTechnique();

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
