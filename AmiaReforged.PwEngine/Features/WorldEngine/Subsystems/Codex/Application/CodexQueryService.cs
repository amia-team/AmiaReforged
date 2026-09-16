using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Enums;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Aggregates;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Application.Queries;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Application;

/// <summary>
/// Compatibility shim over the player-codex <see cref="IQuery{TResult}"/> handlers.
/// Every read routes through <see cref="IQueryDispatcher"/> so callers
/// (PlayerCodexPresenter, dialogue condition evaluators) stay on the dispatch
/// path without signature changes. New code should dispatch the queries in
/// <c>Application/Queries/PlayerCodexQueries.cs</c> directly. (F-6 audit.)
/// </summary>
[ServiceBinding(typeof(CodexQueryService))]
public class CodexQueryService
{
    private readonly IQueryDispatcher _queries;

    public CodexQueryService(IQueryDispatcher queries)
    {
        _queries = queries ?? throw new ArgumentNullException(nameof(queries));
    }

    #region Quest Queries

    /// <summary>
    /// Gets all quests for a character
    /// </summary>
    public Task<IReadOnlyList<CodexQuestEntry>> GetAllQuestsAsync(CharacterId characterId, CancellationToken cancellationToken = default)
    {
        return _queries.DispatchAsync<GetCodexQuestsQuery, IReadOnlyList<CodexQuestEntry>>(
            new GetCodexQuestsQuery { CharacterId = characterId }, cancellationToken);
    }

    /// <summary>
    /// Gets quests by state
    /// </summary>
    public Task<IReadOnlyList<CodexQuestEntry>> GetQuestsByStateAsync(
        CharacterId characterId,
        QuestState state,
        CancellationToken cancellationToken = default)
    {
        return _queries.DispatchAsync<GetCodexQuestsByStateQuery, IReadOnlyList<CodexQuestEntry>>(
            new GetCodexQuestsByStateQuery { CharacterId = characterId, State = state }, cancellationToken);
    }

    /// <summary>
    /// Searches quests by text
    /// </summary>
    public Task<IReadOnlyList<CodexQuestEntry>> SearchQuestsAsync(
        CharacterId characterId,
        string searchTerm,
        CancellationToken cancellationToken = default)
    {
        return _queries.DispatchAsync<SearchCodexQuestsQuery, IReadOnlyList<CodexQuestEntry>>(
            new SearchCodexQuestsQuery { CharacterId = characterId, SearchTerm = searchTerm }, cancellationToken);
    }

    #endregion

    #region Lore Queries

    /// <summary>
    /// Gets all lore for a character
    /// </summary>
    public Task<IReadOnlyList<CodexLoreEntry>> GetAllLoreAsync(CharacterId characterId, CancellationToken cancellationToken = default)
    {
        return _queries.DispatchAsync<GetCodexLoreQuery, IReadOnlyList<CodexLoreEntry>>(
            new GetCodexLoreQuery { CharacterId = characterId }, cancellationToken);
    }

    /// <summary>
    /// Gets lore by tier
    /// </summary>
    public Task<IReadOnlyList<CodexLoreEntry>> GetLoreByTierAsync(
        CharacterId characterId,
        LoreTier tier,
        CancellationToken cancellationToken = default)
    {
        return _queries.DispatchAsync<GetCodexLoreByTierQuery, IReadOnlyList<CodexLoreEntry>>(
            new GetCodexLoreByTierQuery { CharacterId = characterId, Tier = tier }, cancellationToken);
    }

    /// <summary>
    /// Gets lore by category
    /// </summary>
    public Task<IReadOnlyList<CodexLoreEntry>> GetLoreByCategoryAsync(
        CharacterId characterId,
        LoreCategory category,
        CancellationToken cancellationToken = default)
    {
        return _queries.DispatchAsync<GetCodexLoreByCategoryQuery, IReadOnlyList<CodexLoreEntry>>(
            new GetCodexLoreByCategoryQuery { CharacterId = characterId, Category = category }, cancellationToken);
    }

    /// <summary>
    /// Searches lore by text
    /// </summary>
    public Task<IReadOnlyList<CodexLoreEntry>> SearchLoreAsync(
        CharacterId characterId,
        string searchTerm,
        CancellationToken cancellationToken = default)
    {
        return _queries.DispatchAsync<SearchCodexLoreQuery, IReadOnlyList<CodexLoreEntry>>(
            new SearchCodexLoreQuery { CharacterId = characterId, SearchTerm = searchTerm }, cancellationToken);
    }

    #endregion

    #region Note Queries

    /// <summary>
    /// Gets all notes for a character
    /// </summary>
    public Task<IReadOnlyList<CodexNoteEntry>> GetAllNotesAsync(CharacterId characterId, CancellationToken cancellationToken = default)
    {
        return _queries.DispatchAsync<GetCodexNotesQuery, IReadOnlyList<CodexNoteEntry>>(
            new GetCodexNotesQuery { CharacterId = characterId }, cancellationToken);
    }

    /// <summary>
    /// Gets notes by category
    /// </summary>
    public Task<IReadOnlyList<CodexNoteEntry>> GetNotesByCategoryAsync(
        CharacterId characterId,
        NoteCategory category,
        CancellationToken cancellationToken = default)
    {
        return _queries.DispatchAsync<GetCodexNotesByCategoryQuery, IReadOnlyList<CodexNoteEntry>>(
            new GetCodexNotesByCategoryQuery { CharacterId = characterId, Category = category }, cancellationToken);
    }

    /// <summary>
    /// Gets DM notes for a character
    /// </summary>
    public Task<IReadOnlyList<CodexNoteEntry>> GetDmNotesAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default)
    {
        return _queries.DispatchAsync<GetCodexDmNotesQuery, IReadOnlyList<CodexNoteEntry>>(
            new GetCodexDmNotesQuery { CharacterId = characterId }, cancellationToken);
    }

    /// <summary>
    /// Searches notes by text
    /// </summary>
    public Task<IReadOnlyList<CodexNoteEntry>> SearchNotesAsync(
        CharacterId characterId,
        string searchTerm,
        CancellationToken cancellationToken = default)
    {
        return _queries.DispatchAsync<SearchCodexNotesQuery, IReadOnlyList<CodexNoteEntry>>(
            new SearchCodexNotesQuery { CharacterId = characterId, SearchTerm = searchTerm }, cancellationToken);
    }

    #endregion

    #region Reputation Queries

    /// <summary>
    /// Gets all faction reputations for a character
    /// </summary>
    public Task<IReadOnlyList<FactionReputation>> GetAllReputationsAsync(CharacterId characterId, CancellationToken cancellationToken = default)
    {
        return _queries.DispatchAsync<GetCodexReputationsQuery, IReadOnlyList<FactionReputation>>(
            new GetCodexReputationsQuery { CharacterId = characterId }, cancellationToken);
    }

    /// <summary>
    /// Gets reputation with a specific faction
    /// </summary>
    public Task<FactionReputation?> GetReputationAsync(
        CharacterId characterId,
        FactionId factionId,
        CancellationToken cancellationToken = default)
    {
        return _queries.DispatchAsync<GetCodexReputationQuery, FactionReputation?>(
            new GetCodexReputationQuery { CharacterId = characterId, FactionId = factionId }, cancellationToken);
    }

    /// <summary>
    /// Gets all positive faction reputations for a character
    /// </summary>
    public Task<IReadOnlyList<FactionReputation>> GetPositiveReputationsAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default)
    {
        return _queries.DispatchAsync<GetCodexPositiveReputationsQuery, IReadOnlyList<FactionReputation>>(
            new GetCodexPositiveReputationsQuery { CharacterId = characterId }, cancellationToken);
    }

    /// <summary>
    /// Gets all negative faction reputations for a character
    /// </summary>
    public Task<IReadOnlyList<FactionReputation>> GetNegativeReputationsAsync(
        CharacterId characterId,
        CancellationToken cancellationToken = default)
    {
        return _queries.DispatchAsync<GetCodexNegativeReputationsQuery, IReadOnlyList<FactionReputation>>(
            new GetCodexNegativeReputationsQuery { CharacterId = characterId }, cancellationToken);
    }

    #endregion

    #region Trait Queries

    /// <summary>
    /// Gets all traits for a character
    /// </summary>
    public Task<IReadOnlyList<CodexTraitEntry>> GetAllTraitsAsync(CharacterId characterId, CancellationToken cancellationToken = default)
    {
        return _queries.DispatchAsync<GetCodexTraitsQuery, IReadOnlyList<CodexTraitEntry>>(
            new GetCodexTraitsQuery { CharacterId = characterId }, cancellationToken);
    }

    /// <summary>
    /// Gets traits by category
    /// </summary>
    public Task<IReadOnlyList<CodexTraitEntry>> GetTraitsByCategoryAsync(
        CharacterId characterId,
        TraitCategory category,
        CancellationToken cancellationToken = default)
    {
        return _queries.DispatchAsync<GetCodexTraitsByCategoryQuery, IReadOnlyList<CodexTraitEntry>>(
            new GetCodexTraitsByCategoryQuery { CharacterId = characterId, Category = category }, cancellationToken);
    }

    /// <summary>
    /// Searches traits by text
    /// </summary>
    public Task<IReadOnlyList<CodexTraitEntry>> SearchTraitsAsync(
        CharacterId characterId,
        string searchTerm,
        CancellationToken cancellationToken = default)
    {
        return _queries.DispatchAsync<SearchCodexTraitsQuery, IReadOnlyList<CodexTraitEntry>>(
            new SearchCodexTraitsQuery { CharacterId = characterId, SearchTerm = searchTerm }, cancellationToken);
    }

    #endregion

    #region Summary Queries

    /// <summary>
    /// Gets codex statistics
    /// </summary>
    public Task<CodexStatistics> GetStatisticsAsync(CharacterId characterId, CancellationToken cancellationToken = default)
    {
        return _queries.DispatchAsync<GetCodexStatisticsQuery, CodexStatistics>(
            new GetCodexStatisticsQuery { CharacterId = characterId }, cancellationToken);
    }

    #endregion
}

/// <summary>
/// DTO for codex statistics
/// </summary>
public record CodexStatistics(
    int TotalQuests,
    int CompletedQuests,
    int ActiveQuests,
    int TotalLore,
    int TotalNotes,
    int TotalFactions,
    int TotalTraits,
    DateTime? LastUpdated
);
