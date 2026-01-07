using Anvil.API;

namespace AmiaReforged.Classes.Spells.SpellLearning;

/// <summary>
/// Represents a spell option in the spell learning interface.
/// </summary>
public class SpellOption
{
    public NwSpell Spell { get; set; } = null!;
    public string DisplayName { get; set; } = string.Empty;
    public int SpellLevel { get; set; }
    public int SpellId { get; set; }
    public bool IsSelected { get; set; }
    public bool AlreadyKnown { get; set; }
}

/// <summary>
/// Represents spell selections organized by spell level.
/// </summary>
public class SpellLevelGroup
{
    public int SpellLevel { get; set; }
    public int MaxSelections { get; set; }
    public int CurrentSelections { get; set; }
    public List<SpellOption> AvailableSpells { get; set; } = new();
}

