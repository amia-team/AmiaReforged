using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.Classes.EffectUtils.DamageOverTime;

[ServiceBinding(typeof(DamageOverTimeService))]
public class DamageOverTimeService(ScriptHandleFactory scriptHandleFactory)
{
    /// <summary>
    /// Creates a DOT effect that applies damage every round for the effect's duration.
    /// </summary>
    /// <param name="caster">Caster of the DOT</param>
    /// <param name="dieSides">How many sides per damage die</param>
    /// <param name="diceAmount">How many damage dice</param>
    /// <param name="damageType">Type of damage</param>
    /// <param name="durVfx">The duration vfx that plays on the target object for the effect duration, default null</param>
    /// <param name="impVfx">The impact vfx that plays on the target object every time the damage is applied</param>
    /// <returns></returns>
    public Effect DotEffect(NwGameObject caster, int dieSides, int diceAmount, DamageType damageType, VfxType durVfx = VfxType.None, VfxType impVfx = VfxType.None)
    {
        ScriptCallbackHandle dotTic = scriptHandleFactory.CreateUniqueHandler(info
            => DotTic(info, caster, dieSides, diceAmount, damageType, impVfx));
        TimeSpan oneRound = NwTimeSpan.FromRounds(1);

        Effect durVisuals = Effect.LinkEffects(Effect.VisualEffect(durVfx), Effect.VisualEffect(VfxType.DurCessateNegative));
        Effect runAction = Effect.RunAction(onRemovedHandle: dotTic, onIntervalHandle: dotTic, interval: oneRound);
        Effect dotEffect = Effect.LinkEffects(durVisuals, runAction);

        return dotEffect;
    }

    private static ScriptHandleResult DotTic(CallInfo info, NwGameObject caster, int dieSides, int diceAmount, DamageType damageType, VfxType impVfx = VfxType.None)
    {
        if (info.ObjectSelf is not NwGameObject targetObject || targetObject is NwCreature { IsDead: true })
            return ScriptHandleResult.Handled;

        int damageRoll = Random.Shared.Roll(dieSides, diceAmount);

        _ = ApplyDotTic(targetObject, caster, damageRoll, damageType, impVfx);
        return ScriptHandleResult.Handled;
    }

    private static async Task ApplyDotTic(NwGameObject targetObject, NwGameObject caster, int damageRoll, DamageType damageType, VfxType impVfx = VfxType.None)
    {
        await caster.WaitForObjectContext();

        targetObject.ApplyEffect(EffectDuration.Instant, Effect.Damage(damageRoll, damageType));
        targetObject.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(impVfx));
    }
}
