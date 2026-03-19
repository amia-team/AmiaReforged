using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime;

/// <summary>
/// Implemented by entry-point or pipeline stage executors that wish to expose their
/// output pin values as "wireless" context getter nodes. At bootstrap, the system
/// iterates providers, creates a <see cref="Nodes.Context.ContextGetterExecutor"/> per
/// pin, and registers them in the node catalog.
/// <para>
/// Context getters read directly from <see cref="GlyphExecutionContext"/> at runtime,
/// eliminating the need for long wires back to the source node.
/// </para>
/// </summary>
public interface IContextNodeProvider
{
    /// <summary>
    /// The <see cref="IGlyphNodeExecutor.TypeId"/> of the source node
    /// (e.g. <c>"stage.interaction_attempted"</c>, <c>"event.before_group_spawn"</c>).
    /// </summary>
    string SourceTypeId { get; }

    /// <summary>
    /// Human-readable label used as a prefix in context getter display names
    /// (e.g. "Attempted", "Before Group Spawn").
    /// </summary>
    string SourceDisplayName { get; }

    /// <summary>
    /// The event type this source node belongs to. Context getters inherit this restriction
    /// so they only appear in graphs of the matching type.
    /// </summary>
    GlyphEventType? SourceEventType { get; }

    /// <summary>
    /// The script category this source node belongs to. Context getters inherit this restriction.
    /// </summary>
    GlyphScriptCategory? SourceScriptCategory { get; }

    /// <summary>
    /// Returns descriptors for every context pin this source exposes.
    /// Each descriptor produces one context getter node.
    /// </summary>
    List<ContextPinDescriptor> GetContextPins();
}

/// <summary>
/// Describes a single context value that a source node exposes as a wireless getter.
/// The <see cref="Accessor"/> delegate extracts the value from the execution context at runtime.
/// </summary>
/// <param name="PinId">The identifier matching the source node's output pin (e.g. "character_id").</param>
/// <param name="DisplayName">Human-readable pin name (e.g. "Character ID").</param>
/// <param name="DataType">The Glyph data type of the value.</param>
/// <param name="Accessor">Delegate that reads the value from the execution context.</param>
public record ContextPinDescriptor(
    string PinId,
    string DisplayName,
    GlyphDataType DataType,
    Func<GlyphExecutionContext, object?> Accessor);
