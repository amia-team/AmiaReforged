using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Interactions;

/// <summary>
/// Action node that suppresses a later stage in the interaction pipeline.
/// For example, a Started stage script can suppress the Tick stage
/// to prevent tick scripts from running for the remainder of this session.
/// The suppression key is the stage TypeId (e.g., "stage.interaction_tick").
/// Operates on the live <see cref="GlyphExecutionContext.Session"/>.
/// </summary>
public class SuppressEventExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "interaction.suppress_event";

    public string TypeId => NodeTypeId;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        object? eventTypeValue = await resolveInput("event_type");
        string eventType = eventTypeValue?.ToString() ?? InteractionTickStageExecutor.NodeTypeId;

        context.Session?.SuppressEvent(eventType);

        return GlyphNodeResult.Continue("exec_out");
    }

    public static GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Suppress Stage",
        Category = "Interactions",
        Description = "Prevents a later pipeline stage from firing for this interaction session. " +
                      "For example, suppressing the Tick stage from the Started stage " +
                      "will prevent all tick scripts from running.",
        ColorClass = "node-action",
        Archetype = GlyphNodeArchetype.Action,
        ScriptCategory = GlyphScriptCategory.Interaction,
        InputPins =
        [
            new GlyphPin { Id = "exec_in", Name = "Execute", DataType = GlyphDataType.Exec, Direction = GlyphPinDirection.Input },
            new GlyphPin
            {
                Id = "event_type", Name = "Stage TypeId", DataType = GlyphDataType.String,
                Direction = GlyphPinDirection.Input,
                DefaultValue = "stage.interaction_tick"
            }
        ],
        OutputPins =
        [
            new GlyphPin { Id = "exec_out", Name = "Then", DataType = GlyphDataType.Exec, Direction = GlyphPinDirection.Output }
        ]
    };
}
