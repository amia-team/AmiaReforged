namespace AmiaReforged.Core.Models.Sailing;

public sealed class SailingSpellDefinition
{
    public int SpellId { get; init; }

    public string Name { get; init; } =
        string.Empty;

    public SailingSpellEffect Effect { get; init; }

    public int Power { get; init; }

    public float Range { get; init; }

    public bool CanTargetShip { get; init; }

    public bool CanTargetCrew { get; init; }
}