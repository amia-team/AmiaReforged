using AmiaReforged.Classes.Warlock.Constants;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.Classes.Warlock.EldritchBlast.Essence.Essences;

[ServiceBinding(typeof(IEssence))]
public class BrimstoneEssence(ScriptHandleFactory scriptHandleFactory) : IEssence
{
    public EssenceType Essence => EssenceType.Brimstone;

    public EssenceData GetEssenceData(int invocationCl, NwCreature warlock) => new
    (
        Type: Essence,
        DamageType: DamageType.Fire,
        SavingThrow: SavingThrow.Reflex,
        SavingThrowType: SavingThrowType.Fire,
        DmgImpVfx: VfxType.ImpFlameS,
        BeamVfx: VfxType.BeamFire,
        DoomVfx: WarlockVfx.FnfDoomFire,
        PulseVfx: WarlockVfx.ImpPulseFire,
        Effect: BrimstoneEffect(warlock),
        Duration: EssenceDuration(invocationCl)
    );

    private static TimeSpan EssenceDuration(int invocationCl)
    {
        int rounds = Math.Max(1, invocationCl / 5);
        return NwTimeSpan.FromRounds(rounds);
    }

    private Effect BrimstoneEffect(NwCreature warlock)
    {
        ScriptCallbackHandle burn = scriptHandleFactory.CreateUniqueHandler(info => Burn(info, warlock));
        TimeSpan oneRound = NwTimeSpan.FromRounds(1);

        Effect brimstoneEffect = Effect.LinkEffects
        (
            Effect.RunAction(onRemovedHandle: burn, onIntervalHandle: burn, interval: oneRound),
            Effect.VisualEffect(VfxType.DurInfernoChest)
        );

        return brimstoneEffect;
    }

    private static ScriptHandleResult Burn(CallInfo info, NwCreature warlock)
    {
        if (info.ObjectSelf is not NwCreature targetCreature || targetCreature.IsDead)
            return ScriptHandleResult.Handled;

        _ = ApplyBurn(targetCreature, warlock);
        return ScriptHandleResult.Handled;
    }

    private static async Task ApplyBurn(NwCreature targetCreature, NwCreature warlock)
    {
        await warlock.WaitForObjectContext();

        int damageRoll = Random.Shared.Roll(6, 2);
        Effect burnEffect = Effect.Damage(damageRoll, DamageType.Fire);
        targetCreature.ApplyEffect(EffectDuration.Instant, burnEffect);
        targetCreature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpFlameS));
    }
}
