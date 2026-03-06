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
    /// For Sequence/ForEach, use <see cref="BranchPinIds"/> instead for multi-branch flow.
    /// </summary>
    public string? NextExecPinId { get; init; }

    /// <summary>
    /// Computed output data pin values from this node execution.
    /// Keyed by output pin ID, values are boxed .NET types.
    /// </summary>
    public Dictionary<string, object?> OutputValues { get; init; } = new();

    /// <summary>
    /// When set, the interpreter should run each of these exec branches in order.
    /// After the first branch terminates, the interpreter proceeds to the next, and so on.
    /// Used by Sequence nodes to run then_0, then_1, ..., then_N in order.
    /// </summary>
    public string[]? BranchPinIds { get; init; }

    /// <summary>
    /// When true, indicates this is a loop node. After the <see cref="NextExecPinId"/> branch
    /// terminates, the interpreter should re-execute this node to check if another iteration
    /// is needed (the executor manages its own iteration state via context variables).
    /// Used by ForEach to loop the body N times, then follow CompletedPinId.
    /// </summary>
    public bool IsLoopNode { get; init; }

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

    /// <summary>
    /// Creates a result that runs multiple exec output branches in sequence.
    /// Each branch runs to completion before the next starts.
    /// </summary>
    public static GlyphNodeResult MultiBranch(params string[] execPinIds)
    {
        if (execPinIds.Length == 0) return Done();
        if (execPinIds.Length == 1) return Continue(execPinIds[0]);

        return new GlyphNodeResult
        {
            NextExecPinId = execPinIds[0],
            BranchPinIds = execPinIds[1..],
        };
    }

    /// <summary>
    /// Creates a result for a loop iteration. The interpreter follows <paramref name="loopBodyPinId"/>,
    /// then re-executes this node. When the executor decides the loop is done, it should return
    /// <see cref="Continue"/> with the completed pin instead.
    /// </summary>
    public static GlyphNodeResult LoopBody(string loopBodyPinId, Dictionary<string, object?> outputs) =>
        new()
        {
            NextExecPinId = loopBodyPinId,
            OutputValues = outputs,
            IsLoopNode = true,
        };
}
