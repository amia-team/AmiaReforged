using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.ItemData;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.Persistence;

/// <summary>
/// Database-backed implementation of <see cref="IItemDefinitionRepository"/>.
/// Persists item blueprints to PostgreSQL via EF Core.
/// </summary>
[ServiceBinding(typeof(IItemDefinitionRepository))]
public class DbItemDefinitionRepository : IItemDefinitionRepository
{
    private readonly PwContextFactory _contextFactory;

    public DbItemDefinitionRepository(PwContextFactory contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public void AddItemDefinition(ItemBlueprint definition)
    {
        using var ctx = _contextFactory.CreateDbContext();

        var existing = ctx.ItemBlueprints
            .FirstOrDefault(e => e.ResRef == definition.ResRef || e.ItemTag == definition.ItemTag);

        if (existing != null)
        {
            ItemBlueprintMapper.UpdateEntity(existing, definition);
        }
        else
        {
            ctx.ItemBlueprints.Add(ItemBlueprintMapper.ToEntity(definition));
        }

        ctx.SaveChanges();
    }

    public ItemBlueprint? GetByTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag)) return null;

        using var ctx = _contextFactory.CreateDbContext();

        var entity = ctx.ItemBlueprints
            .FirstOrDefault(e => EF.Functions.ILike(e.ItemTag, tag));

        if (entity == null)
        {
            // Fallback: try matching by source file (preserves existing behavior)
            entity = ctx.ItemBlueprints
                .FirstOrDefault(e => e.SourceFile != null && EF.Functions.ILike(e.SourceFile, tag));
        }

        return entity != null ? ItemBlueprintMapper.ToDomain(entity) : null;
    }

    public ItemBlueprint? GetByResRef(string resRef)
    {
        if (string.IsNullOrWhiteSpace(resRef)) return null;

        using var ctx = _contextFactory.CreateDbContext();

        var entity = ctx.ItemBlueprints
            .FirstOrDefault(e => EF.Functions.ILike(e.ResRef, resRef));

        return entity != null ? ItemBlueprintMapper.ToDomain(entity) : null;
    }

    public List<ItemBlueprint> AllItems()
    {
        using var ctx = _contextFactory.CreateDbContext();

        return ctx.ItemBlueprints
            .AsNoTracking()
            .Select(e => ItemBlueprintMapper.ToDomain(e))
            .ToList();
    }

    public List<string> FindSimilarTags(string tag, int maxResults = 3)
    {
        if (string.IsNullOrWhiteSpace(tag)) return new List<string>();

        using var ctx = _contextFactory.CreateDbContext();

        // Load all tags into memory for Levenshtein comparison
        // (pg_trgm extension would be more performant at scale, but this matches existing behavior)
        var allTags = ctx.ItemBlueprints
            .AsNoTracking()
            .Select(e => e.ItemTag)
            .ToList();

        if (allTags.Count == 0) return new List<string>();

        return allTags
            .Select(existingTag => new { Tag = existingTag, Score = CalculateSimilarity(tag, existingTag) })
            .Where(x => x.Score > 0.3)
            .OrderByDescending(x => x.Score)
            .Take(maxResults)
            .Select(x => x.Tag)
            .ToList();
    }

    /// <summary>
    /// Search items by name, tag, or resref. Supports pagination.
    /// </summary>
    public List<ItemBlueprint> Search(string? searchTerm, int page, int pageSize, out int totalCount)
    {
        using var ctx = _contextFactory.CreateDbContext();

        IQueryable<PersistedItemBlueprint> query = ctx.ItemBlueprints.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            string pattern = $"%{searchTerm}%";
            query = query.Where(e =>
                EF.Functions.ILike(e.ItemTag, pattern) ||
                EF.Functions.ILike(e.Name, pattern) ||
                EF.Functions.ILike(e.ResRef, pattern));
        }

        totalCount = query.Count();

        return query
            .OrderBy(e => e.Name)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .AsEnumerable()
            .Select(ItemBlueprintMapper.ToDomain)
            .ToList();
    }

    /// <summary>
    /// Delete an item blueprint by its tag.
    /// </summary>
    public bool DeleteByTag(string tag)
    {
        using var ctx = _contextFactory.CreateDbContext();

        var entity = ctx.ItemBlueprints
            .FirstOrDefault(e => EF.Functions.ILike(e.ItemTag, tag));

        if (entity == null) return false;

        ctx.ItemBlueprints.Remove(entity);
        ctx.SaveChanges();
        return true;
    }

    private static double CalculateSimilarity(string source, string target)
    {
        source = source.ToLowerInvariant();
        target = target.ToLowerInvariant();

        if (target.Contains(source) || source.Contains(target))
        {
            return 0.8;
        }

        int distance = LevenshteinDistance(source, target);
        int maxLen = Math.Max(source.Length, target.Length);
        if (maxLen == 0) return 1.0;
        return 1.0 - (double)distance / maxLen;
    }

    private static int LevenshteinDistance(string source, string target)
    {
        if (string.IsNullOrEmpty(source)) return target?.Length ?? 0;
        if (string.IsNullOrEmpty(target)) return source.Length;

        int[,] dp = new int[source.Length + 1, target.Length + 1];

        for (int i = 0; i <= source.Length; i++) dp[i, 0] = i;
        for (int j = 0; j <= target.Length; j++) dp[0, j] = j;

        for (int i = 1; i <= source.Length; i++)
        {
            for (int j = 1; j <= target.Length; j++)
            {
                int cost = source[i - 1] == target[j - 1] ? 0 : 1;
                dp[i, j] = Math.Min(
                    Math.Min(dp[i - 1, j] + 1, dp[i, j - 1] + 1),
                    dp[i - 1, j - 1] + cost);
            }
        }

        return dp[source.Length, target.Length];
    }
}
