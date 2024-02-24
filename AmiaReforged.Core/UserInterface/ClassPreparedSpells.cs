namespace AmiaReforged.Core.UserInterface;

public class ClassPreparedSpells
{
    public string Class { get; set; }

    public bool IsInnate { get; set; }
    public Dictionary<byte, IReadOnlyList<PreparedSpellModel>> Spells { get; } = new();

    public Dictionary<byte, byte> InnateSpellUses { get; init; } = new();
}