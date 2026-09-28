namespace AmiaReforged.PwEngine.Features.Glyph.Core;

/// <summary>
/// Mutable executable IR builder used by lowering and runtime tests. Production execution
/// obtains an isolated copy from a validated runtime program; this is not an authoring format.
/// </summary>
public class GlyphGraph
{
    [System.Text.Json.Serialization.JsonIgnore]
    public Guid DefinitionId { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public Guid VersionId { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public IReadOnlyDictionary<Guid, Language.Diagnostics.SourceSpan> SourceMap { get; set; } =
        new Dictionary<Guid, Language.Diagnostics.SourceSpan>();

    /// <summary>
    /// Unique identifier for this graph.
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Human-readable name for this graph (e.g., "Double Spawns at Night").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description of what this graph does.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// The encounter event type this graph triggers on.
    /// Determines which entry-point node is valid and what context data is available.
    /// </summary>
    public GlyphEventType EventType { get; set; }

    /// <summary>
    /// All node instances placed in this graph.
    /// </summary>
    public List<GlyphNodeInstance> Nodes { get; set; } = [];

    /// <summary>
    /// All edges (wires) connecting pins between nodes.
    /// </summary>
    public List<GlyphEdge> Edges { get; set; } = [];

    /// <summary>
    /// User-defined variables available during graph execution.
    /// </summary>
    public List<GlyphVariable> Variables { get; set; } = [];

    /// <summary>
    /// Finds the event entry-point node for this graph. Returns null if none exists.
    /// </summary>
    public GlyphNodeInstance? FindEntryNode()
    {
        string expectedPrefix = EventType switch
        {
            GlyphEventType.BeforeGroupSpawn => "event.before_group_spawn",
            GlyphEventType.AfterGroupSpawn => "event.after_group_spawn",
            GlyphEventType.OnCreatureDeath => "event.on_creature_death",
            GlyphEventType.OnCreatureSpawn => "event.on_creature_spawn",
            GlyphEventType.OnBossSpawn => "event.on_boss_spawn",
            GlyphEventType.OnTraitGranted => "event.on_trait_granted",
            GlyphEventType.OnTraitRemoved => "event.on_trait_removed",
            GlyphEventType.InteractionPipeline => "stage.interaction_attempted",
            _ => string.Empty
        };

        return Nodes.FirstOrDefault(n => n.TypeId == expectedPrefix);
    }

    /// <summary>
    /// Finds a pipeline stage node by its TypeId. Used by the interpreter to locate
    /// the entry point for a specific stage within a pipeline graph.
    /// </summary>
    public GlyphNodeInstance? FindStageNode(string stageTypeId)
    {
        return Nodes.FirstOrDefault(n => n.TypeId == stageTypeId);
    }

    /// <summary>
    /// Gets all edges originating from a specific node and pin.
    /// </summary>
    public IEnumerable<GlyphEdge> GetEdgesFrom(Guid nodeId, string pinId)
    {
        return Edges.Where(e => e.SourceNodeId == nodeId && e.SourcePinId == pinId);
    }

    /// <summary>
    /// Gets the single edge targeting a specific node and pin (data inputs typically have one source).
    /// Returns null if the pin has no incoming connection.
    /// </summary>
    public GlyphEdge? GetEdgeTo(Guid nodeId, string pinId)
    {
        return Edges.FirstOrDefault(e => e.TargetNodeId == nodeId && e.TargetPinId == pinId);
    }

    /// <summary>
    /// Gets a node instance by its ID. Returns null if not found.
    /// </summary>
    public GlyphNodeInstance? GetNode(Guid instanceId)
    {
        return Nodes.FirstOrDefault(n => n.InstanceId == instanceId);
    }
}
