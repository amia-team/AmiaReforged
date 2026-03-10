using AmiaReforged.Classes.Warlock.Constants;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.Classes.Warlock.EldritchBlast.Essence.Essences;

[ServiceBinding(typeof(IEssence))]
public class BrimstoneEssence(ScriptHandleFactory scriptHandleFactory) : IEssence
{
    public EssenceType Essence => EssenceType.Brimstone;

    public EssenceData GetEssenceData(int warlockLevel) => new
    (
        Type: Essence,
        DamageType: DamageType.Fire,
        SavingThrow: SavingThrow.Reflex,
        SavingThrowType: SavingThrowType.Fire,
        DmgImpVfx: VfxType.ImpFlameS,
        BeamVfx: VfxType.BeamFire,
        DoomVfx: WarlockVfx.FnfDoomFire,
        PulseVfx: WarlockVfx.ImpPulseFire,
        Effect: BrimstoneEffect(),
        Duration: EssenceDuration(warlockLevel)
    );

    private static TimeSpan EssenceDuration(int warlockLevel)
    {
        int rounds = Math.Max(1, warlockLevel / 5);
        return NwTimeSpan.FromRounds(rounds);
    }

    private Effect BrimstoneEffect()
    {
        ScriptCallbackHandle burn = scriptHandleFactory.CreateUniqueHandler(Burn);
        TimeSpan oneRound = NwTimeSpan.FromRounds(1);

        Effect brimstoneEffect = Effect.LinkEffects
        (
            Effect.RunAction(onAppliedHandle: burn, onRemovedHandle: burn, onIntervalHandle: burn, interval: oneRound),
            Effect.VisualEffect(VfxType.DurInfernoChest)
        );

        return brimstoneEffect;
    }

    private static ScriptHandleResult Burn(CallInfo info)
    {
        if (info.ObjectSelf is not NwCreature targetCreature || targetCreature.IsDead)
            return ScriptHandleResult.Handled;

        int damageRoll = Random.Shared.Roll(6, 2);
        Effect burnEffect = Effect.Damage(damageRoll, DamageType.Fire);

        targetCreature.ApplyEffect(EffectDuration.Instant, burnEffect);
        targetCreature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpFlameS));
        return ScriptHandleResult.Handled;
    }
}
