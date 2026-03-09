using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Interactions;

/// <summary>
/// Action node that cancels an in-progress interaction mid-tick.
/// Only valid during <see cref="GlyphEventType.OnInteractionTick"/> execution.
/// Sets <see cref="GlyphExecutionContext.ShouldCancelInteraction"/> to true.
/// </summary>
public class CancelInteractionExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "interaction.cancel";

    public string TypeId => NodeTypeId;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        object? messageValue = await resolveInput("message");
        string message = messageValue?.ToString() ?? "Interaction cancelled by script";

        context.ShouldCancelInteraction = true;
        context.CancelInteractionMessage = message;

        return GlyphNodeResult.Continue("exec_out");
    }

    public static GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Cancel Interaction",
        Category = "Interactions",
        Description = "Cancels the current interaction mid-progress. Only usable in OnInteractionTick scripts. " +
                      "Provide a message explaining why the interaction was cancelled.",
        ColorClass = "node-action",
        Archetype = GlyphNodeArchetype.Action,
        RestrictToEventType = GlyphEventType.OnInteractionTick,
        ScriptCategory = GlyphScriptCategory.Interaction,
        InputPins =
        [
            new GlyphPin { Id = "exec_in", Name = "Execute", DataType = GlyphDataType.Exec, Direction = GlyphPinDirection.Input },
            new GlyphPin { Id = "message", Name = "Message", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Input, DefaultValue = "Interaction cancelled by script" }
        ],
        OutputPins =
        [
            new GlyphPin { Id = "exec_out", Name = "Then", DataType = GlyphDataType.Exec, Direction = GlyphPinDirection.Output }
        ]
    };
}
