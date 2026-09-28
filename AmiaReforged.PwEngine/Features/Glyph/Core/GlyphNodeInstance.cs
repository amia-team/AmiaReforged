namespace AmiaReforged.PwEngine.Features.Glyph.Core;

/// <summary>
/// An operation in executable IR, with static property overrides.
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
    /// Instance-specific property overrides, keyed by property name.
    /// Values are JSON-serialized. Used for inline-editable values like
    /// comparison operators, literal numbers, or enum selections.
    /// </summary>
    public Dictionary<string, string> PropertyOverrides { get; init; } = new();

}
