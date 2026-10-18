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
    private readonly Dictionary<string, HashSet<int>> _scripts = new(StringComparer.OrdinalIgnoreCase);
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
        InitializeBindings(new[] { Spell.Fireball, Spell.LightningBolt, Spell.GustOfWind }.Select(type =>
        {
            NwSpell? spell = NwSpell.FromSpellType(type);
            return (Id: (int)type, Script: spell?.ImpactScript);
        }));
    }

    private void InitializeBindings(IEnumerable<(int Id, string? Script)> bindings)
    {
        if (_initialized) return;
        _scripts.Clear();
        foreach (var binding in bindings)
        {
            if (string.IsNullOrWhiteSpace(binding.Script))
            { Log.Error("Sailing native spell {0} has no configured impact script.", binding.Id); continue; }
            if (!_scripts.TryGetValue(binding.Script, out var spells)) _scripts[binding.Script] = spells = [];
            spells.Add(binding.Id);
            Log.Info("Ship spell binding: spell={0}, script={1}, order={2}.", binding.Id, binding.Script, ExecutionOrder);
        }
        _initialized = _scripts.Count > 0;
    }

    public ScriptHandleResult ExecuteScript(string scriptName, uint oidSelf)
    {
        if (!_scripts.ContainsKey(scriptName)) return ScriptHandleResult.NotHandled;
        // Unrelated casts retain their original NWScript/Classes behavior.
        try
        {
            SpellEvents.OnSpellCast impact = new();
            if (impact.Spell == null || impact.Item != null || impact.Caster is not NwCreature caster ||
                !caster.IsPlayerControlled(out NwPlayer? player)) return ScriptHandleResult.NotHandled;
            bool aboard = _spells.TryGetNativeSource(player, out _);
            Log.Info("Ship spell dispatch: script={0}, self={1}, spell={2}, player={3}, aboard={4}.",
                scriptName, oidSelf, impact.Spell.Id, player.PlayerName, aboard);
            return DispatchImpact(scriptName, impact.Spell.Id, aboard, () => _spells.ProcessNativeSpellImpact(impact));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Native sailing spell context/impact failed: script={0}, self={1}.", scriptName, oidSelf);
            return ScriptHandleResult.NotHandled; // Ownership could not be established; retain the original script path.
        }
    }

    private ScriptHandleResult DispatchImpact(string script, int spellId, bool aboard, Func<bool> process)
    {
        if (!_scripts.TryGetValue(script, out var spells) || !spells.Contains(spellId) || !aboard)
            return ScriptHandleResult.NotHandled;
        try
        {
            bool applied = process();
            Log.Info("Ship spell impact: script={0}, spell={1}, applied={2}.", script, spellId, applied);
        }
        catch (Exception ex) { Log.Error(ex, "Native sailing spell effect failed: script={0}, spell={1}.", script, spellId); }
        // An aboard sailing cast has exactly one impact path, including rejection
        // after target loss. Never run the ordinary local spell script as a second effect.
        return ScriptHandleResult.Handled;
    }
}
