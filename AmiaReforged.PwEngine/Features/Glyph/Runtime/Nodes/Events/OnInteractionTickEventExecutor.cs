using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Events;

/// <summary>
/// Entry-point node for <see cref="GlyphEventType.OnInteractionTick"/> graphs.
/// Fires each round/tick of an active interaction. Provides progress and allows
/// the script to cancel the interaction via the <c>interaction.cancel</c> action node.
/// </summary>
public class OnInteractionTickEventExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "event.on_interaction_tick";

    public string TypeId => NodeTypeId;

    public Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        var outputs = new Dictionary<string, object?>
        {
            ["character_id"] = context.CharacterId ?? string.Empty,
            ["interaction_tag"] = context.InteractionTag ?? string.Empty,
            ["target_id"] = context.InteractionTargetId.ToString(),
            ["area_resref"] = context.InteractionAreaResRef ?? string.Empty,
            ["session_id"] = context.InteractionSessionId.ToString(),
            ["progress"] = context.InteractionProgress,
            ["required_rounds"] = context.InteractionRequiredRounds,
            ["proficiency"] = context.InteractionProficiency ?? string.Empty
        };

        return Task.FromResult(new GlyphNodeResult
        {
            NextExecPinId = "exec_out",
            OutputValues = outputs
        });
    }

    public static GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "On Interaction Tick",
        Category = "Events",
        Description = "Entry point for scripts that run each round of an active interaction. " +
                      "Provides progress info and allows conditional cancellation via Cancel Interaction node.",
        ColorClass = "node-event",
        Archetype = GlyphNodeArchetype.EventEntry,
        IsSingleton = true,
        RestrictToEventType = GlyphEventType.OnInteractionTick,
        ScriptCategory = GlyphScriptCategory.Interaction,
        InputPins = [],
        OutputPins =
        [
            new GlyphPin { Id = "exec_out", Name = "Execute", DataType = GlyphDataType.Exec, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "character_id", Name = "Character ID", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "interaction_tag", Name = "Interaction Tag", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "target_id", Name = "Target ID", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "area_resref", Name = "Area ResRef", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "session_id", Name = "Session ID", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "progress", Name = "Progress", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "required_rounds", Name = "Required Rounds", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "proficiency", Name = "Proficiency", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Output }
        ]
    };
}
