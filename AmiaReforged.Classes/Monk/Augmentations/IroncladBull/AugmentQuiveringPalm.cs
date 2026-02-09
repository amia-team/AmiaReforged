using AmiaReforged.Classes.Monk.Techniques.Cast;
using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Monk.Augmentations.IroncladBull;

[ServiceBinding(typeof(IAugmentation))]
public class AugmentQuiveringPalm : IAugmentation.ICastAugment
{
    public PathType Path => PathType.IroncladBull;
    public TechniqueType Technique => TechniqueType.QuiveringPalm;

    /// <summary>
    /// Petrifies the target for 1 round (reflex negates). Each Ki Focus adds +1 round.
    /// </summary>
    public void ApplyCastAugmentation(NwCreature monk, OnSpellCast castData, BaseTechniqueCallback baseTechnique)
    {
        TouchAttackResult touchAttackResult = QuiveringPalm.DoQuiveringPalm(monk, castData);

        if (touchAttackResult is TouchAttackResult.Miss || castData.TargetObject is not NwCreature targetCreature) return;

        int dc = MonkUtils.CalculateMonkDc(monk);

        SavingThrowResult savingThrowResult =
            targetCreature.RollSavingThrow(SavingThrow.Reflex, dc, SavingThrowType.Paralysis, monk);

        if (savingThrowResult is SavingThrowResult.Success)
        {
            targetCreature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpReflexSaveThrowUse));
            return;
        }

        Effect quiveringEffect = Effect.LinkEffects(Effect.Petrify(),
            Effect.VisualEffect(VfxType.DurStonehold));

        TimeSpan quiveringDuration = MonkUtils.GetKiFocus(monk) switch
        {
            KiFocus.KiFocus1 => NwTimeSpan.FromRounds(2),
            KiFocus.KiFocus2 => NwTimeSpan.FromRounds(3),
            KiFocus.KiFocus3 => NwTimeSpan.FromRounds(4),
            _ => NwTimeSpan.FromRounds(1)
        };

        targetCreature.ApplyEffect(EffectDuration.Temporary, quiveringEffect, quiveringDuration);
    }
}
