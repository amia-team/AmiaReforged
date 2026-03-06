using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Flow;

/// <summary>
/// Sequence node — executes multiple Exec outputs in order. Each output branch
/// runs to completion before the next one starts. Useful for performing
/// multiple independent actions from a single execution point.
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
        // The interpreter handles Sequence specially — it iterates through outputs 0..N.
        // We just signal the first output to start.
        return Task.FromResult(GlyphNodeResult.Continue("then_0"));
    }

    /// <summary>
    /// Returns the next Exec pin ID in the sequence, or null if all executed.
    /// </summary>
    public static string? GetNextOutputPin(int completedIndex)
    {
        int next = completedIndex + 1;
        return next < MaxOutputs ? $"then_{next}" : null;
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
