using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Platform;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Events;

[GlyphNode]
public partial class OnCreatureDeathEventExecutor : GlyphEventNode
{
    public const string NodeTypeId = "event.on_creature_death";
    public static GlyphEventDescriptor Event { get; } = new("encounter.on_creature_death", GlyphEventType.OnCreatureDeath,
        GlyphScriptCategory.Encounter, NodeTypeId, Capabilities: [typeof(EncounterGlyphContext)]);
    public static GlyphContextSchema Context { get; } = new([
        new("dead_creature", "Dead Creature", GlyphDataType.NwObject,
            ctx => ctx.Get<EncounterGlyphContext>() is { } data ? data.DeadCreature : 0u),
        new("killer", "Killer", GlyphDataType.NwObject,
            ctx => ctx.Get<EncounterGlyphContext>() is { } data ? data.Killer : 0u),
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
        new("group_name", "Group Name", GlyphDataType.String,
            ctx => ctx.Get<EncounterGlyphContext>() is { } data ? data.Group?.Name ?? string.Empty : string.Empty),
        new("triggering_player", "Triggering Player", GlyphDataType.NwObject,
            ctx => ctx.Get<EncounterGlyphContext>() is { } data ? data.TriggeringPlayer : 0u, Aliases: ["player"]),
    ]);
    protected override GlyphEventDescriptor EventContract => Event;
    public override GlyphContextSchema Schema => Context;
    public override string SourceDisplayName => "On Creature Death";
    protected override string Description => "Entry point for scripts that run when a dynamically spawned creature dies. " +
                      "Provides the dead creature, killer, and encounter context.";
}
