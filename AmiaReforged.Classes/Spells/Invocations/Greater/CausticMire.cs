using AmiaReforged.Classes.Warlock;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Spells.Invocations.Greater;

[ServiceBinding(typeof(IInvocation))]
public class CausticMire(ScriptHandleFactory scriptHandleFactory) : IInvocation
{
    private const int VfxPerCaustMireId = 49;

    public string ImpactScript => "wlk_causticmire";
    public void CastInvocation(NwCreature warlock, int warlockLevel, SpellEvents.OnSpellCast castData)
    {
        if (castData.TargetLocation is not { } location) return;

        Effect causticSlow = Effect.LinkEffects
        (
            Effect.MovementSpeedDecrease(50),
        Effect.VisualEffect(VfxType.DurCessateNegative),
            Effect.DamageImmunityDecrease(DamageType.Fire, 10)
        );
        causticSlow.SubType = EffectSubType.Magical;

        int chaMod = warlock.GetAbilityModifier(Ability.Charisma);

        ScriptCallbackHandle onEnterMire = scriptHandleFactory.CreateUniqueHandler(info =>
            OnEnterMire(info, warlock, warlockLevel, causticSlow, chaMod, castData.Spell));
        ScriptCallbackHandle onHeartbeatMire = scriptHandleFactory.CreateUniqueHandler(info =>
            OnHeartbeatMire(info, warlock, warlockLevel, chaMod, castData.Spell));
        ScriptCallbackHandle onExitMire = scriptHandleFactory.CreateUniqueHandler(info =>
            OnExitMire(info, warlock, castData.Spell));

        PersistentVfxTableEntry persistentVfx = NwGameTables.PersistentEffectTable.GetRow(VfxPerCaustMireId);
        Effect causticMire = Effect.AreaOfEffect(persistentVfx, onEnterMire, onHeartbeatMire, onExitMire);
        TimeSpan duration = NwTimeSpan.FromRounds(warlockLevel);

        location.RemoveAoeSpell(warlock, castData.Spell, RadiusSize.Huge);
        location.ApplyEffect(EffectDuration.Temporary, causticMire, duration);
    }

    private static ScriptHandleResult OnEnterMire(CallInfo info, NwCreature warlock, int warlockLevel, Effect causticSlow,
        int chaMod, NwSpell spell)
    {
        if (!info.TryGetEvent(out AreaOfEffectEvents.OnEnter? eventData)
            || eventData.Entering is not NwCreature creature
            || !creature.IsValidInvocationTarget(warlock))
            return ScriptHandleResult.Handled;

        CreatureEvents.OnSpellCastAt.Signal(warlock, creature, spell);

        if (warlock.InvocationResistCheck(creature, warlockLevel))
            return ScriptHandleResult.Handled;

        creature.ApplyEffect(EffectDuration.Permanent, causticSlow);
        int damage = CalculateDamage(chaMod);
        _ = ApplyDamage(creature, warlock, damage);

        return ScriptHandleResult.Handled;
    }

    private static ScriptHandleResult OnHeartbeatMire(CallInfo info, NwCreature warlock, int warlockLevel, int chaMod,
        NwSpell spell)
    {
        if (!info.TryGetEvent(out AreaOfEffectEvents.OnHeartbeat? eventData))
            return ScriptHandleResult.Handled;

        foreach (NwCreature creature in eventData.Effect.GetObjectsInEffectArea<NwCreature>())
        {
            if (!creature.IsValidInvocationTarget(warlock))
                continue;

            CreatureEvents.OnSpellCastAt.Signal(warlock, creature, spell);

            if (warlock.InvocationResistCheck(creature, warlockLevel))
                continue;

            int damage = CalculateDamage(chaMod);
            _ = ApplyDamage(creature, warlock, damage);
        }

        return ScriptHandleResult.Handled;
    }

    private static ScriptHandleResult OnExitMire(CallInfo info, NwCreature warlock, NwSpell spell)
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

    private static int CalculateDamage(int chaMod) => Random.Shared.Roll(6) + chaMod;

    private static async Task ApplyDamage(NwCreature creature, NwCreature warlock, int damage)
    {
        await warlock.WaitForObjectContext();
        Effect damageEffect = Effect.Damage(damage, DamageType.Acid);
        creature.ApplyEffect(EffectDuration.Instant, damageEffect);
    }
}
