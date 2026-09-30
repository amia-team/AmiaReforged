using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Platform;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Events;

[GlyphNode]
public partial class OnBossSpawnEventExecutor : GlyphEventNode
{
    public const string NodeTypeId = "event.on_boss_spawn";
    public static GlyphEventDescriptor Event { get; } = new("encounter.on_boss_spawn", GlyphEventType.OnBossSpawn,
        GlyphScriptCategory.Encounter, NodeTypeId, Capabilities: [typeof(EncounterGlyphContext)]);
    public static GlyphContextSchema Context { get; } = new([
        new("creature", "Boss Creature", GlyphDataType.NwObject,
            ctx => ctx.Get<EncounterGlyphContext>() is { } data ? data.SpawnedCreature : 0u),
        new("creature_resref", "Boss ResRef", GlyphDataType.String,
            ctx => ctx.Get<EncounterGlyphContext>() is { } data ? data.CreatureResRef ?? string.Empty : string.Empty),
        new("party_size", "Party Size", GlyphDataType.Int,
            ctx => ctx.Get<EncounterGlyphContext>() is { } data ? data.EncounterContext?.PartySize ?? 0 : 0, Aliases: ["party.size"]),
        new("area_resref", "Area ResRef", GlyphDataType.String,
            ctx => ctx.Get<EncounterGlyphContext>() is { } data ? data.EncounterContext?.AreaResRef ?? string.Empty : string.Empty),
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
        new("triggering_player", "Triggering Player", GlyphDataType.NwObject,
            ctx => ctx.Get<EncounterGlyphContext>() is { } data ? data.TriggeringPlayer : 0u, Aliases: ["player"]),
    ]);
    protected override GlyphEventDescriptor EventContract => Event;
    public override GlyphContextSchema Schema => Context;
    public override string SourceDisplayName => "On Boss Spawn";
    protected override string Description => "Entry point for scripts that run when a boss or mini-boss creature is spawned, " +
                      "before its bonuses are applied. Use Skip Bonuses to bypass the data-driven bonus pipeline.";
}
