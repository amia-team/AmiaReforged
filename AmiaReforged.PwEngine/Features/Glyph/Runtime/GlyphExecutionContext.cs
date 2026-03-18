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
    /// The encounter context from the spawn system. Null for non-encounter scripts.
    /// </summary>
    public EncounterContext? EncounterContext { get; init; }

    /// <summary>
    /// The spawn profile that owns the encounter. Null for non-encounter scripts.
    /// </summary>
    public SpawnProfile? Profile { get; init; }

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
    /// NWN object ID of the player creature that triggered the encounter.
    /// Available in all encounter event contexts (BeforeGroupSpawn, AfterGroupSpawn,
    /// OnCreatureSpawn, OnCreatureDeath, OnBossSpawn). Defaults to OBJECT_INVALID.
    /// </summary>
    public uint TriggeringPlayer { get; set; }

    /// <summary>
    /// Object ID of the creature that died (populated for OnCreatureDeath).
    /// </summary>
    public uint DeadCreature { get; set; }

    /// <summary>
    /// Object ID of the killer (populated for OnCreatureDeath). May be NWScript.OBJECT_INVALID.
    /// </summary>
    public uint Killer { get; set; }

    // ==================== Per-Creature Spawn Context ====================

    /// <summary>
    /// Object ID of the single creature being processed during <see cref="GlyphEventType.OnCreatureSpawn"/>
    /// or <see cref="GlyphEventType.OnBossSpawn"/>. Not set for group-level events.
    /// </summary>
    public uint SpawnedCreature { get; set; }

    /// <summary>
    /// The blueprint ResRef of the creature that was spawned (populated for OnCreatureSpawn / OnBossSpawn).
    /// </summary>
    public string? CreatureResRef { get; set; }

    /// <summary>
    /// Zero-based index of this creature within the group's spawn list (for OnCreatureSpawn).
    /// </summary>
    public int SpawnIndex { get; set; }

    /// <summary>
    /// Total number of creatures being spawned in the current group (for OnCreatureSpawn).
    /// </summary>
    public int TotalGroupSpawnCount { get; set; }

    /// <summary>
    /// When set to true by a node during OnCreatureSpawn execution,
    /// the data-driven bonus pipeline (ApplyBonuses) is skipped for this creature.
    /// </summary>
    public bool ShouldSkipBonuses { get; set; }

    /// <summary>
    /// When set to true by a node during OnCreatureSpawn execution,
    /// the data-driven mutation pipeline (TryApplyMutation) is skipped for this creature.
    /// </summary>
    public bool ShouldSkipMutations { get; set; }

    /// <summary>
    /// True when the event is firing for a boss or mini-boss creature (OnBossSpawn).
    /// </summary>
    public bool IsBoss { get; set; }

    // ==================== Trait Context ====================

    /// <summary>
    /// The character ID for trait-related scripts (e.g., OnTraitGranted, OnTraitRemoved).
    /// Null for non-trait scripts.
    /// </summary>
    public string? CharacterId { get; set; }

    /// <summary>
    /// The trait tag being granted/removed. Null for non-trait scripts.
    /// </summary>
    public string? TraitTag { get; set; }

    /// <summary>
    /// Object ID of the creature that the trait is being applied to/removed from.
    /// </summary>
    public uint TargetCreature { get; set; }

    // ==================== Interaction Context ====================

    /// <summary>
    /// NWN object ID of the creature performing the interaction.
    /// Resolved from the character ID at graph execution time. 0 if unavailable.
    /// </summary>
    public uint InteractionCreature { get; set; }

    /// <summary>
    /// The interaction definition tag (e.g., "prospect_minerals"). Null for non-interaction scripts.
    /// </summary>
    public string? InteractionTag { get; set; }

    /// <summary>
    /// The target entity ID for the interaction.
    /// </summary>
    public Guid InteractionTargetId { get; set; }

    /// <summary>
    /// The target mode string ("Node", "Trigger", "Placeable"). Null for non-interaction scripts.
    /// </summary>
    public string? InteractionTargetMode { get; set; }

    /// <summary>
    /// The area ResRef where the interaction is taking place. Null for non-interaction scripts.
    /// </summary>
    public string? InteractionAreaResRef { get; set; }

    /// <summary>
    /// The interaction session ID. <see cref="Guid.Empty"/> for OnInteractionAttempted (no session yet).
    /// </summary>
    public Guid InteractionSessionId { get; set; }

    /// <summary>
    /// Current progress (tick count) of the interaction session.
    /// </summary>
    public int InteractionProgress { get; set; }

    /// <summary>
    /// Total rounds required for the interaction to complete.
    /// </summary>
    public int InteractionRequiredRounds { get; set; }

    /// <summary>
    /// The character's best proficiency level name (e.g., "Novice", "Expert"). Null if unknown.
    /// </summary>
    public string? InteractionProficiency { get; set; }

    /// <summary>
    /// The selected response tag from the data-driven system. Populated for OnInteractionCompleted.
    /// </summary>
    public string? InteractionResponseTag { get; set; }

    /// <summary>
    /// When set to true by a node during OnInteractionAttempted execution,
    /// the interaction is prevented from starting.
    /// </summary>
    public bool ShouldBlockInteraction { get; set; }

    /// <summary>
    /// Rejection message when <see cref="ShouldBlockInteraction"/> is true.
    /// </summary>
    public string? BlockInteractionMessage { get; set; }

    /// <summary>
    /// When set to true by a node during OnInteractionTick execution,
    /// the interaction is cancelled mid-progress.
    /// </summary>
    public bool ShouldCancelInteraction { get; set; }

    /// <summary>
    /// Cancellation message when <see cref="ShouldCancelInteraction"/> is true.
    /// </summary>
    public string? CancelInteractionMessage { get; set; }

    /// <summary>
    /// Arbitrary metadata from the interaction command. Null for non-interaction scripts.
    /// </summary>
    public Dictionary<string, object>? InteractionMetadata { get; set; }

    /// <summary>
    /// Reference to the live interaction session. Null for the Attempted stage (session not yet created)
    /// and for non-interaction scripts. Allows nodes to modify session state directly.
    /// </summary>
    public AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions.InteractionSession? Session { get; set; }

    /// <summary>
    /// The TypeId of the pipeline stage currently being executed (e.g., "stage.interaction_attempted").
    /// Used by stage-aware nodes like FailInteraction to determine the appropriate failure behavior.
    /// Null for non-pipeline scripts.
    /// </summary>
    public string? CurrentPipelineStage { get; set; }

    // ==================== World Engine API ====================

    /// <summary>
    /// Read-only facade for querying World Engine industry and knowledge data.
    /// Set by the hook service that creates the context. Null for non-interaction scripts
    /// or in test environments where the facade is not provided.
    /// </summary>
    public IGlyphWorldEngineApi? WorldEngine { get; init; }

    // ==================== Variables & Cache ====================

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
