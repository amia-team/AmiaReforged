using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Persistence;
using AmiaReforged.PwEngine.Features.Glyph.Runtime;
using AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Interactions;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions.Events;
using Anvil.API;
using Anvil.Services;
using NLog;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Integration;

/// <summary>
/// Connects the Glyph interpreter to interaction lifecycle events using the pipeline model.
/// A single InteractionPipeline graph contains four stage nodes (Attempted → Started → Tick → Completed).
/// Each stage is executed independently when its lifecycle event fires.
/// <para>
/// Provides synchronous hooks for Attempted/Tick (called by the command handler)
/// and async event handlers for Started/Completed (fired via event bus).
/// </para>
/// </summary>
[ServiceBinding(typeof(GlyphInteractionHookService))]
[ServiceBinding(typeof(IEventHandler<InteractionStartedEvent>))]
[ServiceBinding(typeof(IEventHandler<InteractionCompletedEvent>))]
public class GlyphInteractionHookService
    : IEventHandler<InteractionStartedEvent>,
      IEventHandler<InteractionCompletedEvent>,
      IEventHandlerMarker
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly GlyphBootstrap _bootstrap;
    private readonly IGlyphRepository _repository;
    private readonly IInteractionSessionManager _sessionManager;

    /// <summary>
    /// Cache of active pipeline bindings keyed by InteractionTag.
    /// Each entry contains pipeline graphs paired with their optional area scope.
    /// </summary>
    private Dictionary<string, List<CachedBinding>> _cache = new(StringComparer.OrdinalIgnoreCase);

    public GlyphInteractionHookService(
        GlyphBootstrap bootstrap,
        IGlyphRepository repository,
        IInteractionSessionManager sessionManager)
    {
        _bootstrap = bootstrap;
        _repository = repository;
        _sessionManager = sessionManager;

        RefreshCacheAsync().GetAwaiter().GetResult();
        Log.Info("GlyphInteractionHookService initialized with {Count} cached interaction bindings.", _cache.Count);
    }

    /// <summary>
    /// Refreshes the interaction binding cache from the database.
    /// Called by the API controller after binding mutations.
    /// </summary>
    public async Task RefreshCacheAsync()
    {
        List<InteractionGlyphBinding> bindings = await _repository.GetAllInteractionBindingsAsync();
        Log.Info("[Glyph] Refreshing cache: {Count} raw bindings loaded from DB.", bindings.Count);

        Dictionary<string, List<CachedBinding>> newCache = new(StringComparer.OrdinalIgnoreCase);

        foreach (InteractionGlyphBinding binding in bindings)
        {
            if (binding.GlyphDefinition is not { IsActive: true })
            {
                Log.Debug("[Glyph] Skipping binding {Id}: GlyphDefinition is null or inactive. " +
                          "Tag='{Tag}', DefId={DefId}",
                    binding.Id, binding.InteractionTag, binding.GlyphDefinitionId);
                continue;
            }

            // Only cache InteractionPipeline definitions
            if (binding.GlyphDefinition.EventType != nameof(GlyphEventType.InteractionPipeline))
            {
                Log.Debug("[Glyph] Skipping binding {Id}: EventType '{EventType}' is not InteractionPipeline.",
                    binding.Id, binding.GlyphDefinition.EventType);
                continue;
            }

            GlyphGraph? graph = DeserializeGraph(binding.GlyphDefinition);
            if (graph == null) continue;

            if (!newCache.TryGetValue(binding.InteractionTag, out List<CachedBinding>? list))
            {
                list = [];
                newCache[binding.InteractionTag] = list;
            }

            list.Add(new CachedBinding(graph, binding.AreaResRef, binding.Priority));

            Log.Info("[Glyph] Cached pipeline binding: tag='{Tag}', graph='{Name}' " +
                     "(nodes={Nodes}, edges={Edges}), area={Area}, priority={Priority}",
                binding.InteractionTag, graph.Name,
                graph.Nodes.Count, graph.Edges.Count,
                binding.AreaResRef ?? "(global)", binding.Priority);
        }

        // Sort each list by priority
        foreach (List<CachedBinding> list in newCache.Values)
        {
            list.Sort((a, b) => a.Priority.CompareTo(b.Priority));
        }

        _cache = newCache;
        Log.Info("Glyph interaction binding cache refreshed: {Count} interaction tags.", newCache.Count);
    }

    // ==================== Synchronous Hooks ====================
    // Called directly by PerformInteractionCommandHandler (not via event bus)

    /// <summary>
    /// Runs the Attempted stage of pipeline graphs for the given interaction tag.
    /// Returns whether the interaction should be blocked and an optional rejection message.
    /// </summary>
    public (bool ShouldBlock, string? Message) RunOnInteractionAttempted(
        string interactionTag,
        string characterId,
        Guid targetId,
        string targetMode,
        string? areaResRef,
        string? proficiency,
        Dictionary<string, object>? metadata)
    {
        Log.Debug("[Glyph] OnAttempted hook fired for interaction '{Tag}', character={CharId}, area={Area}",
            interactionTag, characterId, areaResRef ?? "(null)");

        List<GlyphGraph> graphs = GetMatchingGraphs(interactionTag, areaResRef);

        if (graphs.Count == 0)
        {
            Log.Debug("[Glyph] OnAttempted: no matching pipeline graphs for '{Tag}'. Cache has {Count} entries: [{Keys}]",
                interactionTag, _cache.Count,
                string.Join(", ", _cache.Keys));
            return (false, null);
        }

        Log.Debug("[Glyph] OnAttempted: found {Count} matching pipeline graph(s) for '{Tag}'", graphs.Count, interactionTag);

        foreach (GlyphGraph graph in graphs)
        {
            uint creatureId = ResolveCreatureObjectId(characterId);
            Log.Debug("[Glyph] OnAttempted: executing stage '{Stage}' of graph '{Name}', creature=0x{Creature:X}",
                InteractionAttemptedStageExecutor.NodeTypeId, graph.Name, creatureId);

            GlyphExecutionContext ctx = CreateInteractionContext(graph, interactionTag, characterId,
                targetId, targetMode, areaResRef, proficiency, metadata,
                creatureObjectId: creatureId);
            try
            {
                _bootstrap.Interpreter.ExecuteStageAsync(ctx, InteractionAttemptedStageExecutor.NodeTypeId)
                    .GetAwaiter().GetResult();
                DumpTraceLog(ctx, "OnAttempted");

                if (ctx.ShouldBlockInteraction)
                {
                    Log.Info("Glyph pipeline '{Name}' blocked interaction '{Tag}' for character {CharId}: {Message}",
                        graph.Name, interactionTag, characterId, ctx.BlockInteractionMessage);
                    return (true, ctx.BlockInteractionMessage);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error executing Attempted stage of pipeline '{Name}' for '{Tag}'.",
                    graph.Name, interactionTag);
            }
        }

        return (false, null);
    }

    /// <summary>
    /// Runs the Tick stage of pipeline graphs for the given interaction.
    /// Returns whether the interaction should be cancelled and an optional message.
    /// </summary>
    public (bool ShouldCancel, string? Message) RunOnInteractionTick(
        string interactionTag,
        string characterId,
        Guid targetId,
        string? areaResRef,
        Guid sessionId,
        int progress,
        int requiredRounds,
        string? proficiency,
        Dictionary<string, object>? metadata)
    {
        Log.Debug("[Glyph] OnTick hook fired for interaction '{Tag}', tick {Progress}/{Total}",
            interactionTag, progress, requiredRounds);

        List<GlyphGraph> graphs = GetMatchingGraphs(interactionTag, areaResRef);
        if (graphs.Count == 0) return (false, null);

        Log.Debug("[Glyph] OnTick: found {Count} matching pipeline graph(s) for '{Tag}'", graphs.Count, interactionTag);

        // Check if OnInteractionTick has been suppressed by a prior stage
        InteractionSession? tickSession = Guid.TryParse(characterId, out Guid tickCharGuid)
            ? _sessionManager.GetActiveSession(new CharacterId(tickCharGuid))
            : null;
        if (tickSession?.SuppressedEventTypes.Contains(InteractionTickStageExecutor.NodeTypeId) == true)
        {
            Log.Info("[Glyph] Tick stage suppressed for '{Tag}' by prior script", interactionTag);
            return (false, null);
        }

        foreach (GlyphGraph graph in graphs)
        {
            uint creatureId = ResolveCreatureObjectId(characterId);
            GlyphExecutionContext ctx = CreateInteractionContext(graph, interactionTag, characterId,
                targetId, null, areaResRef, proficiency, metadata,
                creatureObjectId: creatureId);
            ctx.Session = tickSession;
            ctx.InteractionSessionId = sessionId;
            ctx.InteractionProgress = progress;
            ctx.InteractionRequiredRounds = requiredRounds;

            try
            {
                _bootstrap.Interpreter.ExecuteStageAsync(ctx, InteractionTickStageExecutor.NodeTypeId)
                    .GetAwaiter().GetResult();
                DumpTraceLog(ctx, "OnTick");

                if (ctx.ShouldCancelInteraction)
                {
                    Log.Info("Glyph pipeline '{Name}' cancelled interaction '{Tag}' at tick {Progress}/{Total}: {Message}",
                        graph.Name, interactionTag, progress, requiredRounds, ctx.CancelInteractionMessage);
                    return (true, ctx.CancelInteractionMessage);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error executing Tick stage of pipeline '{Name}' for '{Tag}'.",
                    graph.Name, interactionTag);
            }
        }

        return (false, null);
    }

    // ==================== Async Event Handlers ====================

    /// <summary>
    /// Handles <see cref="InteractionStartedEvent"/> by running the Started stage of pipeline graphs.
    /// </summary>
    public async Task HandleAsync(InteractionStartedEvent @event, CancellationToken cancellationToken = default)
    {
        Log.Debug("[Glyph] OnStarted event received for interaction '{Tag}', character={CharId}",
            @event.InteractionTag, @event.CharacterId);

        await SwitchToMainThreadSafe();

        List<GlyphGraph> graphs = GetMatchingGraphs(@event.InteractionTag, areaResRef: null);

        if (graphs.Count == 0)
        {
            Log.Debug("[Glyph] OnStarted: no matching pipeline graphs for '{Tag}'", @event.InteractionTag);
            return;
        }

        Log.Debug("[Glyph] OnStarted: found {Count} matching pipeline graph(s) for '{Tag}'", graphs.Count, @event.InteractionTag);

        // Check suppression
        InteractionSession? startedSession = _sessionManager.GetActiveSession(new CharacterId(@event.CharacterId));
        if (startedSession?.SuppressedEventTypes.Contains(InteractionStartedStageExecutor.NodeTypeId) == true)
        {
            Log.Info("[Glyph] Started stage suppressed for '{Tag}' by prior script", @event.InteractionTag);
            return;
        }

        foreach (GlyphGraph graph in graphs)
        {
            uint creatureId = ResolveCreatureObjectId(@event.CharacterId.ToString());
            Log.Debug("[Glyph] OnStarted: executing stage '{Stage}' of graph '{Name}', creature=0x{Creature:X}",
                InteractionStartedStageExecutor.NodeTypeId, graph.Name, creatureId);

            GlyphExecutionContext ctx = CreateInteractionContext(graph,
                @event.InteractionTag,
                @event.CharacterId.ToString(),
                @event.TargetId,
                targetMode: null,
                areaResRef: null,
                proficiency: null,
                metadata: null,
                cancellationToken,
                creatureObjectId: creatureId);
            ctx.Session = startedSession;
            ctx.InteractionSessionId = @event.SessionId;
            ctx.InteractionRequiredRounds = @event.RequiredRounds;

            try
            {
                await _bootstrap.Interpreter.ExecuteStageAsync(ctx, InteractionStartedStageExecutor.NodeTypeId);
                DumpTraceLog(ctx, "OnStarted");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error executing Started stage of pipeline '{Name}' for '{Tag}'.",
                    graph.Name, @event.InteractionTag);
            }
        }
    }

    /// <summary>
    /// Handles <see cref="InteractionCompletedEvent"/> by running the Completed stage of pipeline graphs.
    /// These run as augmentation — the data-driven response system processes after this event.
    /// </summary>
    public async Task HandleAsync(InteractionCompletedEvent @event, CancellationToken cancellationToken = default)
    {
        Log.Debug("[Glyph] OnCompleted event received for interaction '{Tag}', character={CharId}, success={Success}",
            @event.InteractionTag, @event.CharacterId, @event.Success);

        await SwitchToMainThreadSafe();

        List<GlyphGraph> graphs = GetMatchingGraphs(@event.InteractionTag, areaResRef: null);

        if (graphs.Count == 0)
        {
            Log.Debug("[Glyph] OnCompleted: no matching pipeline graphs for '{Tag}'. Cache has {Count} entries: [{Keys}]",
                @event.InteractionTag, _cache.Count,
                string.Join(", ", _cache.Keys));
            return;
        }

        Log.Debug("[Glyph] OnCompleted: found {Count} matching pipeline graph(s) for '{Tag}'", graphs.Count, @event.InteractionTag);

        // Check suppression
        InteractionSession? completedSession = _sessionManager.GetActiveSession(new CharacterId(@event.CharacterId));
        if (completedSession?.SuppressedEventTypes.Contains(InteractionCompletedStageExecutor.NodeTypeId) == true)
        {
            Log.Info("[Glyph] Completed stage suppressed for '{Tag}' by prior script", @event.InteractionTag);
            return;
        }

        foreach (GlyphGraph graph in graphs)
        {
            uint creatureId = ResolveCreatureObjectId(@event.CharacterId.ToString());
            Log.Debug("[Glyph] OnCompleted: executing stage '{Stage}' of graph '{Name}', creature=0x{Creature:X}",
                InteractionCompletedStageExecutor.NodeTypeId, graph.Name, creatureId);

            GlyphExecutionContext ctx = CreateInteractionContext(graph,
                @event.InteractionTag,
                @event.CharacterId.ToString(),
                @event.TargetId,
                targetMode: null,
                areaResRef: null,
                proficiency: null,
                metadata: null,
                cancellationToken,
                creatureObjectId: creatureId);
            ctx.Session = completedSession;
            ctx.InteractionSessionId = @event.SessionId;

            try
            {
                await _bootstrap.Interpreter.ExecuteStageAsync(ctx, InteractionCompletedStageExecutor.NodeTypeId);
                DumpTraceLog(ctx, "OnCompleted");
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error executing Completed stage of pipeline '{Name}' for '{Tag}'.",
                    graph.Name, @event.InteractionTag);
            }
        }
    }

    // ==================== Private Helpers ====================

    /// <summary>
    /// Gets matching pipeline graphs for an interaction tag, considering area scope.
    /// Returns global bindings (AreaResRef = null) + exact area matches.
    /// </summary>
    private List<GlyphGraph> GetMatchingGraphs(string interactionTag, string? areaResRef)
    {
        if (!_cache.TryGetValue(interactionTag, out List<CachedBinding>? bindings)) return [];

        List<GlyphGraph> result = [];
        foreach (CachedBinding binding in bindings)
        {
            if (binding.AreaResRef == null ||
                string.Equals(binding.AreaResRef, areaResRef, StringComparison.OrdinalIgnoreCase))
            {
                result.Add(binding.Graph);
            }
        }

        return result;
    }

    private static GlyphExecutionContext CreateInteractionContext(
        GlyphGraph graph,
        string interactionTag,
        string characterId,
        Guid targetId,
        string? targetMode,
        string? areaResRef,
        string? proficiency,
        Dictionary<string, object>? metadata,
        CancellationToken cancellationToken = default,
        uint creatureObjectId = 0)
    {
        return new GlyphExecutionContext
        {
            Graph = graph,
            CharacterId = characterId,
            InteractionTag = interactionTag,
            InteractionTargetId = targetId,
            InteractionTargetMode = targetMode,
            InteractionAreaResRef = areaResRef,
            InteractionProficiency = proficiency,
            InteractionMetadata = metadata,
            InteractionCreature = creatureObjectId,
            MaxExecutionSteps = 10_000,
            EnableTracing = true,
            CancellationToken = cancellationToken
        };
    }

    /// <summary>
    /// Attempts to resolve the NWN creature object ID from a character ID string.
    /// Returns 0 if resolution fails (e.g., in test environments without a live server).
    /// </summary>
    private static uint ResolveCreatureObjectId(string? characterId)
    {
        if (string.IsNullOrEmpty(characterId))
        {
            Log.Debug("[Glyph] ResolveCreatureObjectId: characterId is null/empty, returning 0");
            return 0;
        }

        try
        {
            uint result = NWScript.GetObjectByUUID(characterId);
            Log.Debug("[Glyph] ResolveCreatureObjectId: UUID '{CharId}' -> 0x{Result:X} (invalid={IsInvalid})",
                characterId, result, result == NWScript.OBJECT_INVALID);
            return result;
        }
        catch (Exception ex)
        {
            Log.Warn("[Glyph] ResolveCreatureObjectId: failed for UUID '{CharId}': {Error}",
                characterId, ex.Message);
            return 0;
        }
    }

    private GlyphGraph? DeserializeGraph(GlyphDefinition definition)
    {
        return GlyphGraphSerializer.Deserialize(definition);
    }

    /// <summary>
    /// Dumps the execution trace log to NLog Debug output for diagnosing script execution.
    /// </summary>
    private static void DumpTraceLog(GlyphExecutionContext ctx, string hookName)
    {
        if (ctx.TraceLog.Count == 0)
        {
            Log.Debug("[Glyph] {Hook} trace: (empty — no trace entries recorded)", hookName);
            return;
        }

        Log.Debug("[Glyph] {Hook} trace ({Count} entries) for graph '{Name}':",
            hookName, ctx.TraceLog.Count, ctx.Graph.Name);
        foreach (string entry in ctx.TraceLog)
        {
            Log.Debug("[Glyph]   {Entry}", entry);
        }
    }

    /// <summary>
    /// Switches to the NWN main thread if running under Anvil.
    /// No-ops gracefully in test environments where NwTask is unavailable.
    /// </summary>
    private static async Task SwitchToMainThreadSafe()
    {
        try
        {
            await NwTask.SwitchToMainThread();
        }
        catch (NullReferenceException)
        {
            // Test environment — no Anvil synchronization context.
        }
    }

    private sealed record CachedBinding(GlyphGraph Graph, string? AreaResRef, int Priority);
}
