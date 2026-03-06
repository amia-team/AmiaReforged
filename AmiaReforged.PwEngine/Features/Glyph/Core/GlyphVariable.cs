namespace AmiaReforged.PwEngine.Features.Glyph.Core;

/// <summary>
/// A user-defined variable within a <see cref="GlyphGraph"/>. Variables persist
/// across the execution of a single graph run and can be read/written by nodes.
/// </summary>
public record GlyphVariable
{
    /// <summary>
    /// Variable name. Must be unique within the graph.
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// The data type of this variable.
    /// </summary>
    public required GlyphDataType DataType { get; init; }

    /// <summary>
    /// JSON-serialized default value. Applied at the start of each graph execution.
    /// </summary>
    public string? DefaultValue { get; init; }
}
