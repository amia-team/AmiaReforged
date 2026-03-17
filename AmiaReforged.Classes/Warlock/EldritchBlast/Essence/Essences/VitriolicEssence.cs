using AmiaReforged.Classes.Warlock.Constants;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.Classes.Warlock.EldritchBlast.Essence.Essences;

[ServiceBinding(typeof(IEssence))]
public class VitriolicEssence(ScriptHandleFactory scriptHandleFactory) : IEssence
{
    public EssenceType Essence => EssenceType.Vitriolic;

    public EssenceData GetEssenceData(int invocationCl, NwCreature warlock) => new
    (
        Type: Essence,
        DamageType: DamageType.Acid,
        SavingThrow: SavingThrow.Fortitude,
        SavingThrowType: SavingThrowType.Acid,
        DmgImpVfx: VfxType.ImpAcidS,
        BeamVfx: VfxType.BeamDisintegrate,
        DoomVfx: WarlockVfx.FnfDoomAcid,
        PulseVfx: WarlockVfx.ImpPulseNature,
        Effect: VitriolicEffect(warlock),
        AllowStacking: true,
        Duration: EssenceDuration(invocationCl)
    );

    private static TimeSpan EssenceDuration(int invocationCl)
    {
        int rounds = Math.Max(1, invocationCl / 5);
        return NwTimeSpan.FromRounds(rounds);
    }

    private Effect VitriolicEffect(NwCreature warlock)
    {
        ScriptCallbackHandle burn = scriptHandleFactory.CreateUniqueHandler(info => Corrode(info, warlock));
        TimeSpan oneRound = NwTimeSpan.FromRounds(1);

        Effect vitriolicEffect = Effect.LinkEffects
        (
            Effect.RunAction(onRemovedHandle: burn, onIntervalHandle: burn, interval: oneRound),
            Effect.VisualEffect(VfxType.DurAuraGreenDark)
        );

        return vitriolicEffect;
    }

    private static ScriptHandleResult Corrode(CallInfo info, NwCreature warlock)
    {
        if (info.ObjectSelf is not NwCreature targetCreature || targetCreature.IsDead)
            return ScriptHandleResult.Handled;

        _ = ApplyCorrode(targetCreature, warlock);
        return ScriptHandleResult.Handled;
    }

    private static async Task ApplyCorrode(NwCreature targetCreature, NwCreature warlock)
    {
        await warlock.WaitForObjectContext();

        int damageRoll = Random.Shared.Roll(6, 2);
        Effect corrodeEffect = Effect.Damage(damageRoll, DamageType.Acid);
        targetCreature.ApplyEffect(EffectDuration.Instant, corrodeEffect);
        targetCreature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpAcidS));
    }
}
