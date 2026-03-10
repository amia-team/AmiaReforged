using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Interactions;

/// <summary>
/// Action node that modifies the total required rounds for the current interaction session.
/// Can be used to extend or shorten an interaction mid-flight.
/// Operates on the live <see cref="GlyphExecutionContext.Session"/>.
/// </summary>
public class SetRequiredRoundsExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "interaction.set_required_rounds";

    public string TypeId => NodeTypeId;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        object? newRoundsValue = await resolveInput("new_rounds");
        int newRounds = Convert.ToInt32(newRoundsValue);

        if (context.Session != null)
        {
            context.Session.RequiredRounds = System.Math.Max(1, newRounds);
            context.InteractionRequiredRounds = context.Session.RequiredRounds;
        }

        return GlyphNodeResult.Continue("exec_out");
    }

    public static GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Set Required Rounds",
        Category = "Interactions",
        Description = "Changes the total number of rounds needed for the interaction to complete. " +
                      "Minimum value is 1. Can extend or shorten an interaction mid-flight.",
        ColorClass = "node-action",
        Archetype = GlyphNodeArchetype.Action,
        ScriptCategory = GlyphScriptCategory.Interaction,
        InputPins =
        [
            new GlyphPin { Id = "exec_in", Name = "Execute", DataType = GlyphDataType.Exec, Direction = GlyphPinDirection.Input },
            new GlyphPin { Id = "new_rounds", Name = "New Rounds", DataType = GlyphDataType.Int, Direction = GlyphPinDirection.Input, DefaultValue = "3" }
        ],
        OutputPins =
        [
            new GlyphPin { Id = "exec_out", Name = "Then", DataType = GlyphDataType.Exec, Direction = GlyphPinDirection.Output }
        ]
    };
}
