using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Flow;

/// <summary>
/// Sequence node — executes multiple Exec outputs in order. Each output branch
/// runs to completion before the next one starts. Useful for performing
/// multiple independent actions from a single execution point.
/// <para>
/// The executor inspects the graph to find which then_N pins actually have outgoing edges,
/// and returns a <see cref="GlyphNodeResult.MultiBranch"/> result so the interpreter
/// runs all connected branches in order.
/// </para>
/// </summary>
public class SequenceExecutor : IGlyphNodeExecutor
{
    public const string NodeTypeId = "flow.sequence";

    /// <summary>
    /// Maximum number of outputs a Sequence node supports.
    /// </summary>
    public const int MaxOutputs = 6;

    public string TypeId => NodeTypeId;

    public Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput)
    {
        // Collect all output exec pins that have outgoing edges
        List<string> connectedPins = [];

        for (int i = 0; i < MaxOutputs; i++)
        {
            string pinId = $"then_{i}";
            if (context.Graph.GetEdgesFrom(node.InstanceId, pinId).Any())
            {
                connectedPins.Add(pinId);
            }
        }

        if (connectedPins.Count == 0)
        {
            return Task.FromResult(GlyphNodeResult.Done());
        }

        return Task.FromResult(GlyphNodeResult.MultiBranch(connectedPins.ToArray()));
    }

    /// <summary>
    /// Creates the node definition for registration in the registry.
    /// </summary>
    public static GlyphNodeDefinition CreateDefinition() => new()
    {
        TypeId = NodeTypeId,
        DisplayName = "Sequence",
        Category = "Flow Control",
        Description = "Executes multiple output branches in order, one after another.",
        ColorClass = "node-flow",
        Archetype = GlyphNodeArchetype.FlowControl,
        InputPins =
        [
            new GlyphPin { Id = "exec_in", Name = "Execute", DataType = GlyphDataType.Exec, Direction = GlyphPinDirection.Input }
        ],
        OutputPins =
        [
            new GlyphPin { Id = "then_0", Name = "Then 0", DataType = GlyphDataType.Exec, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "then_1", Name = "Then 1", DataType = GlyphDataType.Exec, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "then_2", Name = "Then 2", DataType = GlyphDataType.Exec, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "then_3", Name = "Then 3", DataType = GlyphDataType.Exec, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "then_4", Name = "Then 4", DataType = GlyphDataType.Exec, Direction = GlyphPinDirection.Output },
            new GlyphPin { Id = "then_5", Name = "Then 5", DataType = GlyphDataType.Exec, Direction = GlyphPinDirection.Output }
        ]
    };
}
