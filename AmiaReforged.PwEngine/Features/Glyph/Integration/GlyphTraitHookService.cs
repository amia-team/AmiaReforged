using System.Text.Json;
using System.Text.Json.Serialization;
using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Persistence;
using AmiaReforged.PwEngine.Features.Glyph.Runtime;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.Glyph.Integration;

/// <summary>
/// Connects the Glyph interpreter to trait domain events.
/// Listens for <see cref="TraitSelectedEvent"/> (grant) and <see cref="TraitDeselectedEvent"/> (remove),
/// then executes any Glyph graphs bound to the affected trait tag.
/// </summary>
[ServiceBinding(typeof(GlyphTraitHookService))]
[ServiceBinding(typeof(IEventHandler<TraitSelectedEvent>))]
[ServiceBinding(typeof(IEventHandler<TraitDeselectedEvent>))]
public class GlyphTraitHookService
    : IEventHandler<TraitSelectedEvent>,
      IEventHandler<TraitDeselectedEvent>,
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
    private readonly ITraitSubsystem _traitSubsystem;

    /// <summary>
    /// Cache of active trait bindings keyed by (TraitTag, EventType).
    /// </summary>
    private Dictionary<(string TraitTag, GlyphEventType EventType), List<GlyphGraph>> _traitBindingCache = new();

    public GlyphTraitHookService(
        GlyphBootstrap bootstrap,
        IGlyphRepository repository,
        ITraitSubsystem traitSubsystem)
    {
        _bootstrap = bootstrap;
        _repository = repository;
        _traitSubsystem = traitSubsystem;

        RefreshCacheAsync().GetAwaiter().GetResult();
        Log.Info("GlyphTraitHookService initialized with {Count} cached trait bindings.", _traitBindingCache.Count);
    }

    /// <summary>
    /// Refreshes the trait binding cache from the database.
    /// </summary>
    public async Task RefreshCacheAsync()
    {
        List<TraitGlyphBinding> bindings = await _repository.GetAllTraitBindingsAsync();
        Dictionary<(string, GlyphEventType), List<GlyphGraph>> newCache = new();

        foreach (TraitGlyphBinding binding in bindings)
        {
            if (binding.GlyphDefinition is not { IsActive: true }) continue;

            if (!Enum.TryParse<GlyphEventType>(binding.GlyphDefinition.EventType, out GlyphEventType eventType))
            {
                Log.Warn("Trait Glyph binding {Id} has unknown event type '{EventType}'. Skipping.",
                    binding.Id, binding.GlyphDefinition.EventType);
                continue;
            }

            GlyphGraph? graph = DeserializeGraph(binding.GlyphDefinition);
            if (graph == null) continue;

            var key = (binding.TraitTag, eventType);
            if (!newCache.TryGetValue(key, out List<GlyphGraph>? list))
            {
                list = [];
                newCache[key] = list;
            }

            list.Add(graph);
        }

        _traitBindingCache = newCache;
        Log.Info("Glyph trait binding cache refreshed: {Count} tag-event combinations.", newCache.Count);
    }

    /// <summary>
    /// Handles trait selection (grant) by running OnTraitGranted graphs.
    /// </summary>
    public async Task HandleAsync(TraitSelectedEvent @event, CancellationToken cancellationToken = default)
    {
        if (!@event.IsConfirmed) return; // Only run on confirmed selections

        await RunTraitGraphs(
            @event.TraitTag,
            @event.CharacterId,
            GlyphEventType.OnTraitGranted,
            cancellationToken);
    }

    /// <summary>
    /// Handles trait deselection (removal) by running OnTraitRemoved graphs.
    /// </summary>
    public async Task HandleAsync(TraitDeselectedEvent @event, CancellationToken cancellationToken = default)
    {
        await RunTraitGraphs(
            @event.TraitTag,
            @event.CharacterId,
            GlyphEventType.OnTraitRemoved,
            cancellationToken);
    }

    private async Task RunTraitGraphs(
        TraitTag traitTag,
        CharacterId characterId,
        GlyphEventType eventType,
        CancellationToken ct)
    {
        var key = (traitTag.Value, eventType);
        if (!_traitBindingCache.TryGetValue(key, out List<GlyphGraph>? graphs)) return;

        // Collect the character's current traits for the context
        List<string> characterTraits = [];
        try
        {
            var traits = await _traitSubsystem.GetCharacterTraitsAsync(characterId, ct);
            characterTraits = traits.Select(t => t.TraitTag.Value).ToList();
        }
        catch (Exception ex)
        {
            Log.Warn(ex, "Failed to fetch character traits for {CharacterId}.", characterId);
        }

        foreach (GlyphGraph graph in graphs)
        {
            GlyphExecutionContext ctx = new()
            {
                Graph = graph,
                CharacterId = characterId.Value.ToString(),
                TraitTag = traitTag.Value,
                TargetCreature = 0, // Set by caller if creature is known
                CancellationToken = ct,
                MaxExecutionSteps = 10_000,
                EnableTracing = false
            };

            // Populate the variable store with character traits
            ctx.Variables["character_traits"] = characterTraits;

            try
            {
                await _bootstrap.Interpreter.ExecuteAsync(ctx);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error executing {EventType} Glyph graph '{Name}' for trait '{TraitTag}'.",
                    eventType, graph.Name, traitTag.Value);
            }
        }
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
}
