using AmiaReforged.Core.Models.Sailing;
using Anvil.Services;
using NLog;

namespace AmiaReforged.Core.Services.Sailing;

[ServiceBinding(typeof(SailingSpellRegistry))]
public sealed class SailingSpellRegistry
{
    private static readonly Logger Log =
        LogManager.GetCurrentClassLogger();

    private readonly Dictionary<int, SailingSpellDefinition>
        _spells = new();

    public SailingSpellRegistry()
    {
        Log.Info(
            "Sailing Spell Registry initialized.");
    }

    public void Register(
        SailingSpellDefinition spell)
    {
        _spells[spell.SpellId] = spell;

        Log.Info(
            $"Registered sailing spell: " +
            $"{spell.Name} ({spell.SpellId}).");
    }

    public SailingSpellDefinition? GetSpell(
        int spellId)
    {
        return _spells.TryGetValue(
            spellId,
            out SailingSpellDefinition? spell)
            ? spell
            : null;
    }

    public bool TryGetSpell(
        int spellId,
        out SailingSpellDefinition? spell)
    {
        return _spells.TryGetValue(
            spellId,
            out spell);
    }

    public IReadOnlyCollection<SailingSpellDefinition>
        GetSpells()
    {
        return _spells.Values;
    }
}