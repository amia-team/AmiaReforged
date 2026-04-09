using AmiaReforged.Classes.EffectUtils;
using AmiaReforged.Classes.Warlock;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Spells.Invocations.Pact;

[ServiceBinding(typeof(IInvocation))]
public class BindingOfMaggots(ScriptHandleFactory scriptHandleFactory) : IInvocation
{
    private const string FiendSummonResRef = "wlkfiend";
    private const int VfxPerEvilSymbolId = 60;

    public string ImpactScript => "wlk_bindingmag";
    public void CastInvocation(NwCreature warlock, int invocationCl, SpellEvents.OnSpellCast castData)
    {
        if (castData.TargetLocation is not { } location) return;

        int invocationDc = warlock.InvocationDc(invocationCl);

        Effect paralysis = Effect.LinkEffects(
            Effect.VisualEffect(VfxType.DurParalyzed),
            Effect.Paralyze()
        );

        TimeSpan summonDuration = WarlockExtensions.PactSummonDuration(invocationCl);
        int summonCount = 1 + invocationCl / 5;
        VisualEffectTableEntry summonVfx = NwGameTables.VisualEffectTable.GetRow((int)VfxType.ImpDestruction);
        Effect summonEffect = Effect.SummonCreature(FiendSummonResRef, summonVfx, unsummonVfx: summonVfx);
        summonEffect.SubType = EffectSubType.Magical;

        ScriptCallbackHandle onEnterMaggot = scriptHandleFactory.CreateUniqueHandler(info =>
            BindWithMaggots(info, warlock, invocationCl, invocationDc, paralysis, castData.Spell,
                location, summonCount, summonDuration, summonEffect));

        PersistentVfxTableEntry persistentVfx = NwGameTables.PersistentEffectTable.GetRow(VfxPerEvilSymbolId);
        Effect bindingOfMaggots = Effect.AreaOfEffect(persistentVfx, onEnterMaggot);
        bindingOfMaggots.SubType = EffectSubType.Magical;

        TimeSpan duration = NwTimeSpan.FromTurns(1);

        location.RemoveAoeSpell(warlock, castData.Spell, RadiusSize.Large);
        location.ApplyEffect(EffectDuration.Temporary, bindingOfMaggots, duration);
    }

    private static ScriptHandleResult BindWithMaggots(CallInfo info, NwCreature warlock, int invocationCl,
        int invocationDc, Effect paralysis, NwSpell spell, Location location, int summonCount,
        TimeSpan summonDuration, Effect summonEffect)
    {
        if (!info.TryGetEvent(out AreaOfEffectEvents.OnEnter? eventData)
            || eventData.Entering is not NwCreature creature
            || !creature.IsValidInvocationTarget(warlock, hurtSelf: false))
            return ScriptHandleResult.Handled;

        CreatureEvents.OnSpellCastAt.Signal(warlock, creature, spell);

        if (warlock.InvocationResistCheck(creature, invocationCl))
            return ScriptHandleResult.Handled;

        SavingThrowResult willSave =
            creature.RollSavingThrow(SavingThrow.Will, invocationDc, SavingThrowType.Spell, warlock);

        if (willSave == SavingThrowResult.Success)
        {
            creature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpWillSavingThrowUse));
            return ScriptHandleResult.Handled;
        }

        if (willSave == SavingThrowResult.Failure)
        {
            creature.ApplyEffect(EffectDuration.Temporary, paralysis, NwTimeSpan.FromRounds(1));
        }

        if (warlock.HasPactCooldown())
            return ScriptHandleResult.Handled;

        location.SummonMany(warlock, summonCount, RadiusSize.Medium, delayMin: 1f, delayMax: 2f, summonEffect,
            summonDuration);

        warlock.ApplyPactCooldown();

        return ScriptHandleResult.Handled;
    }
}
