using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Application.Traits.Queries;

/// <summary>
/// Gets a single trait definition by tag.
/// </summary>
public record GetTraitDefinitionQuery : IQuery<PersistedTraitDefinition?>
{
    public required string Tag { get; init; }
}

/// <summary>
/// Searches trait definitions with optional filters. Empty term returns all.
/// Results are ordered by category then name.
/// </summary>
public record SearchTraitDefinitionsQuery : IQuery<List<PersistedTraitDefinition>>
{
    public string? SearchTerm { get; init; }
    public string? Category { get; init; }
    public string? DeathBehavior { get; init; }
    public bool? DmOnly { get; init; }
}

[ServiceBinding(typeof(IQueryHandler<GetTraitDefinitionQuery, PersistedTraitDefinition?>))]
public sealed class GetTraitDefinitionHandler : IQueryHandler<GetTraitDefinitionQuery, PersistedTraitDefinition?>
{
    private readonly PwContextFactory _contextFactory;

    public GetTraitDefinitionHandler(PwContextFactory contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<PersistedTraitDefinition?> HandleAsync(GetTraitDefinitionQuery query, CancellationToken cancellationToken = default)
    {
        using PwEngineContext context = _contextFactory.CreateDbContext();
        return await context.TraitDefinitions.FindAsync([query.Tag], cancellationToken);
    }
}

[ServiceBinding(typeof(IQueryHandler<SearchTraitDefinitionsQuery, List<PersistedTraitDefinition>>))]
public sealed class SearchTraitDefinitionsHandler : IQueryHandler<SearchTraitDefinitionsQuery, List<PersistedTraitDefinition>>
{
    private readonly PwContextFactory _contextFactory;

    public SearchTraitDefinitionsHandler(PwContextFactory contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<PersistedTraitDefinition>> HandleAsync(SearchTraitDefinitionsQuery query, CancellationToken cancellationToken = default)
    {
        using PwEngineContext context = _contextFactory.CreateDbContext();
        IQueryable<PersistedTraitDefinition> dbQuery = context.TraitDefinitions;

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            string term = query.SearchTerm.Trim().ToLower();
            dbQuery = dbQuery.Where(d =>
                d.Tag.ToLower().Contains(term) ||
                d.Name.ToLower().Contains(term) ||
                d.Description.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(query.Category)
            && Enum.TryParse<TraitCategory>(query.Category.Trim(), true, out TraitCategory cat))
        {
            dbQuery = dbQuery.Where(d => d.Category == cat);
        }

        if (!string.IsNullOrWhiteSpace(query.DeathBehavior)
            && Enum.TryParse<TraitDeathBehavior>(query.DeathBehavior.Trim(), true, out TraitDeathBehavior db))
        {
            dbQuery = dbQuery.Where(d => d.DeathBehavior == db);
        }

        if (query.DmOnly.HasValue)
        {
            bool dm = query.DmOnly.Value;
            dbQuery = dbQuery.Where(d => d.DmOnly == dm);
        }

        return await dbQuery
            .OrderBy(d => d.Category)
            .ThenBy(d => d.Name)
            .ToListAsync(cancellationToken);
    }
}
