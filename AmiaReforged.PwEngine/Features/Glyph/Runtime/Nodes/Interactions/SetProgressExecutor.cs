using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Platform;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Interactions;

/// <summary>
/// Action node that modifies the current interaction session's progress (tick count).
/// Operates on the live <see cref="GlyphExecutionContext.Session"/>.
/// </summary>
[GlyphNode]
public partial class SetProgressExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "interaction.set_progress";

    public string TypeId => NodeTypeId;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        object? newProgressValue = await resolveInput(Inputs.NewProgress);
        int newProgress = Convert.ToInt32(newProgressValue);

        if (context.Session != null)
        {
            context.Session.Progress = newProgress;
            // Also update the context so downstream nodes see the new value
            context.InteractionProgress = newProgress;
        }

        return GlyphNodeResult.Continue("exec_out");
    }

    public static GlyphIntrinsicDescriptor Descriptor { get; } = new()
    {
        TypeId = NodeTypeId,
        Exports = [
            new("set_progress", AllowedStages: ["started", "tick"], WritableAs: "progress")
        ],
        DisplayName = "Set Progress",
        Category = "Interactions",
        Description = "Sets the interaction session's progress (tick count) to a new value. " +
                      "Can be used to skip ahead or reset progress.",
        ColorClass = "node-action",
        Archetype = GlyphNodeArchetype.Action,
        ScriptCategory = GlyphScriptCategory.Interaction,
        Parameters =
        [
            Pins.In("new_progress", "New Progress", GlyphDataType.Int, "0")
        ]
    };
}
