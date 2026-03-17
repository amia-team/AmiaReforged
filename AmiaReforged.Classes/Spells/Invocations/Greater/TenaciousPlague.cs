using AmiaReforged.Classes.Warlock;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Spells.Invocations.Greater;

[ServiceBinding(typeof(IInvocation))]
public class TenaciousPlague(ScriptHandleFactory scriptHandleFactory) : IInvocation
{
    private const int VfxPerWlkSwarmId = 50;

    public string ImpactScript => "wlk_tenplague";
    public void CastInvocation(NwCreature warlock, int invocationCl, SpellEvents.OnSpellCast castData)
    {
        if (castData.TargetLocation is not { } location) return;

        Effect slow = Effect.LinkEffects
        (
            Effect.MovementSpeedDecrease(50),
        Effect.VisualEffect(VfxType.DurCessateNegative)
        );
        slow.SubType = EffectSubType.Magical;

        int chaMod = warlock.GetAbilityModifier(Ability.Charisma);

        ScriptCallbackHandle onEnterPlague = scriptHandleFactory.CreateUniqueHandler(info =>
            OnEnterPlague(info, warlock, slow, chaMod, castData.Spell));
        ScriptCallbackHandle onHeartbeatPlague = scriptHandleFactory.CreateUniqueHandler(info =>
            OnHeartbeatPlague(info, warlock, chaMod, castData.Spell));
        ScriptCallbackHandle onExitPlague = scriptHandleFactory.CreateUniqueHandler(info =>
            OnExitPlague(info, warlock, castData.Spell));

        PersistentVfxTableEntry persistentVfx = NwGameTables.PersistentEffectTable.GetRow(VfxPerWlkSwarmId);
        Effect tenaciousPlague = Effect.AreaOfEffect(persistentVfx, onEnterPlague, onHeartbeatPlague, onExitPlague);

        TimeSpan duration = NwTimeSpan.FromRounds(3);

        location.RemoveAoeSpell(warlock, castData.Spell, RadiusSize.Huge);
        location.ApplyEffect(EffectDuration.Temporary, tenaciousPlague, duration);
    }

    private static ScriptHandleResult OnEnterPlague(CallInfo info, NwCreature warlock, Effect slow, int chaMod,
        NwSpell spell)
    {
        if (!info.TryGetEvent(out AreaOfEffectEvents.OnEnter? eventData)
            || eventData.Entering is not NwCreature creature
            || !creature.IsValidInvocationTarget(warlock))
            return ScriptHandleResult.Handled;

        CreatureEvents.OnSpellCastAt.Signal(warlock, creature, spell);

        creature.ApplyEffect(EffectDuration.Permanent, slow);
        int damage = CalculateDamage(chaMod);
        _ = ApplyDamage(creature, warlock, damage);

        return ScriptHandleResult.Handled;
    }

    private static ScriptHandleResult OnHeartbeatPlague(CallInfo info, NwCreature warlock, int chaMod, NwSpell spell)
    {
        if (!info.TryGetEvent(out AreaOfEffectEvents.OnHeartbeat? eventData))
            return ScriptHandleResult.Handled;

        foreach (NwCreature creature in eventData.Effect.GetObjectsInEffectArea<NwCreature>())
        {
            if (!creature.IsValidInvocationTarget(warlock))
                continue;

            CreatureEvents.OnSpellCastAt.Signal(warlock, creature, spell);

            int damage = CalculateDamage(chaMod);
            _ = ApplyDamage(creature, warlock, damage);
        }

        return ScriptHandleResult.Handled;
    }

    private static ScriptHandleResult OnExitPlague(CallInfo info, NwCreature warlock, NwSpell spell)
    {
        if (!info.TryGetEvent(out AreaOfEffectEvents.OnExit? eventData)
            || eventData.Exiting is not NwCreature creature)
            return ScriptHandleResult.Handled;

        foreach (Effect effect in creature.ActiveEffects)
        {
            if (effect.Spell == spell && effect.Creator == warlock)
                creature.RemoveEffect(effect);
        }

        return ScriptHandleResult.Handled;
    }

    private static int CalculateDamage(int chaMod) => Random.Shared.Roll(6, 2) + chaMod;

    private static async Task ApplyDamage(NwCreature creature, NwCreature warlock, int damage)
    {
        await NwTask.Delay(SpellUtils.GetRandomDelay());
        await warlock.WaitForObjectContext();
        Effect damageEffect = Effect.LinkEffects(Effect.Damage(damage),
            Effect.VisualEffect(VfxType.ComBloodCrtRed));
        creature.ApplyEffect(EffectDuration.Instant, damageEffect);
    }
}
