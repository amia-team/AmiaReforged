using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Platform;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Events;

[GlyphNode]
public partial class OnCreatureSpawnEventExecutor : GlyphEventNode
{
    public const string NodeTypeId = "event.on_creature_spawn";
    public static GlyphEventDescriptor Event { get; } = new("encounter.on_creature_spawn", GlyphEventType.OnCreatureSpawn,
        GlyphScriptCategory.Encounter, NodeTypeId, Capabilities: [typeof(EncounterGlyphContext)]);
    public static GlyphContextSchema Context { get; } = new([
        new("creature", "Creature", GlyphDataType.NwObject,
            ctx => ctx.Get<EncounterGlyphContext>() is { } data ? data.SpawnedCreature : 0u),
        new("creature_resref", "Creature ResRef", GlyphDataType.String,
            ctx => ctx.Get<EncounterGlyphContext>() is { } data ? data.CreatureResRef ?? string.Empty : string.Empty),
        new("spawn_index", "Spawn Index", GlyphDataType.Int,
            ctx => ctx.Get<EncounterGlyphContext>() is { } data ? data.SpawnIndex : 0),
        new("total_count", "Total Count", GlyphDataType.Int,
            ctx => ctx.Get<EncounterGlyphContext>() is { } data ? data.TotalGroupSpawnCount : 0),
        new("party_size", "Party Size", GlyphDataType.Int,
            ctx => ctx.Get<EncounterGlyphContext>() is { } data ? data.EncounterContext?.PartySize ?? 0 : 0, Aliases: ["party.size"]),
        new("area_resref", "Area ResRef", GlyphDataType.String,
            ctx => ctx.Get<EncounterGlyphContext>() is { } data ? data.EncounterContext?.AreaResRef ?? string.Empty : string.Empty),
        new("game_time", "Game Time (hours)", GlyphDataType.Float,
            ctx => ctx.Get<EncounterGlyphContext>() is { } data ? data.EncounterContext?.GameTime.TotalHours ?? 0.0 : 0.0, Aliases: ["time.hour"]),
        new("danger", "Chaos: Danger", GlyphDataType.Int,
            ctx => ctx.Get<EncounterGlyphContext>() is { } data ? data.EncounterContext?.Chaos.Danger ?? 0 : 0),
        new("corruption", "Chaos: Corruption", GlyphDataType.Int,
            ctx => ctx.Get<EncounterGlyphContext>() is { } data ? data.EncounterContext?.Chaos.Corruption ?? 0 : 0),
        new("density", "Chaos: Density", GlyphDataType.Int,
            ctx => ctx.Get<EncounterGlyphContext>() is { } data ? data.EncounterContext?.Chaos.Density ?? 0 : 0),
        new("mutation", "Chaos: Mutation", GlyphDataType.Int,
            ctx => ctx.Get<EncounterGlyphContext>() is { } data ? data.EncounterContext?.Chaos.Mutation ?? 0 : 0),
        new("profile_name", "Profile Name", GlyphDataType.String,
            ctx => ctx.Get<EncounterGlyphContext>() is { } data ? data.Profile?.Name ?? string.Empty : string.Empty),
        new("group_name", "Group Name", GlyphDataType.String,
            ctx => ctx.Get<EncounterGlyphContext>() is { } data ? data.Group?.Name ?? string.Empty : string.Empty),
        new("is_boss", "Is Boss", GlyphDataType.Bool,
            ctx => ctx.Get<EncounterGlyphContext>() is { } data ? data.IsBoss : false),
        new("triggering_player", "Triggering Player", GlyphDataType.NwObject,
            ctx => ctx.Get<EncounterGlyphContext>() is { } data ? data.TriggeringPlayer : 0u, Aliases: ["player"]),
    ]);
    protected override GlyphEventDescriptor EventContract => Event;
    public override GlyphContextSchema Schema => Context;
    public override string SourceDisplayName => "On Creature Spawn";
    protected override string Description => "Entry point for scripts that run when each creature is spawned, before bonuses " +
                      "and mutations are applied. Use Skip Bonuses / Skip Mutations actions to bypass " +
                      "the data-driven pipeline for this creature.";
}
