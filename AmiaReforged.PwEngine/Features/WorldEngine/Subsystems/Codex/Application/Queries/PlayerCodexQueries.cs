using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Application;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Aggregates;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Enums;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Repositories;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.ValueObjects;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Application.Queries;

/// <summary>
/// Aggregate-backed player-codex reads. 1:1 CQRS counterparts of the former
/// bespoke <see cref="CodexQueryService"/> methods (F-6 audit); the service
/// delegates to these handlers until its callers move to dispatch.
/// </summary>
public record GetCodexQuestsQuery : IQuery<IReadOnlyList<CodexQuestEntry>>
{
    public required CharacterId CharacterId { get; init; }
}

public record GetCodexQuestsByStateQuery : IQuery<IReadOnlyList<CodexQuestEntry>>
{
    public required CharacterId CharacterId { get; init; }
    public required QuestState State { get; init; }
}

public record SearchCodexQuestsQuery : IQuery<IReadOnlyList<CodexQuestEntry>>
{
    public required CharacterId CharacterId { get; init; }
    public required string SearchTerm { get; init; }
}

public record GetCodexLoreQuery : IQuery<IReadOnlyList<CodexLoreEntry>>
{
    public required CharacterId CharacterId { get; init; }
}

public record GetCodexLoreByTierQuery : IQuery<IReadOnlyList<CodexLoreEntry>>
{
    public required CharacterId CharacterId { get; init; }
    public required LoreTier Tier { get; init; }
}

public record GetCodexLoreByCategoryQuery : IQuery<IReadOnlyList<CodexLoreEntry>>
{
    public required CharacterId CharacterId { get; init; }
    public required LoreCategory Category { get; init; }
}

public record SearchCodexLoreQuery : IQuery<IReadOnlyList<CodexLoreEntry>>
{
    public required CharacterId CharacterId { get; init; }
    public required string SearchTerm { get; init; }
}

public record GetCodexNotesQuery : IQuery<IReadOnlyList<CodexNoteEntry>>
{
    public required CharacterId CharacterId { get; init; }
}

public record GetCodexNotesByCategoryQuery : IQuery<IReadOnlyList<CodexNoteEntry>>
{
    public required CharacterId CharacterId { get; init; }
    public required NoteCategory Category { get; init; }
}

public record GetCodexDmNotesQuery : IQuery<IReadOnlyList<CodexNoteEntry>>
{
    public required CharacterId CharacterId { get; init; }
}

public record SearchCodexNotesQuery : IQuery<IReadOnlyList<CodexNoteEntry>>
{
    public required CharacterId CharacterId { get; init; }
    public required string SearchTerm { get; init; }
}

public record GetCodexReputationsQuery : IQuery<IReadOnlyList<FactionReputation>>
{
    public required CharacterId CharacterId { get; init; }
}

public record GetCodexReputationQuery : IQuery<FactionReputation?>
{
    public required CharacterId CharacterId { get; init; }
    public required FactionId FactionId { get; init; }
}

public record GetCodexPositiveReputationsQuery : IQuery<IReadOnlyList<FactionReputation>>
{
    public required CharacterId CharacterId { get; init; }
}

public record GetCodexNegativeReputationsQuery : IQuery<IReadOnlyList<FactionReputation>>
{
    public required CharacterId CharacterId { get; init; }
}

public record GetCodexTraitsQuery : IQuery<IReadOnlyList<CodexTraitEntry>>
{
    public required CharacterId CharacterId { get; init; }
}

public record GetCodexTraitsByCategoryQuery : IQuery<IReadOnlyList<CodexTraitEntry>>
{
    public required CharacterId CharacterId { get; init; }
    public required TraitCategory Category { get; init; }
}

public record SearchCodexTraitsQuery : IQuery<IReadOnlyList<CodexTraitEntry>>
{
    public required CharacterId CharacterId { get; init; }
    public required string SearchTerm { get; init; }
}

public record GetCodexStatisticsQuery : IQuery<CodexStatistics>
{
    public required CharacterId CharacterId { get; init; }
}

[ServiceBinding(typeof(IQueryHandler<GetCodexQuestsQuery, IReadOnlyList<CodexQuestEntry>>))]
public sealed class GetCodexQuestsHandler : IQueryHandler<GetCodexQuestsQuery, IReadOnlyList<CodexQuestEntry>>
{
    private readonly IPlayerCodexRepository _repository;
    public GetCodexQuestsHandler(IPlayerCodexRepository repository) => _repository = repository;
    public async Task<IReadOnlyList<CodexQuestEntry>> HandleAsync(GetCodexQuestsQuery query, CancellationToken ct = default)
    {
        PlayerCodex? codex = await _repository.LoadAsync(query.CharacterId, ct);
        return codex?.Quests.ToList() ?? new List<CodexQuestEntry>();
    }
}

[ServiceBinding(typeof(IQueryHandler<GetCodexQuestsByStateQuery, IReadOnlyList<CodexQuestEntry>>))]
public sealed class GetCodexQuestsByStateHandler : IQueryHandler<GetCodexQuestsByStateQuery, IReadOnlyList<CodexQuestEntry>>
{
    private readonly IPlayerCodexRepository _repository;
    public GetCodexQuestsByStateHandler(IPlayerCodexRepository repository) => _repository = repository;
    public async Task<IReadOnlyList<CodexQuestEntry>> HandleAsync(GetCodexQuestsByStateQuery query, CancellationToken ct = default)
    {
        PlayerCodex? codex = await _repository.LoadAsync(query.CharacterId, ct);
        return codex?.GetQuestsByState(query.State).ToList() ?? new List<CodexQuestEntry>();
    }
}

[ServiceBinding(typeof(IQueryHandler<SearchCodexQuestsQuery, IReadOnlyList<CodexQuestEntry>>))]
public sealed class SearchCodexQuestsHandler : IQueryHandler<SearchCodexQuestsQuery, IReadOnlyList<CodexQuestEntry>>
{
    private readonly IPlayerCodexRepository _repository;
    public SearchCodexQuestsHandler(IPlayerCodexRepository repository) => _repository = repository;
    public async Task<IReadOnlyList<CodexQuestEntry>> HandleAsync(SearchCodexQuestsQuery query, CancellationToken ct = default)
    {
        PlayerCodex? codex = await _repository.LoadAsync(query.CharacterId, ct);
        return codex?.SearchQuests(query.SearchTerm).ToList() ?? new List<CodexQuestEntry>();
    }
}

[ServiceBinding(typeof(IQueryHandler<GetCodexLoreQuery, IReadOnlyList<CodexLoreEntry>>))]
public sealed class GetCodexLoreHandler : IQueryHandler<GetCodexLoreQuery, IReadOnlyList<CodexLoreEntry>>
{
    private readonly IPlayerCodexRepository _repository;
    public GetCodexLoreHandler(IPlayerCodexRepository repository) => _repository = repository;
    public async Task<IReadOnlyList<CodexLoreEntry>> HandleAsync(GetCodexLoreQuery query, CancellationToken ct = default)
    {
        PlayerCodex? codex = await _repository.LoadAsync(query.CharacterId, ct);
        return codex?.Lore.ToList() ?? new List<CodexLoreEntry>();
    }
}

[ServiceBinding(typeof(IQueryHandler<GetCodexLoreByTierQuery, IReadOnlyList<CodexLoreEntry>>))]
public sealed class GetCodexLoreByTierHandler : IQueryHandler<GetCodexLoreByTierQuery, IReadOnlyList<CodexLoreEntry>>
{
    private readonly IPlayerCodexRepository _repository;
    public GetCodexLoreByTierHandler(IPlayerCodexRepository repository) => _repository = repository;
    public async Task<IReadOnlyList<CodexLoreEntry>> HandleAsync(GetCodexLoreByTierQuery query, CancellationToken ct = default)
    {
        PlayerCodex? codex = await _repository.LoadAsync(query.CharacterId, ct);
        return codex?.GetLoreByTier(query.Tier).ToList() ?? new List<CodexLoreEntry>();
    }
}

[ServiceBinding(typeof(IQueryHandler<GetCodexLoreByCategoryQuery, IReadOnlyList<CodexLoreEntry>>))]
public sealed class GetCodexLoreByCategoryHandler : IQueryHandler<GetCodexLoreByCategoryQuery, IReadOnlyList<CodexLoreEntry>>
{
    private readonly IPlayerCodexRepository _repository;
    public GetCodexLoreByCategoryHandler(IPlayerCodexRepository repository) => _repository = repository;
    public async Task<IReadOnlyList<CodexLoreEntry>> HandleAsync(GetCodexLoreByCategoryQuery query, CancellationToken ct = default)
    {
        PlayerCodex? codex = await _repository.LoadAsync(query.CharacterId, ct);
        return codex?.GetLoreByCategory(query.Category).ToList() ?? new List<CodexLoreEntry>();
    }
}

[ServiceBinding(typeof(IQueryHandler<SearchCodexLoreQuery, IReadOnlyList<CodexLoreEntry>>))]
public sealed class SearchCodexLoreHandler : IQueryHandler<SearchCodexLoreQuery, IReadOnlyList<CodexLoreEntry>>
{
    private readonly IPlayerCodexRepository _repository;
    public SearchCodexLoreHandler(IPlayerCodexRepository repository) => _repository = repository;
    public async Task<IReadOnlyList<CodexLoreEntry>> HandleAsync(SearchCodexLoreQuery query, CancellationToken ct = default)
    {
        PlayerCodex? codex = await _repository.LoadAsync(query.CharacterId, ct);
        return codex?.SearchLore(query.SearchTerm).ToList() ?? new List<CodexLoreEntry>();
    }
}

[ServiceBinding(typeof(IQueryHandler<GetCodexNotesQuery, IReadOnlyList<CodexNoteEntry>>))]
public sealed class GetCodexNotesHandler : IQueryHandler<GetCodexNotesQuery, IReadOnlyList<CodexNoteEntry>>
{
    private readonly IPlayerCodexRepository _repository;
    public GetCodexNotesHandler(IPlayerCodexRepository repository) => _repository = repository;
    public async Task<IReadOnlyList<CodexNoteEntry>> HandleAsync(GetCodexNotesQuery query, CancellationToken ct = default)
    {
        PlayerCodex? codex = await _repository.LoadAsync(query.CharacterId, ct);
        return codex?.Notes.ToList() ?? new List<CodexNoteEntry>();
    }
}

[ServiceBinding(typeof(IQueryHandler<GetCodexNotesByCategoryQuery, IReadOnlyList<CodexNoteEntry>>))]
public sealed class GetCodexNotesByCategoryHandler : IQueryHandler<GetCodexNotesByCategoryQuery, IReadOnlyList<CodexNoteEntry>>
{
    private readonly IPlayerCodexRepository _repository;
    public GetCodexNotesByCategoryHandler(IPlayerCodexRepository repository) => _repository = repository;
    public async Task<IReadOnlyList<CodexNoteEntry>> HandleAsync(GetCodexNotesByCategoryQuery query, CancellationToken ct = default)
    {
        PlayerCodex? codex = await _repository.LoadAsync(query.CharacterId, ct);
        return codex?.GetNotesByCategory(query.Category).ToList() ?? new List<CodexNoteEntry>();
    }
}

[ServiceBinding(typeof(IQueryHandler<GetCodexDmNotesQuery, IReadOnlyList<CodexNoteEntry>>))]
public sealed class GetCodexDmNotesHandler : IQueryHandler<GetCodexDmNotesQuery, IReadOnlyList<CodexNoteEntry>>
{
    private readonly IPlayerCodexRepository _repository;
    public GetCodexDmNotesHandler(IPlayerCodexRepository repository) => _repository = repository;
    public async Task<IReadOnlyList<CodexNoteEntry>> HandleAsync(GetCodexDmNotesQuery query, CancellationToken ct = default)
    {
        PlayerCodex? codex = await _repository.LoadAsync(query.CharacterId, ct);
        return codex?.GetNotesByCategory(NoteCategory.DmNote).ToList() ?? new List<CodexNoteEntry>();
    }
}

[ServiceBinding(typeof(IQueryHandler<SearchCodexNotesQuery, IReadOnlyList<CodexNoteEntry>>))]
public sealed class SearchCodexNotesHandler : IQueryHandler<SearchCodexNotesQuery, IReadOnlyList<CodexNoteEntry>>
{
    private readonly IPlayerCodexRepository _repository;
    public SearchCodexNotesHandler(IPlayerCodexRepository repository) => _repository = repository;
    public async Task<IReadOnlyList<CodexNoteEntry>> HandleAsync(SearchCodexNotesQuery query, CancellationToken ct = default)
    {
        PlayerCodex? codex = await _repository.LoadAsync(query.CharacterId, ct);
        return codex?.SearchNotes(query.SearchTerm).ToList() ?? new List<CodexNoteEntry>();
    }
}

[ServiceBinding(typeof(IQueryHandler<GetCodexReputationsQuery, IReadOnlyList<FactionReputation>>))]
public sealed class GetCodexReputationsHandler : IQueryHandler<GetCodexReputationsQuery, IReadOnlyList<FactionReputation>>
{
    private readonly IPlayerCodexRepository _repository;
    public GetCodexReputationsHandler(IPlayerCodexRepository repository) => _repository = repository;
    public async Task<IReadOnlyList<FactionReputation>> HandleAsync(GetCodexReputationsQuery query, CancellationToken ct = default)
    {
        PlayerCodex? codex = await _repository.LoadAsync(query.CharacterId, ct);
        return codex?.Reputations.ToList() ?? new List<FactionReputation>();
    }
}

[ServiceBinding(typeof(IQueryHandler<GetCodexReputationQuery, FactionReputation?>))]
public sealed class GetCodexReputationHandler : IQueryHandler<GetCodexReputationQuery, FactionReputation?>
{
    private readonly IPlayerCodexRepository _repository;
    public GetCodexReputationHandler(IPlayerCodexRepository repository) => _repository = repository;
    public async Task<FactionReputation?> HandleAsync(GetCodexReputationQuery query, CancellationToken ct = default)
    {
        PlayerCodex? codex = await _repository.LoadAsync(query.CharacterId, ct);
        return codex?.GetReputation(query.FactionId);
    }
}

[ServiceBinding(typeof(IQueryHandler<GetCodexPositiveReputationsQuery, IReadOnlyList<FactionReputation>>))]
public sealed class GetCodexPositiveReputationsHandler : IQueryHandler<GetCodexPositiveReputationsQuery, IReadOnlyList<FactionReputation>>
{
    private readonly IPlayerCodexRepository _repository;
    public GetCodexPositiveReputationsHandler(IPlayerCodexRepository repository) => _repository = repository;
    public async Task<IReadOnlyList<FactionReputation>> HandleAsync(GetCodexPositiveReputationsQuery query, CancellationToken ct = default)
    {
        PlayerCodex? codex = await _repository.LoadAsync(query.CharacterId, ct);
        return codex?.Reputations.Where(r => r.CurrentScore.Value > 0).ToList() ?? new List<FactionReputation>();
    }
}

[ServiceBinding(typeof(IQueryHandler<GetCodexNegativeReputationsQuery, IReadOnlyList<FactionReputation>>))]
public sealed class GetCodexNegativeReputationsHandler : IQueryHandler<GetCodexNegativeReputationsQuery, IReadOnlyList<FactionReputation>>
{
    private readonly IPlayerCodexRepository _repository;
    public GetCodexNegativeReputationsHandler(IPlayerCodexRepository repository) => _repository = repository;
    public async Task<IReadOnlyList<FactionReputation>> HandleAsync(GetCodexNegativeReputationsQuery query, CancellationToken ct = default)
    {
        PlayerCodex? codex = await _repository.LoadAsync(query.CharacterId, ct);
        return codex?.Reputations.Where(r => r.CurrentScore.Value < 0).ToList() ?? new List<FactionReputation>();
    }
}

[ServiceBinding(typeof(IQueryHandler<GetCodexTraitsQuery, IReadOnlyList<CodexTraitEntry>>))]
public sealed class GetCodexTraitsHandler : IQueryHandler<GetCodexTraitsQuery, IReadOnlyList<CodexTraitEntry>>
{
    private readonly IPlayerCodexRepository _repository;
    public GetCodexTraitsHandler(IPlayerCodexRepository repository) => _repository = repository;
    public async Task<IReadOnlyList<CodexTraitEntry>> HandleAsync(GetCodexTraitsQuery query, CancellationToken ct = default)
    {
        PlayerCodex? codex = await _repository.LoadAsync(query.CharacterId, ct);
        return codex?.Traits.ToList() ?? new List<CodexTraitEntry>();
    }
}

[ServiceBinding(typeof(IQueryHandler<GetCodexTraitsByCategoryQuery, IReadOnlyList<CodexTraitEntry>>))]
public sealed class GetCodexTraitsByCategoryHandler : IQueryHandler<GetCodexTraitsByCategoryQuery, IReadOnlyList<CodexTraitEntry>>
{
    private readonly IPlayerCodexRepository _repository;
    public GetCodexTraitsByCategoryHandler(IPlayerCodexRepository repository) => _repository = repository;
    public async Task<IReadOnlyList<CodexTraitEntry>> HandleAsync(GetCodexTraitsByCategoryQuery query, CancellationToken ct = default)
    {
        PlayerCodex? codex = await _repository.LoadAsync(query.CharacterId, ct);
        return codex?.GetTraitsByCategory(query.Category).ToList() ?? new List<CodexTraitEntry>();
    }
}

[ServiceBinding(typeof(IQueryHandler<SearchCodexTraitsQuery, IReadOnlyList<CodexTraitEntry>>))]
public sealed class SearchCodexTraitsHandler : IQueryHandler<SearchCodexTraitsQuery, IReadOnlyList<CodexTraitEntry>>
{
    private readonly IPlayerCodexRepository _repository;
    public SearchCodexTraitsHandler(IPlayerCodexRepository repository) => _repository = repository;
    public async Task<IReadOnlyList<CodexTraitEntry>> HandleAsync(SearchCodexTraitsQuery query, CancellationToken ct = default)
    {
        PlayerCodex? codex = await _repository.LoadAsync(query.CharacterId, ct);
        return codex?.SearchTraits(query.SearchTerm).ToList() ?? new List<CodexTraitEntry>();
    }
}

[ServiceBinding(typeof(IQueryHandler<GetCodexStatisticsQuery, CodexStatistics>))]
public sealed class GetCodexStatisticsHandler : IQueryHandler<GetCodexStatisticsQuery, CodexStatistics>
{
    private readonly IPlayerCodexRepository _repository;
    public GetCodexStatisticsHandler(IPlayerCodexRepository repository) => _repository = repository;
    public async Task<CodexStatistics> HandleAsync(GetCodexStatisticsQuery query, CancellationToken ct = default)
    {
        PlayerCodex? codex = await _repository.LoadAsync(query.CharacterId, ct);

        if (codex == null)
        {
            return new CodexStatistics(
                TotalQuests: 0,
                CompletedQuests: 0,
                ActiveQuests: 0,
                TotalLore: 0,
                TotalNotes: 0,
                TotalFactions: 0,
                TotalTraits: 0,
                LastUpdated: null
            );
        }

        return new CodexStatistics(
            TotalQuests: codex.Quests.Count,
            CompletedQuests: codex.GetQuestsByState(QuestState.Completed).Count(),
            ActiveQuests: codex.GetQuestsByState(QuestState.InProgress).Count(),
            TotalLore: codex.Lore.Count,
            TotalNotes: codex.Notes.Count,
            TotalFactions: codex.Reputations.Count,
            TotalTraits: codex.Traits.Count,
            LastUpdated: codex.LastUpdated
        );
    }
}
