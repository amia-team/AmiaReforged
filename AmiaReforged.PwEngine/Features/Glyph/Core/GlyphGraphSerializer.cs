using System.Text.Json;
using AmiaReforged.PwEngine.Features.Glyph.Persistence;
using NLog;

namespace AmiaReforged.PwEngine.Features.Glyph.Core;

/// <summary>
/// Shared utility for serializing and deserializing <see cref="GlyphGraph"/> instances.
/// Replaces the identical <c>DeserializeGraph</c> methods previously duplicated across
/// the three hook services. Also performs stale-edge cleanup on load to handle graphs
/// persisted before pipeline stage nodes lost their <c>exec_in</c> pins.
/// </summary>
public static class GlyphGraphSerializer
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Deserializes a <see cref="GlyphGraph"/> from a <see cref="GlyphDefinition"/>,
    /// populating the graph's Name, Description, and EventType from the definition.
    /// Strips stale exec edges targeting pipeline stage nodes (which no longer have exec_in).
    /// Returns null if deserialization fails.
    /// </summary>
    public static GlyphGraph? Deserialize(GlyphDefinition definition)
    {
        try
        {
            GlyphGraph? graph = JsonSerializer.Deserialize<GlyphGraph>(
                definition.GraphJson, GlyphJsonDefaults.Options);

            if (graph != null)
            {
                graph.Name = definition.Name;
                graph.Description = definition.Description ?? string.Empty;

                if (Enum.TryParse<GlyphEventType>(definition.EventType, out GlyphEventType et))
                    graph.EventType = et;

                // Strip stale inter-stage exec edges from graphs saved before the exec_in removal.
                // Stage nodes are now pure entry points with no exec_in pin.
                StripStaleStageEdges(graph);
            }

            return graph;
        }
        catch (JsonException ex)
        {
            Log.Error(ex, "Failed to deserialize Glyph graph JSON for definition '{Name}' ({Id}).",
                definition.Name, definition.Id);
            return null;
        }
    }

    /// <summary>
    /// Serializes a <see cref="GlyphGraph"/> to JSON using the standard Glyph options.
    /// </summary>
    public static string Serialize(GlyphGraph graph)
    {
        return JsonSerializer.Serialize(graph, GlyphJsonDefaults.Options);
    }

    /// <summary>
    /// Removes any exec edges whose target is a pipeline stage node. These edges are
    /// remnants from the old auto-generated inter-stage wiring and serve no runtime purpose.
    /// </summary>
    private static void StripStaleStageEdges(GlyphGraph graph)
    {
        // Build a set of node instance IDs that are pipeline stage nodes
        HashSet<Guid> stageNodeIds = new();
        foreach (GlyphNodeInstance node in graph.Nodes)
        {
            if (node.TypeId.StartsWith("stage.", StringComparison.Ordinal))
                stageNodeIds.Add(node.InstanceId);
        }

        if (stageNodeIds.Count == 0) return;

        int removed = graph.Edges.RemoveAll(e =>
            stageNodeIds.Contains(e.TargetNodeId) && e.TargetPinId == "exec_in");

        if (removed > 0)
        {
            Log.Info("Stripped {Count} stale inter-stage exec edge(s) from graph '{Name}'.",
                removed, graph.Name);
        }
    }
}
