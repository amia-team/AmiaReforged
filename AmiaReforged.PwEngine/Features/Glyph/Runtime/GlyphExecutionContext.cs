using AmiaReforged.PwEngine.Features.Encounters.Models;
using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime;

/// <summary>
/// Mutable runtime state bag passed through a Glyph graph during execution.
/// Contains encounter context, graph variables, spawned creature references,
/// and control flags. A new context is created for each graph execution.
/// </summary>
public class GlyphExecutionContext
{
    /// <summary>
    /// The graph being executed.
    /// </summary>
    public required GlyphGraph Graph { get; init; }

    /// <summary>
    /// The encounter context from the spawn system.
    /// </summary>
    public required EncounterContext EncounterContext { get; init; }

    /// <summary>
    /// The spawn profile that owns the encounter.
    /// </summary>
    public required SpawnProfile Profile { get; init; }

    /// <summary>
    /// The spawn group being processed (null for OnCreatureDeath if group is unknown).
    /// </summary>
    public SpawnGroup? Group { get; init; }

    /// <summary>
    /// The current spawn count, modifiable by BeforeGroupSpawn nodes.
    /// Initialized to the scaled count calculated by the spawner.
    /// </summary>
    public int SpawnCount { get; set; }

    /// <summary>
    /// When set to true by a node during BeforeGroupSpawn execution, the spawn
    /// for the current group is cancelled entirely.
    /// </summary>
    public bool ShouldCancelSpawn { get; set; }

    /// <summary>
    /// Object IDs of creatures spawned by the current group (populated for AfterGroupSpawn).
    /// </summary>
    public List<uint> SpawnedCreatures { get; set; } = [];

    /// <summary>
    /// Object ID of the creature that died (populated for OnCreatureDeath).
    /// </summary>
    public uint DeadCreature { get; set; }

    /// <summary>
    /// Object ID of the killer (populated for OnCreatureDeath). May be NWScript.OBJECT_INVALID.
    /// </summary>
    public uint Killer { get; set; }

    /// <summary>
    /// Mutable variable store for the current execution run.
    /// Keys are variable names, values are boxed .NET values.
    /// Initialized from the graph's <see cref="GlyphGraph.Variables"/> defaults.
    /// </summary>
    public Dictionary<string, object?> Variables { get; set; } = new();

    /// <summary>
    /// Data pin output cache. Keyed by "nodeInstanceId:pinId", stores computed values
    /// so that multiple downstream consumers don't re-evaluate the same source node.
    /// </summary>
    public Dictionary<string, object?> PinValueCache { get; set; } = new();

    /// <summary>
    /// Cancellation token for cooperative cancellation (e.g., server shutdown).
    /// </summary>
    public CancellationToken CancellationToken { get; init; }

    /// <summary>
    /// Maximum number of node executions allowed per graph run. Prevents infinite loops.
    /// </summary>
    public int MaxExecutionSteps { get; init; } = 10_000;

    /// <summary>
    /// Number of node executions performed so far in this run.
    /// </summary>
    public int ExecutionStepCount { get; set; }

    /// <summary>
    /// Execution log entries for debugging. Only populated when <see cref="EnableTracing"/> is true.
    /// </summary>
    public List<string> TraceLog { get; set; } = [];

    /// <summary>
    /// When true, the interpreter appends trace entries to <see cref="TraceLog"/>.
    /// </summary>
    public bool EnableTracing { get; init; }

    /// <summary>
    /// Stores a computed value in the pin cache for later retrieval.
    /// </summary>
    public void CachePinValue(Guid nodeId, string pinId, object? value)
    {
        PinValueCache[$"{nodeId}:{pinId}"] = value;
    }

    /// <summary>
    /// Tries to retrieve a cached pin value. Returns false if no cached value exists.
    /// </summary>
    public bool TryGetCachedPinValue(Guid nodeId, string pinId, out object? value)
    {
        return PinValueCache.TryGetValue($"{nodeId}:{pinId}", out value);
    }
}
