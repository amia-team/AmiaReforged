using System.Text.Json.Serialization;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.AreaGraph;

/// <summary>
/// Represents a single area node in the area connectivity graph.
/// </summary>
public record AreaNode(
    string ResRef,
    string Name,
    string? Region = null);

/// <summary>
/// Represents a directed edge between two areas via a transition object.
/// </summary>
public record AreaEdge(
    string SourceResRef,
    string TargetResRef,
    TransitionType TransitionType,
    string TransitionTag);

/// <summary>
/// The type of game object providing the transition.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TransitionType
{
    Door,
    Trigger
}

/// <summary>
/// Complete area connectivity graph. Contains all connected and disconnected areas,
/// plus the edges representing transitions between them.
/// </summary>
public class AreaGraphData
{
    /// <summary>
    /// Areas that participate in at least one transition (connected component).
    /// </summary>
    public List<AreaNode> Nodes { get; init; } = [];

    /// <summary>
    /// Directed transition edges between connected areas.
    /// </summary>
    public List<AreaEdge> Edges { get; init; } = [];

    /// <summary>
    /// Areas with zero incoming or outgoing transitions.
    /// Shown separately from the main graph for clarity.
    /// </summary>
    public List<AreaNode> DisconnectedAreas { get; init; } = [];

    /// <summary>
    /// When this graph snapshot was generated.
    /// </summary>
    public DateTime GeneratedAtUtc { get; init; } = DateTime.UtcNow;
}
