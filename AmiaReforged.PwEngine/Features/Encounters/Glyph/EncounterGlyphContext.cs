using AmiaReforged.PwEngine.Features.Encounters.Models;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime;

/// <summary>Typed domain state. Legacy execution-context properties forward to this same instance.</summary>
public sealed class EncounterGlyphContext
{
    public EncounterContext? EncounterContext { get; set; }
    public SpawnProfile? Profile { get; set; }
    public SpawnGroup? Group { get; set; }
    public int SpawnCount { get; set; }
    public bool ShouldCancelSpawn { get; set; }
    public List<uint> SpawnedCreatures { get; set; } = [];
    public uint TriggeringPlayer { get; set; }
    public uint DeadCreature { get; set; }
    public uint Killer { get; set; }
    public uint SpawnedCreature { get; set; }
    public string? CreatureResRef { get; set; }
    public int SpawnIndex { get; set; }
    public int TotalGroupSpawnCount { get; set; }
    public bool ShouldSkipBonuses { get; set; }
    public bool ShouldSkipMutations { get; set; }
    public bool IsBoss { get; set; }
}
