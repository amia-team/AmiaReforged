using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Flow;

/// <summary>
/// DoNothing node — a no-op terminal that explicitly ends an execution branch.
/// Useful as a placeholder or to make graph intent clearer.
/// </summary>
public class DoNothingExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "flow.do_nothing";

    public string TypeId => NodeTypeId;

    public Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        return Task.FromResult(GlyphNodeResult.Done());
    }

    /// <summary>
    /// Creates the node definition for registration in the registry.
    /// </summary>
    public static GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Do Nothing",
        Category = "Flow Control",
        Description = "No-op terminal node. Explicitly ends an execution branch.",
        ColorClass = "node-flow",
        Archetype = GlyphNodeArchetype.FlowControl,
        InputPins =
        [
            new GlyphPin { Id = "exec_in", Name = "Execute", DataType = GlyphDataType.Exec, Direction = GlyphPinDirection.Input }
        ],
        OutputPins = []
    };
}
