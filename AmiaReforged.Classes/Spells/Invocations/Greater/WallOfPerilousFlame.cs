using AmiaReforged.Classes.Warlock;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Spells.Invocations.Greater;

[ServiceBinding(typeof(IInvocation))]
public class WallOfPerilousFlame(ScriptHandleFactory scriptHandleFactory) : IInvocation
{
    private const int VfxPerWlkPerilFlame = 47;
    public string ImpactScript => "wlk_flamewall";
    public void CastInvocation(NwCreature warlock, int invocationCl, SpellEvents.OnSpellCast castData)
    {
        if (castData.TargetLocation is not { } location) return;

        int chaMod = warlock.GetAbilityModifier(Ability.Charisma);

        ScriptCallbackHandle onEnterFlame = scriptHandleFactory.CreateUniqueHandler(info =>
            OnEnterFlame(info, warlock, invocationCl, chaMod, castData.Spell));
        ScriptCallbackHandle onHeartbeatFlame = scriptHandleFactory.CreateUniqueHandler(info =>
            OnHeartbeatFlame(info, warlock, invocationCl, chaMod, castData.Spell));

        PersistentVfxTableEntry persistentVfx = NwGameTables.PersistentEffectTable.GetRow(VfxPerWlkPerilFlame);
        Effect perilousFlame = Effect.AreaOfEffect(persistentVfx, onEnterFlame, onHeartbeatFlame);

        TimeSpan duration = NwTimeSpan.FromRounds(6);

        location.ApplyEffect(EffectDuration.Temporary, perilousFlame, duration);
    }

    private static ScriptHandleResult OnEnterFlame(CallInfo info, NwCreature warlock, int invocationCl, int chaMod,
        NwSpell spell)
    {
        if (!info.TryGetEvent(out AreaOfEffectEvents.OnEnter? eventData)
            || eventData.Entering is not NwCreature creature
            || !creature.IsValidInvocationTarget(warlock))
            return ScriptHandleResult.Handled;

        CreatureEvents.OnSpellCastAt.Signal(warlock, creature, spell);

        if (warlock.InvocationResistCheck(creature, invocationCl))
            return ScriptHandleResult.Handled;

        _ = ApplyDamage(creature, warlock, chaMod);

        return ScriptHandleResult.Handled;
    }

    private static ScriptHandleResult OnHeartbeatFlame(CallInfo info, NwCreature warlock, int invocationCl, int chaMod,
        NwSpell spell)
    {
        if (!info.TryGetEvent(out AreaOfEffectEvents.OnHeartbeat? eventData))
            return ScriptHandleResult.Handled;

        foreach (NwCreature creature in eventData.Effect.GetObjectsInEffectArea<NwCreature>())
        {
            if (!creature.IsValidInvocationTarget(warlock))
                continue;

            CreatureEvents.OnSpellCastAt.Signal(warlock, creature, spell);

            if (warlock.InvocationResistCheck(creature, invocationCl))
                continue;

            _ = ApplyDamage(creature, warlock, chaMod);
        }

        return ScriptHandleResult.Handled;
    }

    private static int CalculateDamage(int chaMod) => (Random.Shared.Roll(12, 2) + chaMod) / 2;

    private static async Task ApplyDamage(NwCreature creature, NwCreature warlock, int chaMod)
    {
        await NwTask.Delay(SpellUtils.GetRandomDelay(0, 0.4));
        await warlock.WaitForObjectContext();

        int damage = CalculateDamage(chaMod);
        Effect damageEffect = Effect.LinkEffects
        (
            Effect.Damage(damage),
            Effect.Damage(damage, DamageType.Fire),
            Effect.VisualEffect(VfxType.ImpFlameM)
        );
        creature.ApplyEffect(EffectDuration.Instant, damageEffect);
    }
}
