using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Enums;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Application.PlayerKnowledge;

/// <summary>
/// Player-knowledge reads backed by the lore definition / unlock tables.
/// CQRS counterparts of the former inline-EF <c>CodexSubsystem</c> reads (F-6 audit).
/// </summary>
public record GetKnowledgeEntryQuery : IQuery<KnowledgeEntry?>
{
    public required string EntryId { get; init; }
}

public record SearchKnowledgeQuery : IQuery<List<KnowledgeEntry>>
{
    public required string SearchTerm { get; init; }
}

public record GetKnowledgeByCategoryQuery : IQuery<List<KnowledgeEntry>>
{
    public required KnowledgeCategory Category { get; init; }
}

public record HasKnowledgeQuery : IQuery<bool>
{
    public required CharacterId CharacterId { get; init; }
    public required string EntryId { get; init; }
}

public record GetCharacterKnowledgeQuery : IQuery<List<KnowledgeEntry>>
{
    public required CharacterId CharacterId { get; init; }
}

[ServiceBinding(typeof(IQueryHandler<GetKnowledgeEntryQuery, KnowledgeEntry?>))]
public sealed class GetKnowledgeEntryHandler : IQueryHandler<GetKnowledgeEntryQuery, KnowledgeEntry?>
{
    private readonly PwContextFactory _contextFactory;

    public GetKnowledgeEntryHandler(PwContextFactory contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<KnowledgeEntry?> HandleAsync(GetKnowledgeEntryQuery query, CancellationToken cancellationToken = default)
    {
        using PwEngineContext ctx = _contextFactory.CreateDbContext();
        PersistedLoreDefinition? def = await ctx.CodexLoreDefinitions
            .AsNoTracking()
            .FirstOrDefaultAsync(d => d.LoreId == query.EntryId, cancellationToken);
        return def is null ? null : KnowledgeMapping.ToKnowledgeEntry(def);
    }
}

[ServiceBinding(typeof(IQueryHandler<SearchKnowledgeQuery, List<KnowledgeEntry>>))]
public sealed class SearchKnowledgeHandler : IQueryHandler<SearchKnowledgeQuery, List<KnowledgeEntry>>
{
    private readonly PwContextFactory _contextFactory;

    public SearchKnowledgeHandler(PwContextFactory contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<KnowledgeEntry>> HandleAsync(SearchKnowledgeQuery query, CancellationToken cancellationToken = default)
    {
        using PwEngineContext ctx = _contextFactory.CreateDbContext();
        string lower = query.SearchTerm.ToLowerInvariant();
        List<PersistedLoreDefinition> defs = await ctx.CodexLoreDefinitions
            .AsNoTracking()
            .Where(d => EF.Functions.ILike(d.Title, $"%{lower}%")
                     || EF.Functions.ILike(d.Content, $"%{lower}%")
                     || (d.Keywords != null && EF.Functions.ILike(d.Keywords, $"%{lower}%")))
            .ToListAsync(cancellationToken);
        return defs.Select(KnowledgeMapping.ToKnowledgeEntry).ToList();
    }
}

[ServiceBinding(typeof(IQueryHandler<GetKnowledgeByCategoryQuery, List<KnowledgeEntry>>))]
public sealed class GetKnowledgeByCategoryHandler : IQueryHandler<GetKnowledgeByCategoryQuery, List<KnowledgeEntry>>
{
    private readonly PwContextFactory _contextFactory;

    public GetKnowledgeByCategoryHandler(PwContextFactory contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<KnowledgeEntry>> HandleAsync(GetKnowledgeByCategoryQuery query, CancellationToken cancellationToken = default)
    {
        using PwEngineContext ctx = _contextFactory.CreateDbContext();
        LoreCategory loreCategory = KnowledgeMapping.MapToLoreCategory(query.Category);
        List<PersistedLoreDefinition> defs = await ctx.CodexLoreDefinitions
            .AsNoTracking()
            .Where(d => d.Category == loreCategory)
            .ToListAsync(cancellationToken);
        return defs.Select(KnowledgeMapping.ToKnowledgeEntry).ToList();
    }
}

[ServiceBinding(typeof(IQueryHandler<HasKnowledgeQuery, bool>))]
public sealed class HasKnowledgeHandler : IQueryHandler<HasKnowledgeQuery, bool>
{
    private readonly PwContextFactory _contextFactory;

    public HasKnowledgeHandler(PwContextFactory contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<bool> HandleAsync(HasKnowledgeQuery query, CancellationToken cancellationToken = default)
    {
        using PwEngineContext ctx = _contextFactory.CreateDbContext();

        bool unlocked = await ctx.CodexLoreUnlocks
            .AnyAsync(u => u.CharacterId == query.CharacterId.Value && u.LoreId == query.EntryId, cancellationToken);
        if (unlocked) return true;

        return await ctx.CodexLoreDefinitions
            .AnyAsync(d => d.LoreId == query.EntryId && d.IsAlwaysAvailable, cancellationToken);
    }
}

[ServiceBinding(typeof(IQueryHandler<GetCharacterKnowledgeQuery, List<KnowledgeEntry>>))]
public sealed class GetCharacterKnowledgeHandler : IQueryHandler<GetCharacterKnowledgeQuery, List<KnowledgeEntry>>
{
    private readonly PwContextFactory _contextFactory;

    public GetCharacterKnowledgeHandler(PwContextFactory contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<KnowledgeEntry>> HandleAsync(GetCharacterKnowledgeQuery query, CancellationToken cancellationToken = default)
    {
        using PwEngineContext ctx = _contextFactory.CreateDbContext();

        List<PersistedLoreDefinition> unlocked = await ctx.CodexLoreUnlocks
            .Include(u => u.LoreDefinition)
            .Where(u => u.CharacterId == query.CharacterId.Value && u.LoreDefinition != null)
            .Select(u => u.LoreDefinition!)
            .ToListAsync(cancellationToken);

        HashSet<string> unlockedIds = unlocked.Select(d => d.LoreId).ToHashSet();
        List<PersistedLoreDefinition> alwaysAvailable = await ctx.CodexLoreDefinitions
            .Where(d => d.IsAlwaysAvailable && !unlockedIds.Contains(d.LoreId))
            .ToListAsync(cancellationToken);

        return unlocked.Concat(alwaysAvailable).Select(KnowledgeMapping.ToKnowledgeEntry).ToList();
    }
}

/// <summary>
/// Shared definition→<see cref="KnowledgeEntry"/> mapping (moved from
/// <c>CodexSubsystem</c>; the subsystem's copies are deleted in Phase 3).
/// </summary>
internal static class KnowledgeMapping
{
    internal static KnowledgeEntry ToKnowledgeEntry(PersistedLoreDefinition def)
    {
        List<string> tags = string.IsNullOrWhiteSpace(def.Keywords)
            ? []
            : def.Keywords.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

        return new KnowledgeEntry(
            EntryId: def.LoreId,
            Title: def.Title,
            Content: def.Content,
            Category: MapToKnowledgeCategory(def.Category),
            Tags: tags,
            CreatedAt: def.CreatedUtc,
            UpdatedAt: null);
    }

    internal static LoreCategory MapToLoreCategory(KnowledgeCategory category) => category switch
    {
        KnowledgeCategory.History => LoreCategory.History,
        KnowledgeCategory.Geography => LoreCategory.Geography,
        KnowledgeCategory.Magic => LoreCategory.Arcana,
        KnowledgeCategory.Religion => LoreCategory.Religion,
        KnowledgeCategory.Nature => LoreCategory.Nature,
        KnowledgeCategory.Culture => LoreCategory.Local,
        KnowledgeCategory.Organizations => LoreCategory.NobilityAndRoyalty,
        KnowledgeCategory.Creatures => LoreCategory.Nature,
        KnowledgeCategory.Items => LoreCategory.Arcana,
        KnowledgeCategory.Persons => LoreCategory.Local,
        KnowledgeCategory.Events => LoreCategory.History,
        KnowledgeCategory.Legends => LoreCategory.ThePlanes,
        KnowledgeCategory.Secrets => LoreCategory.Dungeoneering,
        _ => LoreCategory.Local
    };

    private static KnowledgeCategory MapToKnowledgeCategory(LoreCategory category) => category switch
    {
        LoreCategory.History => KnowledgeCategory.History,
        LoreCategory.Geography => KnowledgeCategory.Geography,
        LoreCategory.Arcana => KnowledgeCategory.Magic,
        LoreCategory.Religion => KnowledgeCategory.Religion,
        LoreCategory.Nature => KnowledgeCategory.Nature,
        LoreCategory.Local => KnowledgeCategory.Culture,
        LoreCategory.NobilityAndRoyalty => KnowledgeCategory.Organizations,
        LoreCategory.ThePlanes => KnowledgeCategory.Legends,
        LoreCategory.Dungeoneering => KnowledgeCategory.Secrets,
        LoreCategory.ArchitectureAndEngineering => KnowledgeCategory.Culture,
        LoreCategory.Ooc => KnowledgeCategory.Culture,
        _ => KnowledgeCategory.Culture
    };
}
