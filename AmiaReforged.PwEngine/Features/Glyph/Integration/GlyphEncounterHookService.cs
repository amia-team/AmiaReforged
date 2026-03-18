using AmiaReforged.PwEngine.Features.Encounters.Models;
using AmiaReforged.PwEngine.Features.Glyph.Core;
using AmiaReforged.PwEngine.Features.Glyph.Persistence;
using AmiaReforged.PwEngine.Features.Glyph.Runtime;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.Glyph.Integration;

/// <summary>
/// Integration service that connects the Glyph interpreter to the encounter lifecycle.
/// Loaded at startup, caches all profile–graph bindings, and exposes hook methods called
/// by <see cref="Encounters.Services.DynamicCreatureSpawner"/> and
/// <see cref="Encounters.DynamicEncounterService"/>.
/// </summary>
[ServiceBinding(typeof(GlyphEncounterHookService))]
public class GlyphEncounterHookService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly GlyphBootstrap _bootstrap;
    private readonly IGlyphRepository _repository;

    /// <summary>
    /// Cache of active bindings keyed by (ProfileId, EventType).
    /// Each entry is a list of graphs sorted by priority (ascending).
    /// </summary>
    private Dictionary<(Guid ProfileId, GlyphEventType EventType), List<GlyphGraph>> _bindingCache = new();

    public GlyphEncounterHookService(GlyphBootstrap bootstrap, IGlyphRepository repository)
    {
        _bootstrap = bootstrap;
        _repository = repository;

        RefreshCacheAsync().GetAwaiter().GetResult();
        Log.Info("GlyphEncounterHookService initialized with {Count} cached bindings.", _bindingCache.Count);
    }

    /// <summary>
    /// Refreshes the binding cache from the database. Call after API mutations.
    /// </summary>
    public async Task RefreshCacheAsync()
    {
        List<SpawnProfileGlyphBinding> bindings = await _repository.GetAllBindingsAsync();
        Dictionary<(Guid, GlyphEventType), List<GlyphGraph>> newCache = new();

        foreach (SpawnProfileGlyphBinding binding in bindings)
        {
            if (binding.GlyphDefinition is not { IsActive: true }) continue;

            if (!Enum.TryParse<GlyphEventType>(binding.GlyphDefinition.EventType, out GlyphEventType eventType))
            {
                Log.Warn("Glyph binding {Id} has unknown event type '{EventType}'. Skipping.",
                    binding.Id, binding.GlyphDefinition.EventType);
                continue;
            }

            GlyphGraph? graph = DeserializeGraph(binding.GlyphDefinition);
            if (graph == null) continue;

            (Guid SpawnProfileId, GlyphEventType eventType) key = (binding.SpawnProfileId, eventType);
            if (!newCache.TryGetValue(key, out List<GlyphGraph>? list))
            {
                list = [];
                newCache[key] = list;
            }

            list.Add(graph);
        }

        _bindingCache = newCache;
        Log.Info("Glyph binding cache refreshed: {Count} profile-event combinations.", newCache.Count);
    }

    /// <summary>
    /// Runs all <see cref="GlyphEventType.BeforeGroupSpawn"/> graphs bound to the profile.
    /// Returns false if any graph cancels the spawn. Modifies <paramref name="spawnCount"/>
    /// if any graph sets it.
    /// </summary>
    public bool RunBeforeGroupSpawn(
        SpawnProfile profile,
        SpawnGroup group,
        EncounterContext encounterContext,
        ref int spawnCount)
    {
        (Guid Id, GlyphEventType BeforeGroupSpawn) key = (profile.Id, GlyphEventType.BeforeGroupSpawn);
        if (!_bindingCache.TryGetValue(key, out List<GlyphGraph>? graphs)) return true;

        foreach (GlyphGraph graph in graphs)
        {
            GlyphExecutionContext ctx = CreateContext(graph, profile, encounterContext, group);
            ctx.SpawnCount = spawnCount;

            try
            {
                _bootstrap.Interpreter.ExecuteAsync(ctx).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error executing BeforeGroupSpawn Glyph graph '{Name}'.", graph.Name);
                continue;
            }

            spawnCount = ctx.SpawnCount;

            if (ctx.ShouldCancelSpawn)
            {
                Log.Info("Glyph graph '{Name}' cancelled spawn for group '{Group}' in profile '{Profile}'.",
                    graph.Name, group.Name, profile.Name);
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Runs all <see cref="GlyphEventType.AfterGroupSpawn"/> graphs bound to the profile.
    /// Fire-and-forget — errors are logged but do not affect the encounter.
    /// </summary>
    public void RunAfterGroupSpawn(
        SpawnProfile profile,
        SpawnGroup group,
        EncounterContext encounterContext,
        List<uint> spawnedCreatures)
    {
        (Guid Id, GlyphEventType AfterGroupSpawn) key = (profile.Id, GlyphEventType.AfterGroupSpawn);
        if (!_bindingCache.TryGetValue(key, out List<GlyphGraph>? graphs)) return;

        foreach (GlyphGraph graph in graphs)
        {
            GlyphExecutionContext ctx = CreateContext(graph, profile, encounterContext, group);
            ctx.SpawnedCreatures = spawnedCreatures.ToList();

            try
            {
                _bootstrap.Interpreter.ExecuteAsync(ctx).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error executing AfterGroupSpawn Glyph graph '{Name}'.", graph.Name);
            }
        }
    }

    /// <summary>
    /// Runs all <see cref="GlyphEventType.OnCreatureDeath"/> graphs bound to the profile.
    /// </summary>
    public void RunOnCreatureDeath(
        uint deadCreature,
        uint killer,
        SpawnProfile profile,
        EncounterContext encounterContext)
    {
        (Guid Id, GlyphEventType OnCreatureDeath) key = (profile.Id, GlyphEventType.OnCreatureDeath);
        if (!_bindingCache.TryGetValue(key, out List<GlyphGraph>? graphs)) return;

        foreach (GlyphGraph graph in graphs)
        {
            GlyphExecutionContext ctx = CreateContext(graph, profile, encounterContext);
            ctx.DeadCreature = deadCreature;
            ctx.Killer = killer;

            try
            {
                _bootstrap.Interpreter.ExecuteAsync(ctx).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error executing OnCreatureDeath Glyph graph '{Name}'.", graph.Name);
            }
        }
    }

    /// <summary>
    /// Runs all <see cref="GlyphEventType.OnCreatureSpawn"/> graphs bound to the profile.
    /// Fires once per creature immediately after spawn, before bonuses and mutations.
    /// Returns control flags indicating whether the caller should skip bonuses/mutations.
    /// </summary>
    public void RunOnCreatureSpawn(
        uint creature,
        string creatureResRef,
        int spawnIndex,
        int totalCount,
        SpawnProfile profile,
        SpawnGroup group,
        EncounterContext encounterContext,
        out bool skipBonuses,
        out bool skipMutations)
    {
        skipBonuses = false;
        skipMutations = false;

        (Guid Id, GlyphEventType OnCreatureSpawn) key = (profile.Id, GlyphEventType.OnCreatureSpawn);
        if (!_bindingCache.TryGetValue(key, out List<GlyphGraph>? graphs)) return;

        foreach (GlyphGraph graph in graphs)
        {
            GlyphExecutionContext ctx = CreateContext(graph, profile, encounterContext, group);
            ctx.SpawnedCreature = creature;
            ctx.CreatureResRef = creatureResRef;
            ctx.SpawnIndex = spawnIndex;
            ctx.TotalGroupSpawnCount = totalCount;

            try
            {
                _bootstrap.Interpreter.ExecuteAsync(ctx).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error executing OnCreatureSpawn Glyph graph '{Name}'.", graph.Name);
                continue;
            }

            if (ctx.ShouldSkipBonuses) skipBonuses = true;
            if (ctx.ShouldSkipMutations) skipMutations = true;
        }
    }

    /// <summary>
    /// Runs all <see cref="GlyphEventType.OnBossSpawn"/> graphs bound to the profile.
    /// Fires when a boss or mini-boss is spawned, before its bonuses are applied.
    /// Returns a control flag indicating whether the caller should skip bonuses.
    /// </summary>
    public void RunOnBossSpawn(
        uint boss,
        string creatureResRef,
        SpawnProfile profile,
        EncounterContext encounterContext,
        out bool skipBonuses)
    {
        skipBonuses = false;

        (Guid Id, GlyphEventType OnBossSpawn) key = (profile.Id, GlyphEventType.OnBossSpawn);
        if (!_bindingCache.TryGetValue(key, out List<GlyphGraph>? graphs)) return;

        foreach (GlyphGraph graph in graphs)
        {
            GlyphExecutionContext ctx = CreateContext(graph, profile, encounterContext);
            ctx.SpawnedCreature = boss;
            ctx.CreatureResRef = creatureResRef;
            ctx.IsBoss = true;

            try
            {
                _bootstrap.Interpreter.ExecuteAsync(ctx).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Error executing OnBossSpawn Glyph graph '{Name}'.", graph.Name);
                continue;
            }

            if (ctx.ShouldSkipBonuses) skipBonuses = true;
        }
    }

    /// <summary>
    /// Checks whether any graphs are bound to the given profile for any event type.
    /// </summary>
    public bool HasBindingsForProfile(Guid profileId)
    {
        return _bindingCache.Keys.Any(k => k.ProfileId == profileId);
    }

    private static GlyphExecutionContext CreateContext(
        GlyphGraph graph,
        SpawnProfile profile,
        EncounterContext encounterContext,
        SpawnGroup? group = null)
    {
        return new GlyphExecutionContext
        {
            Graph = graph,
            EncounterContext = encounterContext,
            Profile = profile,
            Group = group,
            TriggeringPlayer = encounterContext.TriggeringPlayer,
            CancellationToken = CancellationToken.None,
            MaxExecutionSteps = 10_000,
            EnableTracing = false
        };
    }

    private GlyphGraph? DeserializeGraph(GlyphDefinition definition)
    {
        return GlyphGraphSerializer.Deserialize(definition);
    }
}
