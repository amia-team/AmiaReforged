// Called from the spirit technique handler when the technique is cast

using AmiaReforged.Classes.Monk.Augmentations;
using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.Classes.Monk.Techniques.Spirit;

public static class QuiveringPalm
{
    public static void CastQuiveringPalm(OnSpellCast castData)
    {
        NwCreature monk = (NwCreature)castData.Caster;
        PathType? path = MonkUtils.GetMonkPath(monk);
        const TechniqueType technique = TechniqueType.Quivering;

        if (path != null)
        {
            AugmentationApplier.ApplyAugmentations(path, technique, castData);
            return;
        }

        DoQuiveringPalm(castData);
    }
    
    /// <summary>
    /// On a successful melee touch attack against an enemy creature, the target must make a fortitude save or die.
    /// Each use depletes a Spirit Ki Point.
    /// </summary>
    public static TouchAttackResult DoQuiveringPalm(OnSpellCast castData)
    {
        if (castData.TargetObject is not NwCreature targetCreature) return TouchAttackResult.Miss;

        NwCreature monk = (NwCreature)castData.Caster;

        int dc = MonkUtils.CalculateMonkDc(monk);

        Effect quiveringEffect = Effect.Death(true);
        Effect quiveringVfx = Effect.VisualEffect(VfxType.ImpDeath, false, 0.7f);

        CreatureEvents.OnSpellCastAt.Signal(monk, targetCreature, castData.Spell!);

        if (monk.TouchAttackMelee(targetCreature) is TouchAttackResult.Miss) return TouchAttackResult.Miss;

        SavingThrowResult savingThrowResult =
            targetCreature.RollSavingThrow(SavingThrow.Fortitude, dc, SavingThrowType.Death, monk);

        if (savingThrowResult is SavingThrowResult.Success)
        {
            targetCreature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpFortitudeSavingThrowUse));
        }
        else
        {
            targetCreature.ApplyEffect(EffectDuration.Instant, quiveringEffect);
            targetCreature.ApplyEffect(EffectDuration.Instant, quiveringVfx);
        }

        return TouchAttackResult.Hit;
    }
}