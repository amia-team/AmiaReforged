using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Platform;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Interactions;

/// <summary>
/// Action node that modifies the total required rounds for the current interaction session.
/// Can be used to extend or shorten an interaction mid-flight.
/// Operates on the live <see cref="GlyphExecutionContext.Session"/>.
/// </summary>
[GlyphNode]
public partial class SetRequiredRoundsExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "interaction.set_required_rounds";

    public string TypeId => NodeTypeId;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        object? newRoundsValue = await resolveInput(Inputs.NewRounds);
        int newRounds = Convert.ToInt32(newRoundsValue);

        if (context.Session != null)
        {
            context.Session.RequiredRounds = System.Math.Max(1, newRounds);
            context.InteractionRequiredRounds = context.Session.RequiredRounds;
        }

        return GlyphNodeResult.Continue("exec_out");
    }

    public static GlyphIntrinsicDescriptor Descriptor { get; } = new()
    {
        TypeId = NodeTypeId,
        Exports = [
            new("set_required_rounds", AllowedStages: ["started", "tick"], WritableAs: "required_rounds")
        ],
        DisplayName = "Set Required Rounds",
        Category = "Interactions",
        Description = "Changes the total number of rounds needed for the interaction to complete. " +
                      "Minimum value is 1. Can extend or shorten an interaction mid-flight.",
        ColorClass = "node-action",
        Archetype = GlyphNodeArchetype.Action,
        ScriptCategory = GlyphScriptCategory.Interaction,
        Parameters =
        [
            Pins.In("new_rounds", "New Rounds", GlyphDataType.Int, "3")
        ]
    };
}
