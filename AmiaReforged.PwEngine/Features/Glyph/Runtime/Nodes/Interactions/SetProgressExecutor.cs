using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Interactions;

/// <summary>
/// Action node that modifies the current interaction session's progress (tick count).
/// Operates on the live <see cref="GlyphExecutionContext.Session"/>.
/// </summary>
public class SetProgressExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "interaction.set_progress";

    public string TypeId => NodeTypeId;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        object? newProgressValue = await resolveInput("new_progress");
        int newProgress = Convert.ToInt32(newProgressValue);

        if (context.Session != null)
        {
            context.Session.Progress = newProgress;
            // Also update the context so downstream nodes see the new value
            context.InteractionProgress = newProgress;
        }

        return GlyphNodeResult.Continue("exec_out");
    }

    public GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Set Progress",
        Category = "Interactions",
        Description = "Sets the interaction session's progress (tick count) to a new value. " +
                      "Can be used to skip ahead or reset progress.",
        ColorClass = "node-action",
        Archetype = GlyphNodeArchetype.Action,
        ScriptCategory = GlyphScriptCategory.Interaction,
        InputPins =
        [
            new GlyphPin { Id = "exec_in", Name = "Execute", DataType = GlyphDataType.Exec, Direction = GlyphPinDirection.Input },
            new GlyphPin { Id = "new_progress", Name = "New Progress", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Input, DefaultValue = "0" }
        ],
        OutputPins =
        [
            new GlyphPin { Id = "exec_out", Name = "Then", DataType = GlyphDataType.Exec, Direction = GlyphPinDirection.Output }
        ]
    };
}
