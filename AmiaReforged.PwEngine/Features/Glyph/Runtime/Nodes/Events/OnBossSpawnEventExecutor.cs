using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Events;

/// <summary>
/// Entry-point node for <see cref="GlyphEventType.OnBossSpawn"/> graphs.
/// Fires when a boss or mini-boss creature is spawned, before its bonuses are applied.
/// Exposes the boss creature, blueprint ResRef, and encounter context.
/// </summary>
public class OnBossSpawnEventExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "event.on_boss_spawn";

    public string TypeId => NodeTypeId;

    public Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        var outputs = new Dictionary<string, object?>
        {
            ["creature"] = context.SpawnedCreature,
            ["creature_resref"] = context.CreatureResRef ?? string.Empty,
            ["party_size"] = context.EncounterContext!.PartySize,
            ["area_resref"] = context.EncounterContext.AreaResRef,
            ["danger"] = context.EncounterContext.Chaos.Danger,
            ["corruption"] = context.EncounterContext.Chaos.Corruption,
            ["density"] = context.EncounterContext.Chaos.Density,
            ["mutation"] = context.EncounterContext.Chaos.Mutation,
            ["profile_name"] = context.Profile!.Name,
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
        DisplayName = "On Boss Spawn",
        Category = "Events",
        Description = "Entry point for scripts that run when a boss or mini-boss creature is spawned, " +
                      "before its bonuses are applied. Use Skip Bonuses to bypass the data-driven bonus pipeline.",
        ColorClass = "node-event",
        Archetype = GlyphNodeArchetype.EventEntry,
        IsSingleton = true,
        RestrictToEventType = GlyphEventType.OnBossSpawn,
        InputPins = [],
        OutputPins =
        [
            new GlyphPin { Id = "exec_out", Name = "Execute", DataType = GlyphDataType.Exec, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "creature", Name = "Boss Creature", DataType = GlyphDataType.NwObject, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "creature_resref", Name = "Boss ResRef", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "party_size", Name = "Party Size", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "area_resref", Name = "Area ResRef", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "danger", Name = "Chaos: Danger", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "corruption", Name = "Chaos: Corruption", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "density", Name = "Chaos: Density", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "mutation", Name = "Chaos: Mutation", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "profile_name", Name = "Profile Name", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "triggering_player", Name = "Triggering Player", DataType = GlyphDataType.NwObject, Direction = GlyphPinDirection.Output }
        ]
    };
}
