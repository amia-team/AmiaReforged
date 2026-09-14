using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities.Economy.Treasuries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Banks.Queries;

/// <summary>
/// Gets a single coinhouse record with its accounts.
/// </summary>
public record GetCoinhouseDefinitionQuery : IQuery<CoinHouse?>
{
    public required string Tag { get; init; }
}

/// <summary>
/// Searches coinhouse records. Empty term returns all, ordered by tag.
/// </summary>
public record SearchCoinhouseDefinitionsQuery : IQuery<List<CoinHouse>>
{
    public string? SearchTerm { get; init; }
}

[ServiceBinding(typeof(IQueryHandler<GetCoinhouseDefinitionQuery, CoinHouse?>))]
public sealed class GetCoinhouseDefinitionHandler : IQueryHandler<GetCoinhouseDefinitionQuery, CoinHouse?>
{
    private readonly PwContextFactory _contextFactory;

    public GetCoinhouseDefinitionHandler(PwContextFactory contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<CoinHouse?> HandleAsync(GetCoinhouseDefinitionQuery query, CancellationToken cancellationToken = default)
    {
        using PwEngineContext context = _contextFactory.CreateDbContext();
        return await context.CoinHouses
            .Include(c => c.Accounts)
            .FirstOrDefaultAsync(c => c.Tag == query.Tag, cancellationToken);
    }
}

[ServiceBinding(typeof(IQueryHandler<SearchCoinhouseDefinitionsQuery, List<CoinHouse>>))]
public sealed class SearchCoinhouseDefinitionsHandler : IQueryHandler<SearchCoinhouseDefinitionsQuery, List<CoinHouse>>
{
    private readonly PwContextFactory _contextFactory;

    public SearchCoinhouseDefinitionsHandler(PwContextFactory contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<CoinHouse>> HandleAsync(SearchCoinhouseDefinitionsQuery query, CancellationToken cancellationToken = default)
    {
        using PwEngineContext context = _contextFactory.CreateDbContext();
        IQueryable<CoinHouse> dbQuery = context.CoinHouses;

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            string term = query.SearchTerm.Trim().ToLower();
            dbQuery = dbQuery.Where(c => c.Tag.ToLower().Contains(term));
        }

        return await dbQuery
            .OrderBy(c => c.Tag)
            .Include(c => c.Accounts)
            .ToListAsync(cancellationToken);
    }
}
