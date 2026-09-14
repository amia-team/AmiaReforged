using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Application.Queries;

/// <summary>
/// Gets a single quest definition by ID.
/// </summary>
public record GetQuestDefinitionQuery : IQuery<PersistedQuestDefinition?>
{
    public required string QuestId { get; init; }
}

/// <summary>
/// Searches quest definitions. Empty term returns all, ordered by title.
/// </summary>
public record SearchQuestDefinitionsQuery : IQuery<List<PersistedQuestDefinition>>
{
    public string? SearchTerm { get; init; }
}

[ServiceBinding(typeof(IQueryHandler<GetQuestDefinitionQuery, PersistedQuestDefinition?>))]
public sealed class GetQuestDefinitionHandler : IQueryHandler<GetQuestDefinitionQuery, PersistedQuestDefinition?>
{
    private readonly PwContextFactory _contextFactory;

    public GetQuestDefinitionHandler(PwContextFactory contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<PersistedQuestDefinition?> HandleAsync(GetQuestDefinitionQuery query, CancellationToken cancellationToken = default)
    {
        using PwEngineContext context = _contextFactory.CreateDbContext();
        return await context.CodexQuestDefinitions.FindAsync([query.QuestId], cancellationToken);
    }
}

[ServiceBinding(typeof(IQueryHandler<SearchQuestDefinitionsQuery, List<PersistedQuestDefinition>>))]
public sealed class SearchQuestDefinitionsHandler : IQueryHandler<SearchQuestDefinitionsQuery, List<PersistedQuestDefinition>>
{
    private readonly PwContextFactory _contextFactory;

    public SearchQuestDefinitionsHandler(PwContextFactory contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<PersistedQuestDefinition>> HandleAsync(SearchQuestDefinitionsQuery query, CancellationToken cancellationToken = default)
    {
        using PwEngineContext context = _contextFactory.CreateDbContext();
        IQueryable<PersistedQuestDefinition> dbQuery = context.CodexQuestDefinitions;

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            string term = query.SearchTerm.Trim().ToLower();
            dbQuery = dbQuery.Where(d =>
                d.QuestId.ToLower().Contains(term) ||
                d.Title.ToLower().Contains(term) ||
                (d.Keywords != null && d.Keywords.ToLower().Contains(term)));
        }

        return await dbQuery
            .OrderBy(d => d.Title)
            .ToListAsync(cancellationToken);
    }
}
