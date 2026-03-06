using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime;

/// <summary>
/// Result returned by a node executor after processing a node.
/// Contains the output pin ID to follow for execution flow and any
/// computed data pin values.
/// </summary>
public class GlyphNodeResult
{
    /// <summary>
    /// The output Exec pin ID to follow next. Null means this execution branch terminates.
    /// For Branch nodes, this would be "true" or "false".
    /// For Sequence nodes, the interpreter handles multiple outputs internally.
    /// </summary>
    public string? NextExecPinId { get; init; }

    /// <summary>
    /// Computed output data pin values from this node execution.
    /// Keyed by output pin ID, values are boxed .NET types.
    /// </summary>
    public Dictionary<string, object?> OutputValues { get; init; } = new();

    /// <summary>
    /// Creates a result that continues execution along the specified output Exec pin.
    /// </summary>
    public static GlyphNodeResult Continue(string execPinId) => new() { NextExecPinId = execPinId };

    /// <summary>
    /// Creates a result that terminates this execution branch.
    /// </summary>
    public static GlyphNodeResult Done() => new() { NextExecPinId = null };

    /// <summary>
    /// Creates a result with output data values but no execution flow (pure data node).
    /// </summary>
    public static GlyphNodeResult Data(Dictionary<string, object?> outputs) =>
        new() { OutputValues = outputs };
}
