// The ability script called by the MartialTechniqueService
using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.Classes.Monk.Techniques.Martial;

public static class EagleStrike
{
    public static void ApplyEagleStrike(OnCreatureAttack attackData)
    {
        NwCreature monk = attackData.Attacker;
        PathType? path = MonkUtilFunctions.GetMonkPath(monk);
        const TechniqueType technique = TechniqueType.Eagle;

        if (path != null)
        {
            PathEffectApplier.ApplyPathEffects(path, technique, null, attackData);
            return;
        }
        
        TimeSpan effectDuration = NwTimeSpan.FromTurns(1);
        int acDecreaseAmount = 2;
        int effectDc = MonkUtilFunctions.CalculateMonkDc(monk);
        Effect eagleStrikeEffect = Effect.LinkEffects(Effect.ACDecrease(acDecreaseAmount), 
            Effect.VisualEffect(VfxType.DurCessateNegative));
        Effect eagleStrikeVfx = Effect.VisualEffect(VfxType.ComBloodSparkLarge);
        eagleStrikeEffect.Tag = "eaglestrike_effect";
        eagleStrikeEffect.SubType = EffectSubType.Extraordinary;

        // DC check for eagle effect
        if (attackData.Target is not NwCreature targetCreature) return;

        SavingThrowResult savingThrowResult = targetCreature.RollSavingThrow(SavingThrow.Reflex, effectDc, SavingThrowType.None, monk);

        if (savingThrowResult is SavingThrowResult.Success) 
            targetCreature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpReflexSaveThrowUse));

        if (savingThrowResult is SavingThrowResult.Failure)
        {
            // Prevent stacking, instead refresh effect
            foreach (Effect effect in targetCreature.ActiveEffects)
                if (effect.Tag == "eaglestrike_effect") targetCreature.RemoveEffect(effect);
            
            // Apply effect
            targetCreature.ApplyEffect(EffectDuration.Temporary, eagleStrikeEffect, effectDuration);
            targetCreature.ApplyEffect(EffectDuration.Instant, eagleStrikeVfx);
        }
    }
}
