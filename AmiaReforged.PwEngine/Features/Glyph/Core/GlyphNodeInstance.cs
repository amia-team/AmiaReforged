namespace AmiaReforged.PwEngine.Features.Glyph.Core;

/// <summary>
/// A placed node instance within a <see cref="GlyphGraph"/>. References a
/// <see cref="GlyphNodeDefinition"/> by <see cref="TypeId"/> and carries
/// instance-specific data (position, property overrides).
/// </summary>
public record GlyphNodeInstance
{
    /// <summary>
    /// Unique identifier for this node instance within its graph.
    /// </summary>
    public Guid InstanceId { get; init; } = Guid.NewGuid();

    /// <summary>
    /// References the <see cref="GlyphNodeDefinition.TypeId"/> that defines this node's
    /// pins, behavior, and display.
    /// </summary>
    public required string TypeId { get; init; }

    /// <summary>
    /// Horizontal position on the editor canvas.
    /// </summary>
    public float PositionX { get; init; }

    /// <summary>
    /// Vertical position on the editor canvas.
    /// </summary>
    public float PositionY { get; init; }

    /// <summary>
    /// Instance-specific property overrides, keyed by property name.
    /// Values are JSON-serialized. Used for inline-editable values like
    /// comparison operators, literal numbers, or enum selections.
    /// </summary>
    public Dictionary<string, string> PropertyOverrides { get; init; } = new();

    /// <summary>
    /// Optional user comment displayed on the node in the editor.
    /// </summary>
    public string? Comment { get; init; }
}
