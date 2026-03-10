using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Interactions;

/// <summary>
/// Action node that sets the interaction session's lifecycle status.
/// Can forcibly complete, cancel, or fail an interaction.
/// Operates on the live <see cref="GlyphExecutionContext.Session"/>.
/// </summary>
public class SetStatusExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "interaction.set_status";

    public string TypeId => NodeTypeId;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        object? statusValue = await resolveInput("status");
        string statusStr = statusValue?.ToString() ?? "Completed";

        if (context.Session != null && Enum.TryParse<InteractionStatus>(statusStr, ignoreCase: true, out InteractionStatus status))
        {
            context.Session.Status = status;
        }

        return GlyphNodeResult.Continue("exec_out");
    }

    public static GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Set Status",
        Category = "Interactions",
        Description = "Sets the interaction session's lifecycle status. " +
                      "Values: Active, Completed, Cancelled, Failed. " +
                      "Use to forcibly end or fail an interaction from a script.",
        ColorClass = "node-action",
        Archetype = GlyphNodeArchetype.Action,
        ScriptCategory = GlyphScriptCategory.Interaction,
        InputPins =
        [
            new GlyphPin { Id = "exec_in", Name = "Execute", DataType = GlyphDataType.Exec, Direction = GlyphPinDirection.Input },
            new GlyphPin { Id = "status", Name = "Status", DataType = GlyphDataType.String, Direction = GlyphPinDirection.Input, DefaultValue = "Completed" }
        ],
        OutputPins =
        [
            new GlyphPin { Id = "exec_out", Name = "Then", DataType = GlyphDataType.Exec, Direction = GlyphPinDirection.Output }
        ]
    };
}
