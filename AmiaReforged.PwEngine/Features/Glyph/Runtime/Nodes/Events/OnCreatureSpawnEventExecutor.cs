using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Events;

/// <summary>
/// Entry-point node for <see cref="GlyphEventType.OnCreatureSpawn"/> graphs.
/// Fires once per creature immediately after spawn, before bonuses and mutations.
/// Exposes the creature, its blueprint ResRef, spawn index, and full encounter context.
/// </summary>
public class OnCreatureSpawnEventExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "event.on_creature_spawn";

    public string TypeId => NodeTypeId;

    public Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        Dictionary<string, object?> outputs = new Dictionary<string, object?>
        {
            ["creature"] = context.SpawnedCreature,
            ["creature_resref"] = context.CreatureResRef ?? string.Empty,
            ["spawn_index"] = context.SpawnIndex,
            ["total_count"] = context.TotalGroupSpawnCount,
            ["party_size"] = context.EncounterContext!.PartySize,
            ["area_resref"] = context.EncounterContext.AreaResRef,
            ["game_time"] = context.EncounterContext.GameTime,
            ["danger"] = context.EncounterContext.Chaos.Danger,
            ["corruption"] = context.EncounterContext.Chaos.Corruption,
            ["density"] = context.EncounterContext.Chaos.Density,
            ["mutation"] = context.EncounterContext.Chaos.Mutation,
            ["profile_name"] = context.Profile!.Name,
            ["group_name"] = context.Group?.Name ?? string.Empty,
            ["is_boss"] = context.IsBoss,
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
    public static GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "On Creature Spawn",
        Category = "Events",
        Description = "Entry point for scripts that run when each creature is spawned, before bonuses " +
                      "and mutations are applied. Use Skip Bonuses / Skip Mutations actions to bypass " +
                      "the data-driven pipeline for this creature.",
        ColorClass = "node-event",
        Archetype = GlyphNodeArchetype.EventEntry,
        IsSingleton = true,
        RestrictToEventType = GlyphEventType.OnCreatureSpawn,
        InputPins = [],
        OutputPins =
        [
            new GlyphPin { Id = "exec_out", Name = "Execute", DataType = GlyphDataType.Exec, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "creature", Name = "Creature", DataType = GlyphDataType.NwObject, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "creature_resref", Name = "Creature ResRef", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "spawn_index", Name = "Spawn Index", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "total_count", Name = "Total Count", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "party_size", Name = "Party Size", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "area_resref", Name = "Area ResRef", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "game_time", Name = "Game Time (hours)", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "danger", Name = "Chaos: Danger", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "corruption", Name = "Chaos: Corruption", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "density", Name = "Chaos: Density", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "mutation", Name = "Chaos: Mutation", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "profile_name", Name = "Profile Name", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "group_name", Name = "Group Name", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "is_boss", Name = "Is Boss", DataType = GlyphDataType.Bool, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "triggering_player", Name = "Triggering Player", DataType = GlyphDataType.NwObject, Direction = GlyphPinDirection.Output }
        ]
    };
}
