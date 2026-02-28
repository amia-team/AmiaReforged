using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes.ResourceNodeData;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes.Persistence;

/// <summary>
/// Database-backed implementation of <see cref="IResourceNodeDefinitionRepository"/>.
/// Persists resource node definitions to PostgreSQL via EF Core.
/// </summary>
[ServiceBinding(typeof(IResourceNodeDefinitionRepository))]
public class DbResourceNodeDefinitionRepository : IResourceNodeDefinitionRepository
{
    private readonly PwContextFactory _contextFactory;

    public DbResourceNodeDefinitionRepository(PwContextFactory contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public void Create(ResourceNodeDefinition definition)
    {
        using var ctx = _contextFactory.CreateDbContext();

        var existing = ctx.ResourceNodeDefinitions
            .FirstOrDefault(e => e.Tag == definition.Tag);

        if (existing != null)
        {
            ResourceNodeMapper.UpdateEntity(existing, definition);
        }
        else
        {
            ctx.ResourceNodeDefinitions.Add(ResourceNodeMapper.ToEntity(definition));
        }

        ctx.SaveChanges();
    }

    public ResourceNodeDefinition? Get(string tag)
    {
        using var ctx = _contextFactory.CreateDbContext();

        var entity = ctx.ResourceNodeDefinitions
            .AsNoTracking()
            .FirstOrDefault(e => e.Tag == tag);

        return entity != null ? ResourceNodeMapper.ToDomain(entity) : null;
    }

    public void Update(ResourceNodeDefinition definition)
    {
        using var ctx = _contextFactory.CreateDbContext();

        var existing = ctx.ResourceNodeDefinitions
            .FirstOrDefault(e => e.Tag == definition.Tag);

        if (existing == null)
        {
            ctx.ResourceNodeDefinitions.Add(ResourceNodeMapper.ToEntity(definition));
        }
        else
        {
            ResourceNodeMapper.UpdateEntity(existing, definition);
        }

        ctx.SaveChanges();
    }

    public bool Delete(string tag)
    {
        using var ctx = _contextFactory.CreateDbContext();

        var entity = ctx.ResourceNodeDefinitions
            .FirstOrDefault(e => e.Tag == tag);

        if (entity == null) return false;

        ctx.ResourceNodeDefinitions.Remove(entity);
        ctx.SaveChanges();
        return true;
    }

    public bool Exists(string tag)
    {
        using var ctx = _contextFactory.CreateDbContext();
        return ctx.ResourceNodeDefinitions.Any(e => e.Tag == tag);
    }

    public List<ResourceNodeDefinition> All()
    {
        using var ctx = _contextFactory.CreateDbContext();

        return ctx.ResourceNodeDefinitions
            .AsNoTracking()
            .AsEnumerable()
            .Select(ResourceNodeMapper.ToDomain)
            .ToList();
    }

    /// <summary>
    /// Search resource node definitions by name or tag. Supports filtering by type and pagination.
    /// </summary>
    public List<ResourceNodeDefinition> Search(string? searchTerm, string? type, int page, int pageSize,
        out int totalCount)
    {
        using var ctx = _contextFactory.CreateDbContext();

        IQueryable<PersistedResourceNodeDefinition> query = ctx.ResourceNodeDefinitions.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            string pattern = $"%{searchTerm}%";
            query = query.Where(e =>
                EF.Functions.ILike(e.Tag, pattern) ||
                EF.Functions.ILike(e.Name, pattern));
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            query = query.Where(e => EF.Functions.ILike(e.Type, type));
        }

        totalCount = query.Count();

        return query
            .OrderBy(e => e.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsEnumerable()
            .Select(ResourceNodeMapper.ToDomain)
            .ToList();
    }
}
