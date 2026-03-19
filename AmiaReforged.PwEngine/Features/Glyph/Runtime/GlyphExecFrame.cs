using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime;

/// <summary>
/// A frame on the Glyph interpreter's execution stack. Represents a flow-control node
/// that has more work to do after its current sub-branch completes.
/// <para>
/// For <b>Sequence</b>: the frame holds the remaining exec output pins (then_1, then_2, ...)
/// that need to run after the current branch terminates.
/// </para>
/// <para>
/// For <b>ForEach</b>: the frame holds the loop node so the interpreter can re-execute it
/// after each iteration's body completes.
/// </para>
/// </summary>
public class GlyphExecFrame
{
    /// <summary>
    /// The flow-control node that created this frame.
    /// </summary>
    public required GlyphNodeInstance Node { get; init; }

    /// <summary>
    /// Remaining exec output pin IDs to run in order (for Sequence-style multi-branch).
    /// After the current branch terminates, the interpreter pops this frame and follows
    /// the next pin in this list. When empty, the frame is fully consumed.
    /// </summary>
    public Queue<string> RemainingBranches { get; init; } = new();

    /// <summary>
    /// When true, this frame represents a loop. After the current branch terminates,
    /// the interpreter re-executes the <see cref="Node"/> to check if another iteration
    /// is needed (the executor manages the iteration state).
    /// </summary>
    public bool IsLoop { get; init; }

    /// <summary>
    /// Tracks all node instance IDs that were executed (or lazily evaluated) inside the
    /// current loop iteration's body. On each iteration advance, the interpreter clears
    /// the <see cref="GlyphExecutionContext.PinValueCache"/> entries for these nodes so
    /// that pure-function nodes are re-evaluated with fresh upstream values.
    /// </summary>
    public HashSet<Guid> LoopBodyNodeIds { get; } = [];
}
