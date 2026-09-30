using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions;
using AmiaReforged.PwEngine.Features.Glyph.Platform;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Interactions;

/// <summary>
/// Action node that sets the interaction session's lifecycle status.
/// Can forcibly complete, cancel, or fail an interaction.
/// Operates on the live <see cref="GlyphExecutionContext.Session"/>.
/// </summary>
[GlyphNode]
public partial class SetStatusExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "interaction.set_status";

    public string TypeId => NodeTypeId;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        object? statusValue = await resolveInput(Inputs.Status);
        string statusStr = statusValue?.ToString() ?? "Completed";

        if (context.Session != null && Enum.TryParse<InteractionStatus>(statusStr, ignoreCase: true, out InteractionStatus status))
        {
            context.Session.Status = status;
        }

        return GlyphNodeResult.Continue("exec_out");
    }

    public static GlyphIntrinsicDescriptor Descriptor { get; } = new()
    {
        TypeId = NodeTypeId,
        Exports = [
            new("set_status", AllowedStages: ["started", "tick", "completed"], WritableAs: "status")
        ],
        DisplayName = "Set Status",
        Category = "Interactions",
        Description = "Sets the interaction session's lifecycle status. " +
                      "Values: Active, Completed, Cancelled, Failed. " +
                      "Use to forcibly end or fail an interaction from a script.",
        ColorClass = "node-action",
        Archetype = GlyphNodeArchetype.Action,
        ScriptCategory = GlyphScriptCategory.Interaction,
        Parameters =
        [
            Pins.In("status", "Status", GlyphDataType.String, "Completed")
        ]
    };
}
