using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Platform;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Events;

[GlyphNode]
public partial class AfterGroupSpawnEventExecutor : GlyphEventNode
{
    public const string NodeTypeId = "event.after_group_spawn";
    public static GlyphEventDescriptor Event { get; } = new("encounter.after_group_spawn", GlyphEventType.AfterGroupSpawn,
        GlyphScriptCategory.Encounter, NodeTypeId, Capabilities: [typeof(EncounterGlyphContext)]);
    public static GlyphContextSchema Context { get; } = new([
        new("spawned_creatures", "Spawned Creatures", GlyphDataType.List,
            ctx => ctx.Get<EncounterGlyphContext>() is { } data ? data.SpawnedCreatures.ToList() : new List<uint>()),
        new("spawn_count", "Spawn Count", GlyphDataType.Int,
            ctx => ctx.Get<EncounterGlyphContext>() is { } data ? data.SpawnedCreatures.Count : 0, Aliases: ["spawn.count"]),
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
        new("triggering_player", "Triggering Player", GlyphDataType.NwObject,
            ctx => ctx.Get<EncounterGlyphContext>() is { } data ? data.TriggeringPlayer : 0u, Aliases: ["player"]),
    ]);
    protected override GlyphEventDescriptor EventContract => Event;
    public override GlyphContextSchema Schema => Context;
    public override string SourceDisplayName => "After Group Spawn";
    protected override string Description => "Entry point for scripts that run after a spawn group's creatures are placed in the world. " +
                      "Can modify spawned creatures, apply effects, or trigger interactions.";
}
