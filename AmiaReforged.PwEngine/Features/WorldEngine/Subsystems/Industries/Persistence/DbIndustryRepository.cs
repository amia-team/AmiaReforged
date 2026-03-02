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
        using var ctx = _contextFactory.CreateDbContext();
        return ctx.IndustryDefinitions.Any(e => e.Tag == industryTag);
    }

    public void Add(Industry industry)
    {
        using var ctx = _contextFactory.CreateDbContext();

        var existing = ctx.IndustryDefinitions
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
        using var ctx = _contextFactory.CreateDbContext();

        var entity = ctx.IndustryDefinitions
            .FirstOrDefault(e => e.Tag == membershipIndustryTag);

        return entity != null ? IndustryMapper.ToDomain(entity) : null;
    }

    public Industry? GetByTag(IndustryTag industryTag)
    {
        return Get(industryTag.Value);
    }

    public List<Industry> All()
    {
        using var ctx = _contextFactory.CreateDbContext();

        return ctx.IndustryDefinitions
            .AsEnumerable()
            .Select(IndustryMapper.ToDomain)
            .ToList();
    }
}
