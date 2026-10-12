using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Core.Services.Sailing;

/// <summary>Routes actual engine spell impacts without replacing the Classes dispatcher or its registrations.</summary>
[ServiceBinding(typeof(IScriptDispatcher))]
[ServiceBinding(typeof(ShipNativeSpellDispatcher))]
public sealed class ShipNativeSpellDispatcher : IScriptDispatcher
{
    private readonly ShipSpellService _spells;
    private readonly Dictionary<string, int> _scripts = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private bool _initialized;
    public int ExecutionOrder => -100;

    public ShipNativeSpellDispatcher(ShipSpellService spells)
    {
        _spells = spells;
    }

    internal void InitializeModule()
    {
        if (_initialized) return;
        _scripts.Clear();
        foreach (Spell type in new[] { Spell.Fireball, Spell.LightningBolt, Spell.GustOfWind })
        {
            NwSpell? spell = NwSpell.FromSpellType(type);
            if (!string.IsNullOrWhiteSpace(spell?.ImpactScript)) _scripts[spell.ImpactScript] = spell.Id;
            else Log.Error("Sailing native spell {0} has no configured impact script.", type);
        }
        _initialized = true;
    }

    public ScriptHandleResult ExecuteScript(string scriptName, uint oidSelf)
    {
        if (!_scripts.TryGetValue(scriptName, out int spellId)) return ScriptHandleResult.NotHandled;
        // Unrelated casts retain their original NWScript/Classes behavior.
        SpellEvents.OnSpellCast impact = new();
        if (impact.Spell?.Id != spellId || impact.Item != null || impact.Caster is not NwCreature caster ||
            !caster.IsPlayerControlled(out NwPlayer? player) || !_spells.TryGetNativeSource(player, out _))
            return ScriptHandleResult.NotHandled;
        try { _spells.ProcessNativeSpellImpact(impact); }
        catch (Exception ex) { Log.Error(ex, "Native sailing spell impact failed for {0}.", player.PlayerName); }
        // An aboard sailing cast has exactly one impact path, including rejection
        // after target loss. Never run the ordinary local spell script as a second effect.
        return ScriptHandleResult.Handled;
    }
}
