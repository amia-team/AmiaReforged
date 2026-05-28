using AmiaReforged.Classes.EffectUtils;
using Anvil.API;
using Anvil.Services;

namespace AmiaReforged.Classes.Warlock.EldritchBlast.Essence.Essences;

[ServiceBinding(typeof(IEssence))]
public class GluttonousEssence(ScriptHandleFactory scriptHandleFactory) : IEssence
{
    public EssenceType Essence => EssenceType.Gluttonous;

    public EssenceData GetEssenceData(int invocationCl, NwCreature warlock) => new
    (
        Type: Essence,
        DamageType: DamageType.Negative,
        SavingThrow: SavingThrow.Fortitude,
        SavingThrowType: SavingThrowType.Negative,
        DmgImpVfx: VfxType.ImpNegativeEnergy,
        BeamVfx: VfxType.BeamEvil,
        DoomVfx: AmiaVfxTypes.FnfDoomNegative,
        PulseVfx: AmiaVfxTypes.ImpPulseNegativeChest,
        HideousBlowVfx: ItemVisual.Evil,
        Effect: GluttonousEffect(warlock, invocationCl),
        Duration: EssenceDuration(invocationCl),
        AllowStacking: true
    );

    private Effect GluttonousEffect(NwCreature warlock, int invocationCl)
    {
        TimeSpan duration = EssenceDuration(invocationCl);
        LocalVariableInt gluttonousEssence = warlock.GetObjectVariable<LocalVariableInt>(nameof(EssenceType.Gluttonous));

        ScriptCallbackHandle onApply = scriptHandleFactory.CreateUniqueHandler(info =>
        {
            if (info.ObjectSelf is not NwCreature targetCreature)
                return ScriptHandleResult.Handled;

            targetCreature.ApplyEffect(EffectDuration.Temporary, SaveDecreaseEffect, duration);
            warlock.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(AmiaVfxTypes.ImpMirvNegative));

            warlock.ApplyEffect(EffectDuration.Temporary, Effect.VisualEffect(AmiaVfxTypes.DurOrbRed), duration);
            gluttonousEssence.Value++;

            return ScriptHandleResult.Handled;
        });

        ScriptCallbackHandle onRemove = scriptHandleFactory.CreateUniqueHandler(_ =>
        {
            gluttonousEssence.Value--;
            return ScriptHandleResult.Handled;
        });

        return Effect.RunAction(onAppliedHandle: onApply, onRemovedHandle: onRemove);
    }

    private static Effect SaveDecreaseEffect =>
        Effect.LinkEffects(Effect.SavingThrowDecrease(SavingThrow.All, 2),
            Effect.VisualEffect(VfxType.DurCessateNegative),
            Effect.VisualEffect(VfxType.DurAuraRedDark));

    private static TimeSpan EssenceDuration(int invocationCl)
    {
        int rounds = Math.Max(1, invocationCl / 5);
        return NwTimeSpan.FromRounds(rounds);
    }
}
