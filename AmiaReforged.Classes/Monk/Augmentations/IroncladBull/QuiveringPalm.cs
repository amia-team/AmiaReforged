using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Monk.Augmentations.IroncladBull;

[ServiceBinding(typeof(IAugmentation))]
public class QuiveringPalm : IAugmentation.ICastAugment
{
    public PathType Path => PathType.IroncladBull;
    public TechniqueType Technique => TechniqueType.QuiveringPalm;
    public void ApplyCastAugmentation(NwCreature monk, OnSpellCast castData, BaseTechniqueCallback baseTechnique)
    {
        AugmentQuiveringPalm(monk, castData);
    }

    /// <summary>
    /// Quivering Palm binds the target with Stonehold for one round if they fail a reflex saving throw.
    /// Each Ki Focus increases the duration by one round, to a maximum of four rounds.
    /// </summary>
    private static void AugmentQuiveringPalm(NwCreature monk, OnSpellCast castData)
    {
        TouchAttackResult touchAttackResult = Techniques.Cast.QuiveringPalm.DoQuiveringPalm(monk, castData);

        if (touchAttackResult is TouchAttackResult.Miss || castData.TargetObject is not NwCreature targetCreature
                                                        || targetCreature.IsImmuneTo(ImmunityType.Paralysis)) return;

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
