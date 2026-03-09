using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Interactions;

/// <summary>
/// Action node that blocks an interaction from starting.
/// Only valid during <see cref="GlyphEventType.OnInteractionAttempted"/> execution.
/// Sets <see cref="GlyphExecutionContext.ShouldBlockInteraction"/> to true.
/// </summary>
public class BlockInteractionExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "interaction.block";

    public string TypeId => NodeTypeId;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        object? messageValue = await resolveInput("message");
        string message = messageValue?.ToString() ?? "Interaction blocked by script";

        context.ShouldBlockInteraction = true;
        context.BlockInteractionMessage = message;

        return GlyphNodeResult.Continue("exec_out");
    }

    public static GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Block Interaction",
        Category = "Interactions",
        Description = "Prevents the interaction from starting. Only usable in OnInteractionAttempted scripts. " +
                      "Provide a message explaining why the interaction is blocked.",
        ColorClass = "node-action",
        Archetype = GlyphNodeArchetype.Action,
        RestrictToEventType = GlyphEventType.OnInteractionAttempted,
        ScriptCategory = GlyphScriptCategory.Interaction,
        InputPins =
        [
            new GlyphPin { Id = "exec_in", Name = "Execute", DataType = GlyphDataType.Exec, Direction = GlyphPinDirection.Input },
            new GlyphPin { Id = "message", Name = "Message", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Input, DefaultValue = "Interaction blocked by script" }
        ],
        OutputPins =
        [
            new GlyphPin { Id = "exec_out", Name = "Then", DataType = GlyphDataType.Exec, Direction = GlyphPinDirection.Output }
        ]
    };
}
