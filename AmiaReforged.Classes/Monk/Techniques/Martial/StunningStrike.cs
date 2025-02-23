// The ability script called by the MartialTechniqueService
using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;

namespace AmiaReforged.Classes.Monk.Techniques.Martial;

public static class StunningStrike
{
    public static void ApplyStunningStrike(OnCreatureAttack attackData)
    {
        NwCreature monk = attackData.Attacker;
        PathType? path = MonkUtilFunctions.GetMonkPath(monk);
        const TechniqueType technique = TechniqueType.Stunning;

        if (path != null)
        {
            PathEffectApplier.ApplyPathEffects(path, technique, null, attackData);
            return;
        }
        
        Effect stunningStrikeEffect = Effect.LinkEffects(Effect.Stunned());
        stunningStrikeEffect.SubType = EffectSubType.Extraordinary;
        TimeSpan effectDuration = NwTimeSpan.FromRounds(3);
        int effectDc = MonkUtilFunctions.CalculateMonkDc(monk);

        // DC check for stunning effect
        if (attackData.Target is not NwCreature targetCreature) return;
            
        SavingThrowResult savingThrowResult = targetCreature.RollSavingThrow(SavingThrow.Fortitude, effectDc, SavingThrowType.None, monk);

        if (savingThrowResult is SavingThrowResult.Success) 
            targetCreature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpFortitudeSavingThrowUse));

        if (savingThrowResult is SavingThrowResult.Failure)
            targetCreature.ApplyEffect(EffectDuration.Temporary, stunningStrikeEffect, effectDuration);
    }
}