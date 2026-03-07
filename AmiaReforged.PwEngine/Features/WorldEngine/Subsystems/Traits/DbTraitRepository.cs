using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits;

/// <summary>
/// PostgreSQL-backed trait repository. Reads are served from an in-memory
/// cache (populated at startup by <see cref="TraitDefinitionLoadingService"/>),
/// while mutations are persisted to the <c>trait_definitions</c> table.
/// </summary>
[ServiceBinding(typeof(ITraitRepository))]
public class DbTraitRepository : ITraitRepository
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly InMemoryTraitRepository _cache = new();
    private readonly PwContextFactory _contextFactory;
    private readonly TraitDefinitionMapper _mapper;

    public DbTraitRepository(PwContextFactory contextFactory, TraitDefinitionMapper mapper)
    {
        _contextFactory = contextFactory;
        _mapper = mapper;
    }

    /// <summary>
    /// Provides direct access to the in-memory cache for bulk-loading at startup.
    /// Used by <see cref="TraitDefinitionLoadingService"/>.
    /// </summary>
    internal InMemoryTraitRepository Cache => _cache;

    public bool TraitExists(string traitTag) => _cache.TraitExists(traitTag);

    public Trait? Get(string traitTag) => _cache.Get(traitTag);

    public List<Trait> All() => _cache.All();

    public void Add(Trait trait)
    {
        _cache.Add(trait);
        PersistAdd(trait);
    }

    public bool Remove(string traitTag)
    {
        bool removed = _cache.Remove(traitTag);
        if (removed)
        {
            PersistRemove(traitTag);
        }
        return removed;
    }

    /// <summary>
    /// Adds a trait to the in-memory cache only (no DB write).
    /// Used by <see cref="TraitDefinitionLoadingService"/> during bulk-load.
    /// </summary>
    internal void AddToCache(Trait trait)
    {
        _cache.Add(trait);
    }

    private void PersistAdd(Trait trait)
    {
        try
        {
            using PwEngineContext ctx = _contextFactory.CreateDbContext();
            PersistedTraitDefinition entity = _mapper.ToPersistent(trait);
            entity.CreatedUtc = DateTime.UtcNow;
            entity.UpdatedUtc = DateTime.UtcNow;

            PersistedTraitDefinition? existing = ctx.TraitDefinitions.Find(trait.Tag);
            if (existing != null)
            {
                existing.Name = entity.Name;
                existing.Description = entity.Description;
                existing.PointCost = entity.PointCost;
                existing.Category = entity.Category;
                existing.DeathBehavior = entity.DeathBehavior;
                existing.RequiresUnlock = entity.RequiresUnlock;
                existing.DmOnly = entity.DmOnly;
                existing.EffectsJson = entity.EffectsJson;
                existing.AllowedRacesJson = entity.AllowedRacesJson;
                existing.AllowedClassesJson = entity.AllowedClassesJson;
                existing.ForbiddenRacesJson = entity.ForbiddenRacesJson;
                existing.ForbiddenClassesJson = entity.ForbiddenClassesJson;
                existing.ConflictingTraitsJson = entity.ConflictingTraitsJson;
                existing.PrerequisiteTraitsJson = entity.PrerequisiteTraitsJson;
                existing.UpdatedUtc = DateTime.UtcNow;
                ctx.TraitDefinitions.Update(existing);
            }
            else
            {
                ctx.TraitDefinitions.Add(entity);
            }

            ctx.SaveChanges();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to persist trait definition '{Tag}'", trait.Tag);
        }
    }

    private void PersistRemove(string traitTag)
    {
        try
        {
            using PwEngineContext ctx = _contextFactory.CreateDbContext();
            PersistedTraitDefinition? existing = ctx.TraitDefinitions.Find(traitTag);
            if (existing != null)
            {
                ctx.TraitDefinitions.Remove(existing);
                ctx.SaveChanges();
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to remove trait definition '{Tag}' from database", traitTag);
        }
    }
}
