namespace AmiaReforged.AdminPanel.Models;

/// <summary>
/// Pre-computed position for a graph node, calculated server-side.
/// </summary>
public class GraphLayoutPosition
{
    public double X { get; set; }
    public double Y { get; set; }
}

/// <summary>
/// Result of a server-side layout computation.
/// Maps node IDs (resRef or region_tag) to their computed positions.
/// </summary>
public class GraphLayoutResult
{
    /// <summary>
    /// Node ID → position mapping. Includes both area nodes (resRef) and region parent nodes (region_tag).
    /// </summary>
    public Dictionary<string, GraphLayoutPosition> Positions { get; set; } = new();

    /// <summary>
    /// The layout algorithm that was used.
    /// </summary>
    public string Algorithm { get; set; } = string.Empty;

    /// <summary>
    /// When this layout was computed.
    /// </summary>
    public DateTime ComputedAtUtc { get; set; } = DateTime.UtcNow;
}
