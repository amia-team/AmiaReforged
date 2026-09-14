using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Dialogue.Application.Queries;

/// <summary>
/// Gets a single dialogue tree by ID.
/// </summary>
public record GetDialogueTreeQuery : IQuery<PersistedDialogueTree?>
{
    public required string DialogueTreeId { get; init; }
}

/// <summary>
/// Searches dialogue trees. Empty term returns all, ordered by title.
/// </summary>
public record SearchDialogueTreesQuery : IQuery<List<PersistedDialogueTree>>
{
    public string? SearchTerm { get; init; }
}

/// <summary>
/// Lists dialogue trees for one NPC speaker tag, ordered by title.
/// </summary>
public record GetDialogueTreesBySpeakerQuery : IQuery<List<PersistedDialogueTree>>
{
    public required string SpeakerTag { get; init; }
}

[ServiceBinding(typeof(IQueryHandler<GetDialogueTreeQuery, PersistedDialogueTree?>))]
public sealed class GetDialogueTreeHandler : IQueryHandler<GetDialogueTreeQuery, PersistedDialogueTree?>
{
    private readonly PwContextFactory _contextFactory;

    public GetDialogueTreeHandler(PwContextFactory contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<PersistedDialogueTree?> HandleAsync(GetDialogueTreeQuery query, CancellationToken cancellationToken = default)
    {
        using PwEngineContext context = _contextFactory.CreateDbContext();
        return await context.DialogueTrees.FindAsync([query.DialogueTreeId], cancellationToken);
    }
}

[ServiceBinding(typeof(IQueryHandler<SearchDialogueTreesQuery, List<PersistedDialogueTree>>))]
public sealed class SearchDialogueTreesHandler : IQueryHandler<SearchDialogueTreesQuery, List<PersistedDialogueTree>>
{
    private readonly PwContextFactory _contextFactory;

    public SearchDialogueTreesHandler(PwContextFactory contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<PersistedDialogueTree>> HandleAsync(SearchDialogueTreesQuery query, CancellationToken cancellationToken = default)
    {
        using PwEngineContext context = _contextFactory.CreateDbContext();
        IQueryable<PersistedDialogueTree> dbQuery = context.DialogueTrees;

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            string term = query.SearchTerm.Trim().ToLower();
            dbQuery = dbQuery.Where(d =>
                d.DialogueTreeId.ToLower().Contains(term) ||
                d.Title.ToLower().Contains(term) ||
                (d.SpeakerTag != null && d.SpeakerTag.ToLower().Contains(term)));
        }

        return await dbQuery
            .OrderBy(d => d.Title)
            .ToListAsync(cancellationToken);
    }
}

[ServiceBinding(typeof(IQueryHandler<GetDialogueTreesBySpeakerQuery, List<PersistedDialogueTree>>))]
public sealed class GetDialogueTreesBySpeakerHandler : IQueryHandler<GetDialogueTreesBySpeakerQuery, List<PersistedDialogueTree>>
{
    private readonly PwContextFactory _contextFactory;

    public GetDialogueTreesBySpeakerHandler(PwContextFactory contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<PersistedDialogueTree>> HandleAsync(GetDialogueTreesBySpeakerQuery query, CancellationToken cancellationToken = default)
    {
        using PwEngineContext context = _contextFactory.CreateDbContext();
        return await context.DialogueTrees
            .Where(d => d.SpeakerTag == query.SpeakerTag)
            .OrderBy(d => d.Title)
            .ToListAsync(cancellationToken);
    }
}
