namespace AmiaReforged.PwEngine.Features.Glyph.Core;

/// <summary>
/// Classifies a node definition into one of the fundamental node archetypes.
/// The interpreter uses this to determine how to handle a node's execution result —
/// particularly for flow control nodes that require special multi-branch or loop handling.
/// </summary>
public enum GlyphNodeArchetype
{
    /// <summary>
    /// Event entry point node. Singleton, no exec_in pin. Fires when the graph's event triggers.
    /// Provides context data as output pins and a single exec_out to start the imperative chain.
    /// </summary>
    EventEntry,

    /// <summary>
    /// Pure function node. No exec pins. Lazily evaluated via backward resolution when a
    /// downstream node requests its output. Output is cached per execution run.
    /// Examples: math operators, getters, constants, comparisons.
    /// </summary>
    PureFunction,

    /// <summary>
    /// Imperative action node. Has exec_in and exec_out pins. Runs in the exec chain,
    /// produces side effects (mutating game state, setting variables, etc.).
    /// May also produce data outputs consumed by downstream nodes.
    /// </summary>
    Action,

    /// <summary>
    /// Flow control node. Has exec_in and multiple exec_out pins. Requires special interpreter
    /// support — the interpreter pushes a frame onto the execution stack so it can return to this
    /// node after a sub-branch completes (e.g., Sequence runs then_0..then_N in order, ForEach
    /// loops the body N times, Branch picks one of two paths).
    /// </summary>
    FlowControl,

    /// <summary>
    /// Pipeline stage node. Similar to EventEntry (no exec_in, provides context data outputs
    /// and exec_out), but multiple can coexist in one graph. Each represents a discrete phase
    /// in a causal pipeline (e.g., Attempted → Started → Tick → Completed for interactions).
    /// The interpreter invokes the appropriate stage node based on the current lifecycle event.
    /// </summary>
    PipelineStage,
}
