using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Persistence;
using AmiaReforged.PwEngine.Features.Glyph.Runtime;
using AmiaReforged.PwEngine.Features.Glyph.Nwn;
using AmiaReforged.PwEngine.Features.Glyph.Runtime.Nodes.Traits;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits.Effects;
using Anvil.Services;
using NLog;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.Glyph.Integration;

/// <summary>
/// Connects the Glyph interpreter to trait domain events.
/// Listens for <see cref="TraitSelectedEvent"/> (grant) and <see cref="TraitDeselectedEvent"/> (remove),
/// then executes any Glyph graphs bound to the affected trait tag.
/// Also dispatches independent effect-resolution stages for the trait effect applier.
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

    private readonly GlyphBootstrap _bootstrap;
    private readonly IGlyphRepository _repository;
    private readonly ITraitSubsystem _traitSubsystem;

    /// <summary>
    /// Cache of trait binding identities keyed by (TraitTag, EventType).
    /// </summary>
    private volatile Dictionary<(string TraitTag, GlyphEventType EventType), List<Guid>> _traitBindingCache = new();

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
        Dictionary<(string, GlyphEventType), List<Guid>> newCache = new();

        foreach (TraitGlyphBinding binding in bindings.OrderBy(b => b.Priority))
        {
            if (binding.GlyphDefinition is null) continue;

            if (!Enum.TryParse<GlyphEventType>(binding.GlyphDefinition.EventType, out GlyphEventType eventType))
            {
                Log.Warn("Trait Glyph binding {Id} has unknown event type '{EventType}'. Skipping.",
                    binding.Id, binding.GlyphDefinition.EventType);
                continue;
            }

            _bootstrap.RestorePublished(binding.GlyphDefinition);

            (string TraitTag, GlyphEventType eventType) key = (binding.TraitTag, eventType);
            if (!newCache.TryGetValue(key, out List<Guid>? list))
            {
                list = [];
                newCache[key] = list;
            }

            list.Add(binding.GlyphDefinitionId);
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

    /// <summary>
    /// Executes main before the requested rebuild stage and returns effects contributed by successful scripts.
    /// Death dispatches only its lifecycle stage. Older published graphs without main remain supported.
    /// Called on the game thread with a snapshot of active, confirmed traits, including glyph-only traits.
    /// </summary>
    public List<GlyphNwnEffect> RunEffectResolution(
        string traitTag,
        Guid characterId,
        uint creature,
        TraitEffectResolutionStage stage,
        IReadOnlyList<string> characterTraits,
        uint killer = NWScript.OBJECT_INVALID)
    {
        string stageTypeId = stage switch
        {
            TraitEffectResolutionStage.ClientEnter => TraitClientEnterStageExecutor.NodeTypeId,
            TraitEffectResolutionStage.LevelUp => TraitLevelUpStageExecutor.NodeTypeId,
            TraitEffectResolutionStage.Respawn => TraitRespawnStageExecutor.NodeTypeId,
            TraitEffectResolutionStage.Confirmed => TraitConfirmedStageExecutor.NodeTypeId,
            TraitEffectResolutionStage.Death => TraitDeathStageExecutor.NodeTypeId,
            _ => throw new ArgumentOutOfRangeException(nameof(stage))
        };
        List<GlyphNwnEffect> effects = [];
        if (!_traitBindingCache.TryGetValue((traitTag, GlyphEventType.TraitEffectResolution), out List<Guid>? graphs))
            return effects;

        foreach (Guid definitionId in graphs)
        {
            var program = _bootstrap.Programs.GetActive(definitionId);
            if (program == null) continue;
            GlyphGraph graph = program.CreateExecutionGraph();
            if (graph.EventType != GlyphEventType.TraitEffectResolution) continue;
            GlyphExecutionContext ctx = new()
            {
                Graph = graph, CharacterId = characterId.ToString(), TraitTag = traitTag,
                TargetCreature = creature, EnableTracing = true
            };
            TraitGlyphContext traitContext = ctx.Get<TraitGlyphContext>()!;
            traitContext.Killer = killer;
            ctx.Variables["character_traits"] = characterTraits.ToList();

            try
            {
                bool succeeded = true;
                if (stage != TraitEffectResolutionStage.Death && graph.FindStageNode(TraitMainStageExecutor.NodeTypeId) != null)
                    succeeded = _bootstrap.Interpreter.ExecuteStageAsync(ctx, TraitMainStageExecutor.NodeTypeId).GetAwaiter().GetResult();
                if (succeeded)
                    succeeded = _bootstrap.Interpreter.ExecuteStageAsync(ctx, stageTypeId).GetAwaiter().GetResult();
                if (succeeded && stage != TraitEffectResolutionStage.Death)
                    effects.AddRange(traitContext.Effects);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error executing {Stage} Glyph '{Name}' for trait '{TraitTag}'.", stage, graph.Name, traitTag);
            }
        }
        return effects;
    }

    private async Task RunTraitGraphs(
        TraitTag traitTag,
        CharacterId characterId,
        GlyphEventType eventType,
        CancellationToken ct)
    {
        (string Value, GlyphEventType eventType) key = (traitTag.Value, eventType);
        if (!_traitBindingCache.TryGetValue(key, out List<Guid>? graphs)) return;

        // Collect the character's current traits for the context
        List<string> characterTraits = [];
        try
        {
            List<CharacterTrait> traits = await _traitSubsystem.GetCharacterTraitsAsync(characterId, ct);
            characterTraits = traits.Select(t => t.TraitTag.Value).ToList();
        }
        catch (Exception ex)
        {
            Log.Warn(ex, "Failed to fetch character traits for {CharacterId}.", characterId);
        }

        foreach (Guid definitionId in graphs)
        {
            var program = _bootstrap.Programs.GetActive(definitionId);
            if (program == null) continue;
            GlyphGraph graph = program.CreateExecutionGraph();
            GlyphExecutionContext ctx = new()
            {
                Graph = graph,
                CharacterId = characterId.Value.ToString(),
                TraitTag = traitTag.Value,
                TargetCreature = 0, // Set by caller if creature is known
                CancellationToken = ct,
                MaxExecutionSteps = 10_000,
                EnableTracing = true
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
}
