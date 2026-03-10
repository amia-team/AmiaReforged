using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Interactions;

/// <summary>
/// Action node that suppresses a later event type in the interaction pipeline.
/// For example, an OnInteractionStarted script can suppress OnInteractionTick
/// to prevent tick scripts from running for the remainder of this session.
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
        string eventType = eventTypeValue?.ToString() ?? "OnInteractionTick";

        context.Session?.SuppressEvent(eventType);

        return GlyphNodeResult.Continue("exec_out");
    }

    public static GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Suppress Event",
        Category = "Interactions",
        Description = "Prevents a later event type from firing for this interaction session. " +
                      "For example, suppressing OnInteractionTick from an OnInteractionStarted script " +
                      "will prevent all tick scripts from running.",
        ColorClass = "node-action",
        Archetype = GlyphNodeArchetype.Action,
        ScriptCategory = GlyphScriptCategory.Interaction,
        InputPins =
        [
            new GlyphPin { Id = "exec_in", Name = "Execute", DataType = GlyphDataType.Exec, Direction = GlyphPinDirection.Input },
            new GlyphPin
            {
                Id = "event_type", Name = "Event Type", DataType = GlyphDataType.String,
                Direction = GlyphPinDirection.Input,
                DefaultValue = "OnInteractionTick"
            }
        ],
        OutputPins =
        [
            new GlyphPin { Id = "exec_out", Name = "Then", DataType = GlyphDataType.Exec, Direction = GlyphPinDirection.Output }
        ]
    };
}
