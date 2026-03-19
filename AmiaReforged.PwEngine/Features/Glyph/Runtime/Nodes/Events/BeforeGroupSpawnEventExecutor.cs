using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Events;

/// <summary>
/// Entry-point node for <see cref="GlyphEventType.BeforeGroupSpawn"/> graphs.
/// Exposes encounter context data as output pins and a single Exec output to begin the script.
/// Also implements <see cref="IContextNodeProvider"/> to generate wireless context getter nodes.
/// </summary>
public class BeforeGroupSpawnEventExecutor : IGlyphNodeExecutor, IContextNodeProvider
{
    public const string NodeTypeId = "event.before_group_spawn";

    public string TypeId => NodeTypeId;

    // ── IContextNodeProvider ─────────────────────────────────────────────

    public string SourceTypeId => NodeTypeId;

    public string SourceDisplayName => "Before Group Spawn";

    public GlyphEventType? SourceEventType => GlyphEventType.BeforeGroupSpawn;

    public GlyphScriptCategory? SourceScriptCategory => null;

    public List<ContextPinDescriptor> GetContextPins() =>
    [
        new("party_size", "Party Size", GlyphDataType.Int,
            ctx => ctx.EncounterContext?.PartySize ?? 0),
        new("spawn_count", "Spawn Count", GlyphDataType.Int,
            ctx => ctx.SpawnCount),
        new("area_resref", "Area ResRef", GlyphDataType.String,
            ctx => ctx.EncounterContext?.AreaResRef ?? string.Empty),
        new("game_time", "Game Time (hours)", GlyphDataType.Float,
            ctx => ctx.EncounterContext?.GameTime.TotalHours ?? 0.0),
        new("danger", "Chaos: Danger", GlyphDataType.Int,
            ctx => ctx.EncounterContext?.Chaos.Danger ?? 0),
        new("corruption", "Chaos: Corruption", GlyphDataType.Int,
            ctx => ctx.EncounterContext?.Chaos.Corruption ?? 0),
        new("density", "Chaos: Density", GlyphDataType.Int,
            ctx => ctx.EncounterContext?.Chaos.Density ?? 0),
        new("mutation", "Chaos: Mutation", GlyphDataType.Int,
            ctx => ctx.EncounterContext?.Chaos.Mutation ?? 0),
        new("profile_name", "Profile Name", GlyphDataType.String,
            ctx => ctx.Profile?.Name ?? string.Empty),
        new("group_name", "Group Name", GlyphDataType.String,
            ctx => ctx.Group?.Name ?? string.Empty),
        new("is_in_region", "Is In Region", GlyphDataType.Bool,
            ctx => ctx.EncounterContext?.IsInRegion ?? false),
        new("region_tag", "Region Tag", GlyphDataType.String,
            ctx => ctx.EncounterContext?.RegionTag ?? string.Empty),
        new("triggering_player", "Triggering Player", GlyphDataType.NwObject,
            ctx => ctx.TriggeringPlayer),
    ];

    public Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        // Expose encounter data as output values for downstream nodes to consume
        Dictionary<string, object?> outputs = new Dictionary<string, object?>
        {
            ["party_size"] = context.EncounterContext!.PartySize,
            ["area_resref"] = context.EncounterContext.AreaResRef,
            ["game_time"] = context.EncounterContext.GameTime.TotalHours,
            ["danger"] = context.EncounterContext.Chaos.Danger,
            ["corruption"] = context.EncounterContext.Chaos.Corruption,
            ["density"] = context.EncounterContext.Chaos.Density,
            ["mutation"] = context.EncounterContext.Chaos.Mutation,
            ["spawn_count"] = context.SpawnCount,
            ["profile_name"] = context.Profile!.Name,
            ["group_name"] = context.Group?.Name ?? string.Empty,
            ["is_in_region"] = context.EncounterContext!.IsInRegion,
            ["region_tag"] = context.EncounterContext.RegionTag ?? string.Empty,
            ["triggering_player"] = context.TriggeringPlayer
        };

        return Task.FromResult(new GlyphNodeResult
        {
            NextExecPinId = "exec_out",
            OutputValues = outputs
        });
    }

    /// <summary>
    /// Creates the node definition for registration in the registry.
    /// </summary>
    public GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Before Group Spawn",
        Category = "Events",
        Description = "Entry point for scripts that run before a spawn group's creatures are created. " +
                      "Can modify spawn count or cancel the spawn.",
        ColorClass = "node-event",
        Archetype = GlyphNodeArchetype.EventEntry,
        IsSingleton = true,
        RestrictToEventType = GlyphEventType.BeforeGroupSpawn,
        InputPins = [],
        OutputPins =
        [
            new GlyphPin { Id = "exec_out", Name = "Execute", DataType = GlyphDataType.Exec, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "party_size", Name = "Party Size", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "spawn_count", Name = "Spawn Count", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "area_resref", Name = "Area ResRef", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "game_time", Name = "Game Time (hours)", DataType = GlyphDataType.Float, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "danger", Name = "Chaos: Danger", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "corruption", Name = "Chaos: Corruption", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "density", Name = "Chaos: Density", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "mutation", Name = "Chaos: Mutation", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "profile_name", Name = "Profile Name", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "group_name", Name = "Group Name", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "is_in_region", Name = "Is In Region", DataType = GlyphDataType.Bool, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "region_tag", Name = "Region Tag", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "triggering_player", Name = "Triggering Player", DataType = GlyphDataType.NwObject, Direction = GlyphPinDirection.Output }
        ]
    };
}
