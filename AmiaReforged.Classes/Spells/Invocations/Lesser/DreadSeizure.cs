using AmiaReforged.Classes.Warlock;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Spells.Invocations.Lesser;

[ServiceBinding(typeof(IInvocation))]
public class DreadSeizure(ScriptHandleFactory scriptHandleFactory) : IInvocation
{
    private const VfxType DurImmobilize = (VfxType)2526;
    private const int MobAuraCustomId = 52;
    public string ImpactScript => "wlk_dreadseizure";
    public void CastInvocation(NwCreature warlock, int warlockLevel, SpellEvents.OnSpellCast castData)
    {
        Effect? dreadSeizureAura =
            warlock.ActiveEffects.FirstOrDefault(e => e.Spell == castData.Spell && e.Creator == warlock);
        if (dreadSeizureAura != null)
            warlock.RemoveEffect(dreadSeizureAura);

        Effect dreadSeizureEffect = Effect.LinkEffects
        (
            Effect.VisualEffect(VfxType.DurCessateNegative),
            Effect.VisualEffect(DurImmobilize),
            Effect.MovementSpeedDecrease(20),
            Effect.AttackDecrease(2)
        );
        dreadSeizureEffect.SubType = EffectSubType.Magical;

        ScriptCallbackHandle onEnterDread = scriptHandleFactory.CreateUniqueHandler(info =>
            OnEnterDread(info, warlock, dreadSeizureEffect, warlockLevel, warlock.InvocationDc(warlockLevel)));

        ScriptCallbackHandle onExitDread = scriptHandleFactory.CreateUniqueHandler(OnExitDread);

        PersistentVfxTableEntry persistentVfx = NwGameTables.PersistentEffectTable.GetRow(MobAuraCustomId);
        dreadSeizureAura = Effect.LinkEffects
        (
            Effect.AreaOfEffect(persistentVfx, onEnterHandle: onEnterDread, onExitHandle: onExitDread),
            Effect.VisualEffect(VfxType.ImpAuraNegativeEnergy)
        );

        TimeSpan duration = NwTimeSpan.FromRounds(warlockLevel);
        warlock.ApplyEffect(EffectDuration.Temporary, dreadSeizureAura, duration);
        warlock.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.FnfPwkill));
    }

    private static ScriptHandleResult OnEnterDread(CallInfo info, NwCreature warlock, Effect dreadSeizureEffect,
        int warlockLevel, int dc)
    {
        if (!info.TryGetEvent(out AreaOfEffectEvents.OnEnter? eventData)
            || eventData.Entering is not NwCreature targetCreature
            || !warlock.IsReactionTypeHostile(targetCreature)
            || eventData.Effect.Spell is not { } spell)
            return ScriptHandleResult.Handled;

        CreatureEvents.OnSpellCastAt.Signal(warlock, targetCreature, spell);

        targetCreature.InvocationResistCheck(warlock, warlockLevel);

        SavingThrowResult fortSave =
            targetCreature.RollSavingThrow(SavingThrow.Fortitude, dc, SavingThrowType.Spell, warlock);

        if (fortSave == SavingThrowResult.Success)
        {
            targetCreature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpFortitudeSavingThrowUse));
            return ScriptHandleResult.Handled;
        }

        targetCreature.ApplyEffect(EffectDuration.Permanent, dreadSeizureEffect);
        return ScriptHandleResult.Handled;
    }

    private static ScriptHandleResult OnExitDread(CallInfo info)
    {
        if (!info.TryGetEvent(out AreaOfEffectEvents.OnExit? eventData)
            || eventData.Exiting is not NwCreature targetCreature)
            return ScriptHandleResult.Handled;

        Effect? existingDreadSeizureEffect =
            targetCreature.ActiveEffects.FirstOrDefault(e => e.Spell == eventData.Effect.Spell);

        if (existingDreadSeizureEffect == null) return ScriptHandleResult.Handled;

        targetCreature.RemoveEffect(existingDreadSeizureEffect);
        return ScriptHandleResult.Handled;
    }
}
