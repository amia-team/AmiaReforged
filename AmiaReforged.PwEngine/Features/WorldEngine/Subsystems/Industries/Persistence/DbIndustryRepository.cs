using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.Persistence;

/// <summary>
/// Database-backed implementation of <see cref="IIndustryRepository"/>.
/// Persists industry definitions to PostgreSQL via EF Core.
/// </summary>
[ServiceBinding(typeof(IIndustryRepository))]
public class DbIndustryRepository : IIndustryRepository
{
    private readonly PwContextFactory _contextFactory;

    public DbIndustryRepository(PwContextFactory contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public bool IndustryExists(string industryTag)
    {
        using PwEngineContext ctx = _contextFactory.CreateDbContext();
        return ctx.IndustryDefinitions.Any(e => e.Tag == industryTag);
    }

    public void Add(Industry industry)
    {
        using PwEngineContext ctx = _contextFactory.CreateDbContext();

        PersistedIndustryDefinition? existing = ctx.IndustryDefinitions
            .FirstOrDefault(e => e.Tag == industry.Tag);

        if (existing != null)
        {
            IndustryMapper.UpdateEntity(existing, industry);
        }
        else
        {
            ctx.IndustryDefinitions.Add(IndustryMapper.ToEntity(industry));
        }

        ctx.SaveChanges();
    }

    public Industry? Get(string membershipIndustryTag)
    {
        using PwEngineContext ctx = _contextFactory.CreateDbContext();

        PersistedIndustryDefinition? entity = ctx.IndustryDefinitions
            .FirstOrDefault(e => e.Tag == membershipIndustryTag);

        return entity != null ? IndustryMapper.ToDomain(entity) : null;
    }

    public Industry? GetByTag(IndustryTag industryTag)
    {
        return Get(industryTag.Value);
    }

    public List<Industry> All()
    {
        using PwEngineContext ctx = _contextFactory.CreateDbContext();

        return ctx.IndustryDefinitions
            .AsEnumerable()
            .Select(IndustryMapper.ToDomain)
            .ToList();
    }

    public void Update(Industry industry)
    {
        using PwEngineContext ctx = _contextFactory.CreateDbContext();

        PersistedIndustryDefinition? existing = ctx.IndustryDefinitions
            .FirstOrDefault(e => e.Tag == industry.Tag);

        if (existing == null)
            throw new InvalidOperationException($"Industry with tag '{industry.Tag}' not found");

        IndustryMapper.UpdateEntity(existing, industry);
        ctx.SaveChanges();
    }

    public bool Delete(string tag)
    {
        using PwEngineContext ctx = _contextFactory.CreateDbContext();

        PersistedIndustryDefinition? existing = ctx.IndustryDefinitions
            .FirstOrDefault(e => e.Tag == tag);

        if (existing == null) return false;

        ctx.IndustryDefinitions.Remove(existing);
        ctx.SaveChanges();
        return true;
    }

    public List<Industry> Search(string? searchTerm, int page, int pageSize, out int totalCount)
    {
        using PwEngineContext ctx = _contextFactory.CreateDbContext();

        IQueryable<PersistedIndustryDefinition> query = ctx.IndustryDefinitions;

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
            .Select(IndustryMapper.ToDomain)
            .ToList();
    }
}
