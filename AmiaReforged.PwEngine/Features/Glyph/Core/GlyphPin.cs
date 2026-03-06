namespace AmiaReforged.PwEngine.Features.Glyph.Core;

/// <summary>
/// A pin (connection point) on a Glyph node. Pins carry either execution flow
/// or data values and define what connections are valid between nodes.
/// </summary>
public record GlyphPin
{
    /// <summary>
    /// Unique identifier for this pin within its parent node definition.
    /// Convention: lowercase with underscores (e.g., "exec_out", "party_size").
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Human-readable display name shown in the editor.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The data type carried by this pin. Determines valid connections.
    /// </summary>
    public required GlyphDataType DataType { get; init; }

    /// <summary>
    /// Whether this pin receives or sends data/flow.
    /// </summary>
    public required GlyphPinDirection Direction { get; init; }

    /// <summary>
    /// JSON-serialized default value for input data pins. Null for Exec pins
    /// and output pins. Used when no edge is connected.
    /// </summary>
    public string? DefaultValue { get; init; }

    /// <summary>
    /// When true, the pin accepts multiple connections (fan-in for inputs, fan-out for outputs).
    /// Exec output pins typically allow only one connection; data output pins may fan out.
    /// </summary>
    public bool AllowMultipleConnections { get; init; }
}
