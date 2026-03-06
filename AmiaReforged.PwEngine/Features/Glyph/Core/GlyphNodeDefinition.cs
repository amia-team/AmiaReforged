namespace AmiaReforged.PwEngine.Features.Glyph.Core;

/// <summary>
/// A node definition is the template/blueprint for a type of node in the Glyph system.
/// It describes the node's pins, category, and display information. Node definitions
/// are registered at startup and shared across all graphs — they are not persisted per-graph.
/// </summary>
public class GlyphNodeDefinition
{
    /// <summary>
    /// Unique type identifier for this node kind. Dot-separated by category.
    /// Convention: "category.name" (e.g., "flow.branch", "action.apply_effect", "event.before_group_spawn").
    /// </summary>
    public required string TypeId { get; init; }

    /// <summary>
    /// Human-readable name displayed in the editor palette and node header.
    /// </summary>
    public required string DisplayName { get; init; }

    /// <summary>
    /// Category for grouping in the editor palette (e.g., "Flow Control", "Actions", "Events").
    /// </summary>
    public required string Category { get; init; }

    /// <summary>
    /// Description shown as a tooltip in the editor.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Header color CSS class hint for the editor (e.g., "node-event", "node-flow", "node-action").
    /// </summary>
    public string ColorClass { get; init; } = "node-default";

    /// <summary>
    /// Input pins on this node (left side in the editor).
    /// </summary>
    public List<GlyphPin> InputPins { get; init; } = [];

    /// <summary>
    /// Output pins on this node (right side in the editor).
    /// </summary>
    public List<GlyphPin> OutputPins { get; init; } = [];

    /// <summary>
    /// When true, only one instance of this node can exist per graph (e.g., event entry points).
    /// </summary>
    public bool IsSingleton { get; init; }

    /// <summary>
    /// When set, this node definition is only valid in graphs with the specified event type.
    /// Null means the node is available in all graph types.
    /// </summary>
    public GlyphEventType? RestrictToEventType { get; init; }

    /// <summary>
    /// When set, this node is only shown in the palette for scripts of the specified category.
    /// Null means the node is available across all script categories (universal).
    /// </summary>
    public GlyphScriptCategory? ScriptCategory { get; init; }
}
