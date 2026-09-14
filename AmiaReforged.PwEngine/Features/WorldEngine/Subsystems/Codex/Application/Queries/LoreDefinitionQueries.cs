using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Enums;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Application.Queries;

/// <summary>
/// Gets a single lore definition by ID.
/// </summary>
public record GetLoreDefinitionQuery : IQuery<PersistedLoreDefinition?>
{
    public required string LoreId { get; init; }
}

/// <summary>
/// Searches lore definitions with an optional category filter. Empty term returns all.
/// Results are ordered by category then title.
/// </summary>
public record SearchLoreDefinitionsQuery : IQuery<List<PersistedLoreDefinition>>
{
    public string? SearchTerm { get; init; }
    public int? Category { get; init; }
}

[ServiceBinding(typeof(IQueryHandler<GetLoreDefinitionQuery, PersistedLoreDefinition?>))]
public sealed class GetLoreDefinitionHandler : IQueryHandler<GetLoreDefinitionQuery, PersistedLoreDefinition?>
{
    private readonly PwContextFactory _contextFactory;

    public GetLoreDefinitionHandler(PwContextFactory contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<PersistedLoreDefinition?> HandleAsync(GetLoreDefinitionQuery query, CancellationToken cancellationToken = default)
    {
        using PwEngineContext context = _contextFactory.CreateDbContext();
        return await context.CodexLoreDefinitions.FindAsync([query.LoreId], cancellationToken);
    }
}

[ServiceBinding(typeof(IQueryHandler<SearchLoreDefinitionsQuery, List<PersistedLoreDefinition>>))]
public sealed class SearchLoreDefinitionsHandler : IQueryHandler<SearchLoreDefinitionsQuery, List<PersistedLoreDefinition>>
{
    private readonly PwContextFactory _contextFactory;

    public SearchLoreDefinitionsHandler(PwContextFactory contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<PersistedLoreDefinition>> HandleAsync(SearchLoreDefinitionsQuery query, CancellationToken cancellationToken = default)
    {
        using PwEngineContext context = _contextFactory.CreateDbContext();
        IQueryable<PersistedLoreDefinition> dbQuery = context.CodexLoreDefinitions;

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            string term = query.SearchTerm.Trim().ToLower();
            dbQuery = dbQuery.Where(d =>
                d.LoreId.ToLower().Contains(term) ||
                d.Title.ToLower().Contains(term) ||
                (d.Keywords != null && d.Keywords.ToLower().Contains(term)));
        }

        if (query.Category.HasValue && Enum.IsDefined(typeof(LoreCategory), query.Category.Value))
        {
            LoreCategory category = (LoreCategory)query.Category.Value;
            dbQuery = dbQuery.Where(d => d.Category == category);
        }

        return await dbQuery
            .OrderBy(d => d.Category)
            .ThenBy(d => d.Title)
            .ToListAsync(cancellationToken);
    }
}
