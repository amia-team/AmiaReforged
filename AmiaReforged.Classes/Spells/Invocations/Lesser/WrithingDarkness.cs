using AmiaReforged.Classes.Warlock;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;

namespace AmiaReforged.Classes.Spells.Invocations.Lesser;

[ServiceBinding(typeof(IInvocation))]
public class WrithingDarkness(ScriptHandleFactory scriptHandleFactory) : IInvocation
{
    private const string DarknessProhibitedVar = "NO_DARKNESS";
    private const int VfxPerWlkDarkId = 48;

    public string ImpactScript => "wlk_insid_shadws";
    public void CastInvocation(NwCreature warlock, int warlockLevel, SpellEvents.OnSpellCast castData)
    {
        if (castData.TargetLocation is not { } location
            || IsDarknessProhibited(warlock)) return;

        int chaMod = warlock.GetAbilityModifier(Ability.Charisma);
        int dc = warlock.InvocationDc(warlockLevel);
        Effect blindness = Effect.LinkEffects(Effect.Blindness(), Effect.VisualEffect(VfxType.DurCessateNegative));
        blindness.SubType = EffectSubType.Magical;

        ScriptCallbackHandle onEnterWrithing = scriptHandleFactory.CreateUniqueHandler(info
            => OnEnterWrithing(info, warlock, chaMod, warlockLevel, dc, blindness, castData.Spell));
        ScriptCallbackHandle onHeartbeatWrithing = scriptHandleFactory.CreateUniqueHandler(info
            => OnHeartbeatWrithing(info, warlock, chaMod, warlockLevel, dc, blindness, castData.Spell));

        PersistentVfxTableEntry persistentVfx = NwGameTables.PersistentEffectTable.GetRow(VfxPerWlkDarkId);
        Effect writhingDark = Effect.AreaOfEffect(persistentVfx, onEnterWrithing, onHeartbeatWrithing);
        writhingDark.SubType = EffectSubType.Magical;

        TimeSpan duration = NwTimeSpan.FromRounds(warlockLevel);

        location.RemoveAoeSpell(warlock, castData.Spell, RadiusSize.Huge);
        location.ApplyEffect(EffectDuration.Temporary, writhingDark, duration);
    }

    private static bool IsDarknessProhibited(NwCreature warlock)
    {
        if (warlock.Area == null
            || warlock.Area.GetObjectVariable<LocalVariableInt>(DarknessProhibitedVar).HasNothing)
            return false;

        warlock.ControllingPlayer?.FloatingTextString
            ("The darkness you try to conjure fizzles in this location!", false);

        return true;
    }

    private static ScriptHandleResult OnHeartbeatWrithing(CallInfo info, NwCreature warlock, int chaMod, int warlockLevel,
        int dc, Effect blindness, NwSpell spell)
    {
        if (!info.TryGetEvent(out AreaOfEffectEvents.OnHeartbeat? eventData))
            return ScriptHandleResult.Handled;

        TimeSpan duration = NwTimeSpan.FromRounds(1);

        foreach (NwCreature creature in eventData.Effect.GetObjectsInEffectArea<NwCreature>())
        {
            if (!creature.IsValidInvocationTarget(warlock)) continue;

            CreatureEvents.OnSpellCastAt.Signal(warlock, creature, spell);

            if (creature.ActiveEffects.Any(e => e.EffectType is EffectType.TrueSeeing or EffectType.Ultravision)
                || warlock.InvocationResistCheck(creature, warlockLevel))
                continue;

            BlindnessCheck(creature, warlock, dc, blindness, duration);

            int damage = CalculateDamage(chaMod);

            _ = ApplyDamage(creature, warlock, damage);
        }

        return ScriptHandleResult.Handled;
    }

    private static ScriptHandleResult OnEnterWrithing(CallInfo info, NwCreature warlock, int chaMod, int warlockLevel,
        int dc, Effect blindness, NwSpell spell)
    {
        if (!info.TryGetEvent(out AreaOfEffectEvents.OnEnter? eventData)
            || eventData.Entering is not NwCreature creature
            || !creature.IsValidInvocationTarget(warlock))
            return ScriptHandleResult.Handled;

        CreatureEvents.OnSpellCastAt.Signal(warlock, creature, spell);

        if (creature.ActiveEffects.Any(e => e.EffectType is EffectType.Blindness)
            || warlock.InvocationResistCheck(creature, warlockLevel))
            return ScriptHandleResult.Handled;

        TimeSpan duration = NwTimeSpan.FromRounds(1);
        BlindnessCheck(creature, warlock, dc, blindness, duration);

        int damage = CalculateDamage(chaMod);

        _ = ApplyDamage(creature, warlock, damage);

        return ScriptHandleResult.Handled;
    }

    /// <summary>
    /// Do the will check for blindness and apply effects accordingly
    /// </summary>
    private static void BlindnessCheck(NwCreature creature, NwCreature warlock, int dc, Effect blindness, TimeSpan duration)
    {
        SavingThrowResult willSave = creature.RollSavingThrow(SavingThrow.Will, dc, SavingThrowType.Spell, warlock);
        switch (willSave)
        {
            case SavingThrowResult.Success:
                creature.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(VfxType.ImpWillSavingThrowUse));
                return;
            case SavingThrowResult.Failure:
                creature.ApplyEffect(EffectDuration.Temporary, blindness, duration);
                break;
        }
    }

    private static int CalculateDamage(int chaMod) => Random.Shared.Roll(6) + chaMod;

    private static async Task ApplyDamage(NwCreature creature, NwCreature warlock, int damage)
    {
        await warlock.WaitForObjectContext();
        Effect damageEffect = Effect.Damage(damage);
        creature.ApplyEffect(EffectDuration.Instant, damageEffect);
    }
}
