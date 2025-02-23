// Called from the spirit technique handler when the technique is cast
using AmiaReforged.Classes.Monk.Types;
using Anvil.API;
using Anvil.API.Events;


namespace AmiaReforged.Classes.Monk.Techniques.Body;

public static class QuiveringPalm
{
    public static void CastQuiveringPalm(OnSpellCast castData)
    {
        NwCreature monk = (NwCreature)castData.Caster;
        PathType? path = MonkUtilFunctions.GetMonkPath(monk);
        const TechniqueType technique = TechniqueType.Quivering;

        if (path != null)
        {
            PathEffectApplier.ApplyPathEffects(path, technique, castData);
            return;
        }

        if (castData.TargetObject is not NwCreature targetCreature) return;
        
        int dc = MonkUtilFunctions.CalculateMonkDc(monk);

        Effect quiveringEffect = Effect.Death(true);
        Effect quiveringVfx = Effect.VisualEffect(VfxType.ImpDeath, false, 0.7f);
        
        TouchAttackResult touchAttackResult = monk.TouchAttackMelee(targetCreature).Result;
        
        CreatureEvents.OnSpellCastAt.Signal(monk, targetCreature, castData.Spell!);

        if (touchAttackResult is TouchAttackResult.Miss) return;
        
        SavingThrowResult savingThrowResult = 
            targetCreature.RollSavingThrow(SavingThrow.Fortitude, dc, SavingThrowType.Death, monk);

        if (savingThrowResult is SavingThrowResult.Success)
            targetCreature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpFortitudeSavingThrowUse));
            
        else
        {
            targetCreature.ApplyEffect(EffectDuration.Instant, quiveringEffect);
            targetCreature.ApplyEffect(EffectDuration.Instant, quiveringVfx);
        }
    }
}
