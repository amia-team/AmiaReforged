using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Events;

/// <summary>
/// Entry-point node for <see cref="GlyphEventType.AfterGroupSpawn"/> graphs.
/// Exposes the spawned creature list and encounter context as output pins.
/// </summary>
public class AfterGroupSpawnEventExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "event.after_group_spawn";

    public string TypeId => NodeTypeId;

    public Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        var outputs = new Dictionary<string, object?>
        {
            ["spawned_creatures"] = context.SpawnedCreatures.ToList(),
            ["spawn_count"] = context.SpawnedCreatures.Count,
            ["party_size"] = context.EncounterContext!.PartySize,
            ["area_resref"] = context.EncounterContext.AreaResRef,
            ["game_time"] = context.EncounterContext.GameTime.TotalHours,
            ["danger"] = context.EncounterContext.Chaos.Danger,
            ["corruption"] = context.EncounterContext.Chaos.Corruption,
            ["density"] = context.EncounterContext.Chaos.Density,
            ["mutation"] = context.EncounterContext.Chaos.Mutation,
            ["profile_name"] = context.Profile!.Name,
            ["group_name"] = context.Group?.Name ?? string.Empty
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
    public static GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "After Group Spawn",
        Category = "Events",
        Description = "Entry point for scripts that run after a spawn group's creatures are placed in the world. " +
                      "Can modify spawned creatures, apply effects, or trigger interactions.",
        ColorClass = "node-event",
        IsSingleton = true,
        RestrictToEventType = GlyphEventType.AfterGroupSpawn,
        InputPins = [],
        OutputPins =
        [
            new GlyphPin { Id = "exec_out", Name = "Execute", DataType = GlyphDataType.Exec, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "spawned_creatures", Name = "Spawned Creatures", DataType = GlyphDataType.List, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "spawn_count", Name = "Spawn Count", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "party_size", Name = "Party Size", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "area_resref", Name = "Area ResRef", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "game_time", Name = "Game Time (hours)", DataType = GlyphDataType.Float, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "danger", Name = "Chaos: Danger", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "corruption", Name = "Chaos: Corruption", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "density", Name = "Chaos: Density", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "mutation", Name = "Chaos: Mutation", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "profile_name", Name = "Profile Name", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "group_name", Name = "Group Name", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Output }
        ]
    };
}
