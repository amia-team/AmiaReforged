using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.Persistence;

/// <summary>
/// Database-backed implementation of <see cref="IWorkstationRepository"/>.
/// Persists workstation definitions to PostgreSQL via EF Core.
/// </summary>
[ServiceBinding(typeof(IWorkstationRepository))]
public class DbWorkstationRepository : IWorkstationRepository
{
    private readonly PwContextFactory _contextFactory;

    public DbWorkstationRepository(PwContextFactory contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public bool WorkstationExists(string tag)
    {
        using var ctx = _contextFactory.CreateDbContext();
        return ctx.WorkstationDefinitions.Any(e => e.Tag == tag);
    }

    public Workstation? GetByTag(WorkstationTag tag)
    {
        using var ctx = _contextFactory.CreateDbContext();

        var entity = ctx.WorkstationDefinitions
            .FirstOrDefault(e => e.Tag == tag.Value);

        return entity != null ? WorkstationMapper.ToDomain(entity) : null;
    }

    public List<Workstation> All()
    {
        using var ctx = _contextFactory.CreateDbContext();

        return ctx.WorkstationDefinitions
            .AsEnumerable()
            .Select(WorkstationMapper.ToDomain)
            .ToList();
    }

    public void Add(Workstation workstation)
    {
        using var ctx = _contextFactory.CreateDbContext();

        var existing = ctx.WorkstationDefinitions
            .FirstOrDefault(e => e.Tag == workstation.Tag.Value);

        if (existing != null)
        {
            WorkstationMapper.UpdateEntity(existing, workstation);
        }
        else
        {
            ctx.WorkstationDefinitions.Add(WorkstationMapper.ToEntity(workstation));
        }

        ctx.SaveChanges();
    }

    public void Update(Workstation workstation)
    {
        using var ctx = _contextFactory.CreateDbContext();

        var existing = ctx.WorkstationDefinitions
            .FirstOrDefault(e => e.Tag == workstation.Tag.Value);

        if (existing == null)
            throw new InvalidOperationException($"Workstation with tag '{workstation.Tag}' not found");

        WorkstationMapper.UpdateEntity(existing, workstation);
        ctx.SaveChanges();
    }

    public bool Delete(string tag)
    {
        using var ctx = _contextFactory.CreateDbContext();

        var existing = ctx.WorkstationDefinitions
            .FirstOrDefault(e => e.Tag == tag);

        if (existing == null) return false;

        ctx.WorkstationDefinitions.Remove(existing);
        ctx.SaveChanges();
        return true;
    }

    public List<Workstation> Search(string? searchTerm, int page, int pageSize, out int totalCount)
    {
        using var ctx = _contextFactory.CreateDbContext();

        IQueryable<PersistedWorkstationDefinition> query = ctx.WorkstationDefinitions;

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(e =>
                e.Tag.Contains(searchTerm) ||
                e.Name.Contains(searchTerm));
        }

        totalCount = query.Count();

        return query
            .OrderBy(e => e.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsEnumerable()
            .Select(WorkstationMapper.ToDomain)
            .ToList();
    }
}
