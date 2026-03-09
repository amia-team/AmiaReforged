using AmiaReforged.PwEngine.Database;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions.Persistence;

/// <summary>
/// Database-backed repository for <see cref="InteractionDefinition"/>.
/// Follows the same pattern as <c>DbIndustryRepository</c> and <c>DbResourceNodeDefinitionRepository</c>.
/// </summary>
[ServiceBinding(typeof(IInteractionDefinitionRepository))]
public sealed class DbInteractionDefinitionRepository : IInteractionDefinitionRepository
{
    private readonly PwContextFactory _contextFactory;

    public DbInteractionDefinitionRepository(PwContextFactory contextFactory)
    {
        _contextFactory = contextFactory;
    }

    /// <inheritdoc />
    public InteractionDefinition? Get(string tag)
    {
        using PwEngineContext db = _contextFactory.CreateDbContext();
        PersistedInteractionDefinition? entity = db.InteractionDefinitions.Find(tag);
        return entity is null ? null : InteractionDefinitionMapper.ToDomain(entity);
    }

    /// <inheritdoc />
    public List<InteractionDefinition> All()
    {
        using PwEngineContext db = _contextFactory.CreateDbContext();
        return db.InteractionDefinitions
            .OrderBy(e => e.Name)
            .AsNoTracking()
            .ToList()
            .Select(InteractionDefinitionMapper.ToDomain)
            .ToList();
    }

    /// <inheritdoc />
    public bool Exists(string tag)
    {
        using PwEngineContext db = _contextFactory.CreateDbContext();
        return db.InteractionDefinitions.Any(e => e.Tag == tag);
    }

    /// <inheritdoc />
    public void Create(InteractionDefinition definition)
    {
        using PwEngineContext db = _contextFactory.CreateDbContext();

        PersistedInteractionDefinition? existing = db.InteractionDefinitions.Find(definition.Tag);
        if (existing is not null)
        {
            InteractionDefinitionMapper.UpdateEntity(existing, definition);
        }
        else
        {
            db.InteractionDefinitions.Add(InteractionDefinitionMapper.ToEntity(definition));
        }

        db.SaveChanges();
    }

    /// <inheritdoc />
    public void Update(InteractionDefinition definition)
    {
        using PwEngineContext db = _contextFactory.CreateDbContext();
        PersistedInteractionDefinition? entity = db.InteractionDefinitions.Find(definition.Tag);
        if (entity is null) return;

        InteractionDefinitionMapper.UpdateEntity(entity, definition);
        db.SaveChanges();
    }

    /// <inheritdoc />
    public bool Delete(string tag)
    {
        using PwEngineContext db = _contextFactory.CreateDbContext();
        PersistedInteractionDefinition? entity = db.InteractionDefinitions.Find(tag);
        if (entity is null) return false;

        db.InteractionDefinitions.Remove(entity);
        db.SaveChanges();
        return true;
    }

    /// <inheritdoc />
    public List<InteractionDefinition> Search(string? search, int page, int pageSize, out int totalCount)
    {
        using PwEngineContext db = _contextFactory.CreateDbContext();

        IQueryable<PersistedInteractionDefinition> query = db.InteractionDefinitions.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(e =>
                EF.Functions.ILike(e.Tag, $"%{search}%") ||
                EF.Functions.ILike(e.Name, $"%{search}%"));
        }

        totalCount = query.Count();

        List<PersistedInteractionDefinition> entities = query
            .OrderBy(e => e.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        return entities.Select(InteractionDefinitionMapper.ToDomain).ToList();
    }
}
