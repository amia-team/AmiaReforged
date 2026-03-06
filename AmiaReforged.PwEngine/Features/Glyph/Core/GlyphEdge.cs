namespace AmiaReforged.PwEngine.Features.Glyph.Core;

/// <summary>
/// A directed edge (wire) connecting an output pin on one node to an input pin on another.
/// Edges carry either execution flow (Exec type) or data values.
/// </summary>
public record GlyphEdge
{
    /// <summary>
    /// Unique identifier for this edge within its graph.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// The <see cref="GlyphNodeInstance.InstanceId"/> of the source (output) node.
    /// </summary>
    public required Guid SourceNodeId { get; init; }

    /// <summary>
    /// The <see cref="GlyphPin.Id"/> of the output pin on the source node.
    /// </summary>
    public required string SourcePinId { get; init; }

    /// <summary>
    /// The <see cref="GlyphNodeInstance.InstanceId"/> of the target (input) node.
    /// </summary>
    public required Guid TargetNodeId { get; init; }

    /// <summary>
    /// The <see cref="GlyphPin.Id"/> of the input pin on the target node.
    /// </summary>
    public required string TargetPinId { get; init; }
}
