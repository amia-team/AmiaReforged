using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime;

/// <summary>
/// Interface for a node executor that performs the runtime logic for a specific
/// <see cref="GlyphNodeDefinition.TypeId"/>. Each node type registers one executor.
/// </summary>
public interface IGlyphNodeExecutor
{
    /// <summary>
    /// The node type ID this executor handles (e.g., "flow.branch", "action.apply_effect").
    /// Must match a registered <see cref="GlyphNodeDefinition.TypeId"/>.
    /// </summary>
    string TypeId { get; }

    /// <summary>
    /// Executes the node's logic. The interpreter calls this when the node is reached
    /// during graph traversal.
    /// </summary>
    /// <param name="node">The node instance being executed</param>
    /// <param name="context">The mutable execution context</param>
    /// <param name="resolveInput">
    /// Delegate to lazily resolve the value of an input data pin by tracing its edge
    /// back to the source node's output. Returns the default value if unconnected.
    /// </param>
    /// <returns>
    /// A result indicating which output Exec pin to follow and any computed data values.
    /// </returns>
    Task<GlyphNodeResult> ExecuteAsync(
        GlyphNodeInstance node,
        GlyphExecutionContext context,
        Func<string, Task<object?>> resolveInput);

    /// <summary>
    /// Creates the <see cref="GlyphNodeDefinition"/> that describes this executor's node type,
    /// including its pins, category, and display information. Used by <see cref="GlyphBootstrap"/>
    /// to register both the definition and executor from a single list.
    /// </summary>
    GlyphNodeDefinition CreateDefinition();
}
