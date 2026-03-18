using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Flow;

/// <summary>
/// Branch node — the Glyph equivalent of an if/else statement.
/// Evaluates a boolean condition input and follows either the True or False Exec output.
/// </summary>
public class BranchExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "flow.branch";

    public string TypeId => NodeTypeId;

    public async Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        object? conditionValue = await resolveInput("condition");
        bool condition = Convert.ToBoolean(conditionValue);

        return GlyphNodeResult.Continue(condition ? "true" : "false");
    }

    /// <summary>
    /// Creates the node definition for registration in the registry.
    /// </summary>
    public GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Branch",
        Category = "Flow Control",
        Description = "If/else branch. Evaluates the condition and follows the True or False path.",
        ColorClass = "node-flow",
        Archetype = GlyphNodeArchetype.FlowControl,
        InputPins =
        [
            new GlyphPin { Id = "exec_in", Name = "Execute", DataType = GlyphDataType.Exec, Direction = GlyphPinDirection.Input },
            new GlyphPin { Id = "condition", Name = "Condition", DataType = GlyphDataType.Bool, Direction = GlyphPinDirection.Input, DefaultValue = "false" }
        ],
        OutputPins =
        [
            new GlyphPin { Id = "true", Name = "True", DataType = GlyphDataType.Exec, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "false", Name = "False", DataType = GlyphDataType.Exec, Direction = GlyphPinDirection.Output }
        ]
    };
}
