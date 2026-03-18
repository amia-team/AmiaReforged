using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Events;

/// <summary>
/// Entry-point node for <see cref="GlyphEventType.OnCreatureDeath"/> graphs.
/// Exposes the dead creature, killer, and encounter context as output pins.
/// </summary>
public class OnCreatureDeathEventExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "event.on_creature_death";

    public string TypeId => NodeTypeId;

    public Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        Dictionary<string, object?> outputs = new Dictionary<string, object?>
        {
            ["dead_creature"] = context.DeadCreature,
            ["killer"] = context.Killer,
            ["party_size"] = context.EncounterContext!.PartySize,
            ["area_resref"] = context.EncounterContext.AreaResRef,
            ["danger"] = context.EncounterContext.Chaos.Danger,
            ["corruption"] = context.EncounterContext.Chaos.Corruption,
            ["density"] = context.EncounterContext.Chaos.Density,
            ["mutation"] = context.EncounterContext.Chaos.Mutation,
            ["profile_name"] = context.Profile!.Name,
            ["group_name"] = context.Group?.Name ?? string.Empty,
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
        DisplayName = "On Creature Death",
        Category = "Events",
        Description = "Entry point for scripts that run when a dynamically spawned creature dies. " +
                      "Provides the dead creature, killer, and encounter context.",
        ColorClass = "node-event",
        Archetype = GlyphNodeArchetype.EventEntry,
        IsSingleton = true,
        RestrictToEventType = GlyphEventType.OnCreatureDeath,
        InputPins = [],
        OutputPins =
        [
            new GlyphPin { Id = "exec_out", Name = "Execute", DataType = GlyphDataType.Exec, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "dead_creature", Name = "Dead Creature", DataType = GlyphDataType.NwObject, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "killer", Name = "Killer", DataType = GlyphDataType.NwObject, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "party_size", Name = "Party Size", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "area_resref", Name = "Area ResRef", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "danger", Name = "Chaos: Danger", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "corruption", Name = "Chaos: Corruption", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "density", Name = "Chaos: Density", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "mutation", Name = "Chaos: Mutation", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "profile_name", Name = "Profile Name", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "group_name", Name = "Group Name", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "triggering_player", Name = "Triggering Player", DataType = GlyphDataType.NwObject, Direction = GlyphPinDirection.Output }
        ]
    };
}
