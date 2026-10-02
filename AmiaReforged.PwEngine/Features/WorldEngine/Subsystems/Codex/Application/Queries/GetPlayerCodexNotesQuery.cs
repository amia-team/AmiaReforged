using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Enums;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Repositories;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Application.Queries;

public record GetPlayerCodexNotesQuery : IQuery<IReadOnlyList<CodexNoteEntry>>
{
    public required CharacterId CharacterId { get; init; }
    public NoteCategory? Category { get; init; }
    public string SearchTerm { get; init; } = "";
}

[ServiceBinding(typeof(IQueryHandler<GetPlayerCodexNotesQuery, IReadOnlyList<CodexNoteEntry>>))]
public sealed class GetPlayerCodexNotesHandler : IQueryHandler<GetPlayerCodexNotesQuery, IReadOnlyList<CodexNoteEntry>>
{
    private readonly IPlayerCodexRepository _repository;

    public GetPlayerCodexNotesHandler(IPlayerCodexRepository repository) => _repository = repository;

    public async Task<IReadOnlyList<CodexNoteEntry>> HandleAsync(GetPlayerCodexNotesQuery query, CancellationToken ct = default)
    {
        var codex = await _repository.LoadAsync(query.CharacterId, ct);
        string search = query.SearchTerm.Trim();
        return codex?.Notes
            .Where(n => n.IsPlayerVisible)
            .Where(n => query.Category == null || n.Category == query.Category)
            .Where(n => search.Length == 0 || n.MatchesSearch(search))
            .OrderByDescending(n => n.LastModified)
            .ThenBy(n => n.Id)
            .ToList() ?? new List<CodexNoteEntry>();
    }
}
