// The ability script called by the MartialTechniqueService

using AmiaReforged.Classes.Monk.Augmentations;
using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.Classes.Monk.Techniques.Martial;

public static class StunningStrike
{
    public static void ApplyStunningStrike(OnCreatureAttack attackData)
    {
        NwCreature monk = attackData.Attacker;
        PathType? path = MonkUtils.GetMonkPath(monk);
        const TechniqueType technique = TechniqueType.Stunning;

        if (path != null)
        {
            AugmentationApplier.ApplyAugmentations(path, technique, attackData: attackData);
            return;
        }

        DoStunningStrike(attackData);
    }

    /// <summary>
    /// On the first successful hit per round against an enemy creature, the target must succeed at a fortitude save
    /// or be stunned for one round.
    /// </summary>
    public static SavingThrowResult DoStunningStrike(OnCreatureAttack attackData)
    {
        NwCreature monk = attackData.Attacker;
        Effect stunningStrikeEffect =
            Effect.LinkEffects(Effect.Stunned(), Effect.VisualEffect(VfxType.DurCessateNegative));
        stunningStrikeEffect.SubType = EffectSubType.Extraordinary;
        TimeSpan effectDuration = NwTimeSpan.FromRounds(1);
        int effectDc = MonkUtils.CalculateMonkDc(monk);

        // DC check for stunning effect
        if (attackData.Target is not NwCreature targetCreature) return SavingThrowResult.Failure;

        SavingThrowResult savingThrowResult =
            targetCreature.RollSavingThrow(SavingThrow.Fortitude, effectDc, SavingThrowType.None, monk);

        if (savingThrowResult is SavingThrowResult.Success)
            targetCreature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpFortitudeSavingThrowUse));

        targetCreature.ApplyEffect(EffectDuration.Temporary, stunningStrikeEffect, effectDuration);

        return savingThrowResult;
    }
}
