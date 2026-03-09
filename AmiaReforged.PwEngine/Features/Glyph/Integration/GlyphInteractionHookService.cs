using System.Text.Json;
using System.Text.Json.Serialization;
using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Persistence;
using AmiaReforged.PwEngine.Features.Glyph.Runtime;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.Glyph.Integration;

/// <summary>
/// Connects the Glyph interpreter to interaction lifecycle events.
/// Provides synchronous hooks for Attempted/Tick (called by the command handler)
/// and async event handlers for Started/Completed (fired via event bus).
/// <para>
/// Glyph augments the data-driven interaction system:
/// • OnAttempted/OnStarted/OnTick are Glyph-only hooks
/// • OnCompleted runs Glyph first, then the data-driven response system processes normally
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

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly GlyphBootstrap _bootstrap;
    private readonly IGlyphRepository _repository;

    /// <summary>
    /// Cache of active interaction bindings keyed by (InteractionTag, EventType).
    /// Each entry contains graphs paired with their optional area scope.
    /// </summary>
    private Dictionary<(string Tag, GlyphEventType EventType), List<CachedBinding>> _cache = new();

    public GlyphInteractionHookService(
        GlyphBootstrap bootstrap,
        IGlyphRepository repository)
    {
        _bootstrap = bootstrap;
        _repository = repository;

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
        Dictionary<(string, GlyphEventType), List<CachedBinding>> newCache = new();

        foreach (InteractionGlyphBinding binding in bindings)
        {
            if (binding.GlyphDefinition is not { IsActive: true }) continue;

            if (!Enum.TryParse<GlyphEventType>(binding.GlyphDefinition.EventType, out GlyphEventType eventType))
            {
                Log.Warn("Interaction Glyph binding {Id} has unknown event type '{EventType}'. Skipping.",
                    binding.Id, binding.GlyphDefinition.EventType);
                continue;
            }

            GlyphGraph? graph = DeserializeGraph(binding.GlyphDefinition);
            if (graph == null) continue;

            var key = (binding.InteractionTag, eventType);
            if (!newCache.TryGetValue(key, out List<CachedBinding>? list))
            {
                list = [];
                newCache[key] = list;
            }

            list.Add(new CachedBinding(graph, binding.AreaResRef, binding.Priority));
        }

        // Sort each list by priority
        foreach (List<CachedBinding> list in newCache.Values)
        {
            list.Sort((a, b) => a.Priority.CompareTo(b.Priority));
        }

        _cache = newCache;
        Log.Info("Glyph interaction binding cache refreshed: {Count} tag-event combinations.", newCache.Count);
    }

    // ==================== Synchronous Hooks ====================
    // Called directly by PerformInteractionCommandHandler (not via event bus)

    /// <summary>
    /// Runs OnInteractionAttempted graphs for the given interaction tag before preconditions.
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
        List<GlyphGraph> graphs = GetMatchingGraphs(interactionTag, GlyphEventType.OnInteractionAttempted, areaResRef);
        if (graphs.Count == 0) return (false, null);

        foreach (GlyphGraph graph in graphs)
        {
            GlyphExecutionContext ctx = CreateInteractionContext(graph, interactionTag, characterId,
                targetId, targetMode, areaResRef, proficiency, metadata);

            try
            {
                _bootstrap.Interpreter.ExecuteAsync(ctx).GetAwaiter().GetResult();

                if (ctx.ShouldBlockInteraction)
                {
                    Log.Info("Glyph script '{Name}' blocked interaction '{Tag}' for character {CharId}: {Message}",
                        graph.Name, interactionTag, characterId, ctx.BlockInteractionMessage);
                    return (true, ctx.BlockInteractionMessage);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error executing OnInteractionAttempted Glyph graph '{Name}' for '{Tag}'.",
                    graph.Name, interactionTag);
            }
        }

        return (false, null);
    }

    /// <summary>
    /// Runs OnInteractionTick graphs for the given interaction.
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
        List<GlyphGraph> graphs = GetMatchingGraphs(interactionTag, GlyphEventType.OnInteractionTick, areaResRef);
        if (graphs.Count == 0) return (false, null);

        foreach (GlyphGraph graph in graphs)
        {
            GlyphExecutionContext ctx = CreateInteractionContext(graph, interactionTag, characterId,
                targetId, null, areaResRef, proficiency, metadata);
            ctx.InteractionSessionId = sessionId;
            ctx.InteractionProgress = progress;
            ctx.InteractionRequiredRounds = requiredRounds;

            try
            {
                _bootstrap.Interpreter.ExecuteAsync(ctx).GetAwaiter().GetResult();

                if (ctx.ShouldCancelInteraction)
                {
                    Log.Info("Glyph script '{Name}' cancelled interaction '{Tag}' at tick {Progress}/{Total}: {Message}",
                        graph.Name, interactionTag, progress, requiredRounds, ctx.CancelInteractionMessage);
                    return (true, ctx.CancelInteractionMessage);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error executing OnInteractionTick Glyph graph '{Name}' for '{Tag}'.",
                    graph.Name, interactionTag);
            }
        }

        return (false, null);
    }

    // ==================== Async Event Handlers ====================

    /// <summary>
    /// Handles <see cref="InteractionStartedEvent"/> by running OnInteractionStarted graphs.
    /// </summary>
    public async Task HandleAsync(InteractionStartedEvent @event, CancellationToken cancellationToken = default)
    {
        List<GlyphGraph> graphs = GetMatchingGraphs(
            @event.InteractionTag, GlyphEventType.OnInteractionStarted, areaResRef: null);
        if (graphs.Count == 0) return;

        foreach (GlyphGraph graph in graphs)
        {
            GlyphExecutionContext ctx = CreateInteractionContext(graph,
                @event.InteractionTag,
                @event.CharacterId.ToString(),
                @event.TargetId,
                targetMode: null,
                areaResRef: null,
                proficiency: null,
                metadata: null,
                cancellationToken);
            ctx.InteractionSessionId = @event.SessionId;
            ctx.InteractionRequiredRounds = @event.RequiredRounds;

            try
            {
                await _bootstrap.Interpreter.ExecuteAsync(ctx);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error executing OnInteractionStarted Glyph graph '{Name}' for '{Tag}'.",
                    graph.Name, @event.InteractionTag);
            }
        }
    }

    /// <summary>
    /// Handles <see cref="InteractionCompletedEvent"/> by running OnInteractionCompleted graphs.
    /// These run as augmentation — the data-driven response system processes after this event.
    /// </summary>
    public async Task HandleAsync(InteractionCompletedEvent @event, CancellationToken cancellationToken = default)
    {
        List<GlyphGraph> graphs = GetMatchingGraphs(
            @event.InteractionTag, GlyphEventType.OnInteractionCompleted, areaResRef: null);
        if (graphs.Count == 0) return;

        foreach (GlyphGraph graph in graphs)
        {
            GlyphExecutionContext ctx = CreateInteractionContext(graph,
                @event.InteractionTag,
                @event.CharacterId.ToString(),
                @event.TargetId,
                targetMode: null,
                areaResRef: null,
                proficiency: null,
                metadata: null,
                cancellationToken);
            ctx.InteractionSessionId = @event.SessionId;

            try
            {
                await _bootstrap.Interpreter.ExecuteAsync(ctx);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error executing OnInteractionCompleted Glyph graph '{Name}' for '{Tag}'.",
                    graph.Name, @event.InteractionTag);
            }
        }
    }

    // ==================== Private Helpers ====================

    /// <summary>
    /// Gets matching graphs for a tag + event type, considering area scope.
    /// Returns global bindings (AreaResRef = null) + exact area matches.
    /// </summary>
    private List<GlyphGraph> GetMatchingGraphs(string interactionTag, GlyphEventType eventType, string? areaResRef)
    {
        var key = (interactionTag, eventType);
        if (!_cache.TryGetValue(key, out List<CachedBinding>? bindings)) return [];

        List<GlyphGraph> result = [];
        foreach (CachedBinding binding in bindings)
        {
            // Include if: no area restriction (global) OR area matches exactly
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
        CancellationToken cancellationToken = default)
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
            MaxExecutionSteps = 10_000,
            EnableTracing = false,
            CancellationToken = cancellationToken
        };
    }

    private GlyphGraph? DeserializeGraph(GlyphDefinition definition)
    {
        try
        {
            GlyphGraph? graph = JsonSerializer.Deserialize<GlyphGraph>(definition.GraphJson, JsonOptions);
            if (graph != null)
            {
                graph.Name = definition.Name;
                graph.Description = definition.Description ?? string.Empty;
                if (Enum.TryParse<GlyphEventType>(definition.EventType, out GlyphEventType et))
                    graph.EventType = et;
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
    /// A cached binding entry containing the deserialized graph, optional area scope, and priority.
    /// </summary>
    private sealed record CachedBinding(GlyphGraph Graph, string? AreaResRef, int Priority);
}
