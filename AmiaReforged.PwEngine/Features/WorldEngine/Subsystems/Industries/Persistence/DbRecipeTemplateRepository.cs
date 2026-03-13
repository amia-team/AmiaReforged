using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.Persistence;

/// <summary>
/// Database-backed implementation of <see cref="IRecipeTemplateRepository"/>.
/// Persists recipe template definitions to PostgreSQL via EF Core.
/// </summary>
[ServiceBinding(typeof(IRecipeTemplateRepository))]
public class DbRecipeTemplateRepository : IRecipeTemplateRepository
{
    private readonly PwContextFactory _contextFactory;

    public DbRecipeTemplateRepository(PwContextFactory contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public void Add(RecipeTemplate template)
    {
        using PwEngineContext ctx = _contextFactory.CreateDbContext();

        PersistedRecipeTemplate? existing = ctx.RecipeTemplateDefinitions
            .FirstOrDefault(e => e.Tag == template.Tag);

        if (existing != null)
        {
            RecipeTemplateMapper.UpdateEntity(existing, template);
        }
        else
        {
            ctx.RecipeTemplateDefinitions.Add(RecipeTemplateMapper.ToEntity(template));
        }

        ctx.SaveChanges();
    }

    public RecipeTemplate? GetByTag(string tag)
    {
        using PwEngineContext ctx = _contextFactory.CreateDbContext();

        PersistedRecipeTemplate? entity = ctx.RecipeTemplateDefinitions
            .FirstOrDefault(e => e.Tag == tag);

        return entity != null ? RecipeTemplateMapper.ToDomain(entity) : null;
    }

    public List<RecipeTemplate> GetByIndustry(IndustryTag industryTag)
    {
        using PwEngineContext ctx = _contextFactory.CreateDbContext();

        return ctx.RecipeTemplateDefinitions
            .Where(e => e.IndustryTag == industryTag.Value)
            .AsEnumerable()
            .Select(RecipeTemplateMapper.ToDomain)
            .ToList();
    }

    public List<RecipeTemplate> All()
    {
        using PwEngineContext ctx = _contextFactory.CreateDbContext();

        return ctx.RecipeTemplateDefinitions
            .AsEnumerable()
            .Select(RecipeTemplateMapper.ToDomain)
            .ToList();
    }

    public void Update(RecipeTemplate template)
    {
        using PwEngineContext ctx = _contextFactory.CreateDbContext();

        PersistedRecipeTemplate? existing = ctx.RecipeTemplateDefinitions
            .FirstOrDefault(e => e.Tag == template.Tag);

        if (existing == null)
            throw new InvalidOperationException($"Recipe template with tag '{template.Tag}' not found");

        RecipeTemplateMapper.UpdateEntity(existing, template);
        ctx.SaveChanges();
    }

    public bool Delete(string tag)
    {
        using PwEngineContext ctx = _contextFactory.CreateDbContext();

        PersistedRecipeTemplate? existing = ctx.RecipeTemplateDefinitions
            .FirstOrDefault(e => e.Tag == tag);

        if (existing == null) return false;

        ctx.RecipeTemplateDefinitions.Remove(existing);
        ctx.SaveChanges();
        return true;
    }

    public List<RecipeTemplate> Search(string? searchTerm, int page, int pageSize, out int totalCount)
    {
        using PwEngineContext ctx = _contextFactory.CreateDbContext();

        IQueryable<PersistedRecipeTemplate> query = ctx.RecipeTemplateDefinitions;

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            query = query.Where(e =>
                e.Tag.Contains(searchTerm) ||
                e.Name.Contains(searchTerm) ||
                e.IndustryTag.Contains(searchTerm));
        }

        totalCount = query.Count();

        return query
            .OrderBy(e => e.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsEnumerable()
            .Select(RecipeTemplateMapper.ToDomain)
            .ToList();
    }
}
