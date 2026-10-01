using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.Glyph;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.KnowledgeSubsystem.Glyph;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes.Glyph;
using AmiaReforged.PwEngine.Features.Encounters.Models;
using AmiaReforged.PwEngine.Features.Glyph.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Runtime;

/// <summary>
/// Per-execution state with typed domain capabilities and isolated variables/caches.
/// Flat domain properties remain compatibility forwards to the same capability instances.
/// </summary>
public class GlyphExecutionContext
{
    private readonly Dictionary<Type, object> _capabilities = new();
    public T? Get<T>() where T : class => _capabilities.GetValueOrDefault(typeof(T)) as T;
    public void Set<T>(T capability) where T : class
    {
        ArgumentNullException.ThrowIfNull(capability);
        _capabilities[typeof(T)] = capability;
    }
    internal T Ensure<T>() where T : class, new()
    {
        if (Get<T>() is { } capability) return capability;
        var created = new T();
        Set(created);
        return created;
    }

    /// <summary>
    /// The graph being executed.
    /// </summary>
    public required GlyphGraph Graph { get; init; }

    /// <summary>
    /// The encounter context from the spawn system. Null for non-encounter scripts.
    /// </summary>
    public EncounterContext? EncounterContext { get => Ensure<EncounterGlyphContext>().EncounterContext; init => Ensure<EncounterGlyphContext>().EncounterContext = value; }

    /// <summary>
    /// The spawn profile that owns the encounter. Null for non-encounter scripts.
    /// </summary>
    public SpawnProfile? Profile { get => Ensure<EncounterGlyphContext>().Profile; init => Ensure<EncounterGlyphContext>().Profile = value; }

    /// <summary>
    /// The spawn group being processed (null for OnCreatureDeath if group is unknown).
    /// </summary>
    public SpawnGroup? Group { get => Ensure<EncounterGlyphContext>().Group; init => Ensure<EncounterGlyphContext>().Group = value; }

    /// <summary>
    /// The current spawn count, modifiable by BeforeGroupSpawn nodes.
    /// Initialized to the scaled count calculated by the spawner.
    /// </summary>
    public int SpawnCount { get => Ensure<EncounterGlyphContext>().SpawnCount; set => Ensure<EncounterGlyphContext>().SpawnCount = value; }

    /// <summary>
    /// When set to true by a node during BeforeGroupSpawn execution, the spawn
    /// for the current group is cancelled entirely.
    /// </summary>
    public bool ShouldCancelSpawn { get => Ensure<EncounterGlyphContext>().ShouldCancelSpawn; set => Ensure<EncounterGlyphContext>().ShouldCancelSpawn = value; }

    /// <summary>
    /// Object IDs of creatures spawned by the current group (populated for AfterGroupSpawn).
    /// </summary>
    public List<uint> SpawnedCreatures { get => Ensure<EncounterGlyphContext>().SpawnedCreatures; set => Ensure<EncounterGlyphContext>().SpawnedCreatures = value; }

    /// <summary>
    /// NWN object ID of the player creature that triggered the encounter.
    /// Available in all encounter event contexts (BeforeGroupSpawn, AfterGroupSpawn,
    /// OnCreatureSpawn, OnCreatureDeath, OnBossSpawn). Defaults to OBJECT_INVALID.
    /// </summary>
    public uint TriggeringPlayer { get => Ensure<EncounterGlyphContext>().TriggeringPlayer; set => Ensure<EncounterGlyphContext>().TriggeringPlayer = value; }

    /// <summary>
    /// Object ID of the creature that died (populated for OnCreatureDeath).
    /// </summary>
    public uint DeadCreature { get => Ensure<EncounterGlyphContext>().DeadCreature; set => Ensure<EncounterGlyphContext>().DeadCreature = value; }

    /// <summary>
    /// Object ID of the killer (populated for OnCreatureDeath). May be NWScript.OBJECT_INVALID.
    /// </summary>
    public uint Killer { get => Ensure<EncounterGlyphContext>().Killer; set => Ensure<EncounterGlyphContext>().Killer = value; }

    // ==================== Per-Creature Spawn Context ====================

    /// <summary>
    /// Object ID of the single creature being processed during <see cref="GlyphEventType.OnCreatureSpawn"/>
    /// or <see cref="GlyphEventType.OnBossSpawn"/>. Not set for group-level events.
    /// </summary>
    public uint SpawnedCreature { get => Ensure<EncounterGlyphContext>().SpawnedCreature; set => Ensure<EncounterGlyphContext>().SpawnedCreature = value; }

    /// <summary>
    /// The blueprint ResRef of the creature that was spawned (populated for OnCreatureSpawn / OnBossSpawn).
    /// </summary>
    public string? CreatureResRef { get => Ensure<EncounterGlyphContext>().CreatureResRef; set => Ensure<EncounterGlyphContext>().CreatureResRef = value; }

    /// <summary>
    /// Zero-based index of this creature within the group's spawn list (for OnCreatureSpawn).
    /// </summary>
    public int SpawnIndex { get => Ensure<EncounterGlyphContext>().SpawnIndex; set => Ensure<EncounterGlyphContext>().SpawnIndex = value; }

    /// <summary>
    /// Total number of creatures being spawned in the current group (for OnCreatureSpawn).
    /// </summary>
    public int TotalGroupSpawnCount { get => Ensure<EncounterGlyphContext>().TotalGroupSpawnCount; set => Ensure<EncounterGlyphContext>().TotalGroupSpawnCount = value; }

    /// <summary>
    /// When set to true by a node during OnCreatureSpawn execution,
    /// the data-driven bonus pipeline (ApplyBonuses) is skipped for this creature.
    /// </summary>
    public bool ShouldSkipBonuses { get => Ensure<EncounterGlyphContext>().ShouldSkipBonuses; set => Ensure<EncounterGlyphContext>().ShouldSkipBonuses = value; }

    /// <summary>
    /// When set to true by a node during OnCreatureSpawn execution,
    /// the data-driven mutation pipeline (TryApplyMutation) is skipped for this creature.
    /// </summary>
    public bool ShouldSkipMutations { get => Ensure<EncounterGlyphContext>().ShouldSkipMutations; set => Ensure<EncounterGlyphContext>().ShouldSkipMutations = value; }

    /// <summary>
    /// True when the event is firing for a boss or mini-boss creature (OnBossSpawn).
    /// </summary>
    public bool IsBoss { get => Ensure<EncounterGlyphContext>().IsBoss; set => Ensure<EncounterGlyphContext>().IsBoss = value; }

    // ==================== Trait Context ====================

    /// <summary>
    /// The character ID for trait-related scripts (e.g., OnTraitGranted, OnTraitRemoved).
    /// Null for non-trait scripts.
    /// </summary>
    public string? CharacterId { get => Ensure<GlyphCharacterContext>().CharacterId; set => Ensure<GlyphCharacterContext>().CharacterId = value; }

    /// <summary>
    /// The trait tag being granted/removed. Null for non-trait scripts.
    /// </summary>
    public string? TraitTag { get => Ensure<TraitGlyphContext>().TraitTag; set => Ensure<TraitGlyphContext>().TraitTag = value; }

    /// <summary>
    /// Object ID of the creature that the trait is being applied to/removed from.
    /// </summary>
    public uint TargetCreature { get => Ensure<TraitGlyphContext>().TargetCreature; set => Ensure<TraitGlyphContext>().TargetCreature = value; }

    // ==================== Interaction Context ====================

    /// <summary>
    /// NWN object ID of the creature performing the interaction.
    /// Resolved from the character ID at graph execution time. 0 if unavailable.
    /// </summary>
    public uint InteractionCreature { get => Ensure<InteractionGlyphContext>().InteractionCreature; set => Ensure<InteractionGlyphContext>().InteractionCreature = value; }

    /// <summary>
    /// The interaction definition tag (e.g., "prospect_minerals"). Null for non-interaction scripts.
    /// </summary>
    public string? InteractionTag { get => Ensure<InteractionGlyphContext>().InteractionTag; set => Ensure<InteractionGlyphContext>().InteractionTag = value; }

    /// <summary>
    /// The target entity ID for the interaction.
    /// </summary>
    public Guid InteractionTargetId { get => Ensure<InteractionGlyphContext>().InteractionTargetId; set => Ensure<InteractionGlyphContext>().InteractionTargetId = value; }

    /// <summary>
    /// The target mode string ("Node", "Trigger", "Placeable"). Null for non-interaction scripts.
    /// </summary>
    public string? InteractionTargetMode { get => Ensure<InteractionGlyphContext>().InteractionTargetMode; set => Ensure<InteractionGlyphContext>().InteractionTargetMode = value; }

    /// <summary>
    /// The area ResRef where the interaction is taking place. Null for non-interaction scripts.
    /// </summary>
    public string? InteractionAreaResRef { get => Ensure<InteractionGlyphContext>().InteractionAreaResRef; set => Ensure<InteractionGlyphContext>().InteractionAreaResRef = value; }

    /// <summary>
    /// The interaction session ID. <see cref="Guid.Empty"/> for OnInteractionAttempted (no session yet).
    /// </summary>
    public Guid InteractionSessionId { get => Ensure<InteractionGlyphContext>().InteractionSessionId; set => Ensure<InteractionGlyphContext>().InteractionSessionId = value; }

    /// <summary>
    /// Current progress (tick count) of the interaction session.
    /// </summary>
    public int InteractionProgress { get => Ensure<InteractionGlyphContext>().InteractionProgress; set => Ensure<InteractionGlyphContext>().InteractionProgress = value; }

    /// <summary>
    /// Total rounds required for the interaction to complete.
    /// </summary>
    public int InteractionRequiredRounds { get => Ensure<InteractionGlyphContext>().InteractionRequiredRounds; set => Ensure<InteractionGlyphContext>().InteractionRequiredRounds = value; }

    /// <summary>
    /// The character's best proficiency level name (e.g., "Novice", "Expert"). Null if unknown.
    /// </summary>
    public string? InteractionProficiency { get => Ensure<InteractionGlyphContext>().InteractionProficiency; set => Ensure<InteractionGlyphContext>().InteractionProficiency = value; }

    /// <summary>
    /// The selected response tag from the data-driven system. Populated for OnInteractionCompleted.
    /// </summary>
    public string? InteractionResponseTag { get => Ensure<InteractionGlyphContext>().InteractionResponseTag; set => Ensure<InteractionGlyphContext>().InteractionResponseTag = value; }

    /// <summary>
    /// When set to true by a node during OnInteractionAttempted execution,
    /// the interaction is prevented from starting.
    /// </summary>
    public bool ShouldBlockInteraction { get => Ensure<InteractionGlyphContext>().ShouldBlockInteraction; set => Ensure<InteractionGlyphContext>().ShouldBlockInteraction = value; }

    /// <summary>
    /// Rejection message when <see cref="ShouldBlockInteraction"/> is true.
    /// </summary>
    public string? BlockInteractionMessage { get => Ensure<InteractionGlyphContext>().BlockInteractionMessage; set => Ensure<InteractionGlyphContext>().BlockInteractionMessage = value; }

    /// <summary>
    /// When set to true by a node during OnInteractionTick execution,
    /// the interaction is cancelled mid-progress.
    /// </summary>
    public bool ShouldCancelInteraction { get => Ensure<InteractionGlyphContext>().ShouldCancelInteraction; set => Ensure<InteractionGlyphContext>().ShouldCancelInteraction = value; }

    /// <summary>
    /// Cancellation message when <see cref="ShouldCancelInteraction"/> is true.
    /// </summary>
    public string? CancelInteractionMessage { get => Ensure<InteractionGlyphContext>().CancelInteractionMessage; set => Ensure<InteractionGlyphContext>().CancelInteractionMessage = value; }

    /// <summary>
    /// Arbitrary metadata from the interaction command. Null for non-interaction scripts.
    /// </summary>
    public Dictionary<string, object>? InteractionMetadata { get => Ensure<InteractionGlyphContext>().InteractionMetadata; set => Ensure<InteractionGlyphContext>().InteractionMetadata = value; }

    /// <summary>
    /// Reference to the live interaction session. Null for the Attempted stage (session not yet created)
    /// and for non-interaction scripts. Allows nodes to modify session state directly.
    /// </summary>
    public AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions.InteractionSession? Session { get => Ensure<InteractionGlyphContext>().Session; set => Ensure<InteractionGlyphContext>().Session = value; }

    /// <summary>
    /// The TypeId of the pipeline stage currently being executed (e.g., "stage.interaction_attempted").
    /// Used by stage-aware nodes like FailInteraction to determine the appropriate failure behavior.
    /// Null for non-pipeline scripts.
    /// </summary>
    public string? CurrentPipelineStage { get => Ensure<InteractionGlyphContext>().CurrentPipelineStage; set => Ensure<InteractionGlyphContext>().CurrentPipelineStage = value; }

    // ==================== World Engine API ====================

    /// <summary>
    /// Read-only facade for querying World Engine industry and knowledge data.
    /// Set by the hook service that creates the context. Null for non-interaction scripts
    /// or in test environments where the facade is not provided.
    /// </summary>
    private IGlyphWorldEngineApi? _legacyWorldEngine;
    public IGlyphIndustryApi? Industries { get; init; }
    public IGlyphKnowledgeApi? Knowledge { get; init; }
    public IGlyphResourceNodeApi? ResourceNodes { get; init; }
    /// <summary>Compatibility initialization forwards to the same narrow APIs used by new executors.</summary>
    public IGlyphWorldEngineApi? WorldEngine
    {
        get => _legacyWorldEngine;
        init { _legacyWorldEngine = value; Industries = value; Knowledge = value; ResourceNodes = value; }
    }

    // ==================== Variables & Cache ====================

    /// Mutable variable store for the current execution run.
    /// Keys are variable names, values are boxed .NET values.
    /// Initialized from the graph's <see cref="GlyphGraph.Variables"/> defaults.
    /// </summary>
    /// <summary>Compiler-assigned identities isolate lexical locals from graph variables.</summary>
    public Dictionary<int, GlyphLocalValue> Locals { get; } = new();

    /// <summary>Executor-owned iteration state, keyed by the loop owner.</summary>
    public Dictionary<Guid, object> LoopStates { get; } = new();

    public Dictionary<string, object?> Variables { get; set; } = new();

    /// <summary>
    /// Data pin output cache. Keyed by "nodeInstanceId:pinId", stores computed values
    /// so that multiple downstream consumers don't re-evaluate the same source node.
    /// </summary>
    public Dictionary<string, object?> PinValueCache { get; set; } = new();

    /// <summary>
    /// Reference to the active execution stack, set during <c>FollowExecChain</c>.
    /// Used by the interpreter to record lazily-evaluated data nodes in loop frames
    /// so their cached outputs can be invalidated on the next loop iteration.
    /// </summary>
    internal Stack<GlyphExecFrame>? ActiveStack { get; set; }

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

    internal bool ExecutionHalted { get; set; }

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
