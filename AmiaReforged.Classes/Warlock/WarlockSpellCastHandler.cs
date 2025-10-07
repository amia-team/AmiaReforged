using AmiaReforged.Classes.EffectUtils;
using AmiaReforged.Classes.Spells;
using AmiaReforged.Classes.Warlock.Feats;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Classes.Warlock;

[ServiceBinding(typeof(WarlockSpellCastHandler))]
public class WarlockSpellCastHandler
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private const VfxType SpellFailHandVfx = (VfxType)292;
    private const VfxType SpellFailHeadVfx = (VfxType)293;
    private const int EldritchBlastId = 981;

    public WarlockSpellCastHandler()
    {
        NwModule.Instance.OnSpellCast += OnInvocationCast;
        NwModule.Instance.OnSpellInterrupt += OnInvocationInterrupt;
        Log.Info(message: "Warlock Spell Cast Handler initialized.");
    }

    private void OnInvocationCast(OnSpellCast eventData)
    {
        if (!eventData.Caster.IsPlayerControlled(out NwPlayer player))
        {
            return;
        }

        if (eventData.Caster is not NwCreature warlock)
            return;
        if (eventData.Spell is not { } spell) return;
        if (spell.Id != EldritchBlastId && warlock.Classes[eventData.ClassIndex].Class != WarlockConstants.WarlockClass)
            return;

        // First disable action modes
        foreach (ActionMode actionMode in Enum.GetValues(typeof(ActionMode)))
            warlock.SetActionMode(actionMode, false);

        WarlockSpells.ResetWarlockInvocations(eventData.Caster);

        // Then do arcane spell failure that accounts for Armored Caster feat
        if (warlock.ArcaneSpellFailure <= 0) return;

        int effectiveAsf = ArmoredCaster.CalculateAsf(warlock);

        if (effectiveAsf <= 0) return;

        if (Random.Shared.Roll(100) > effectiveAsf) return;

        eventData.PreventSpellCast = true;

        VfxType spellFailVfx = SpellFailHandVfx;
        if (spell.CastAnim == SpellCastAnimType.Up)
            spellFailVfx = SpellFailHeadVfx;

        warlock.ApplyEffect(EffectDuration.Instant, Effect.VisualEffect(spellFailVfx));

        player.SendServerMessage("Spell failed due to arcane spell failure!");
    }

    private void OnInvocationInterrupt(OnSpellInterrupt eventData)
    {
        if (eventData.InterruptedCaster is not NwCreature warlock) return;
        if (warlock.Classes[eventData.ClassIndex].Class != WarlockConstants.WarlockClass) return;

        WarlockSpells.ResetWarlockInvocations(eventData.InterruptedCaster);
    }

    [ScriptHandler(scriptName: "wlk_el_blst")]
    public void OnEldritchBlasts(CallInfo info)
    {
        EldritchBlasts attack = new();
        attack.CastEldritchBlasts(info.ObjectSelf);
    }
}
