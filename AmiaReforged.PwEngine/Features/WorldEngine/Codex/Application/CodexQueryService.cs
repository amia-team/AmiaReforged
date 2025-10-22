using AmiaReforged.PwEngine.Features.Codex.Domain.Enums;
using AmiaReforged.PwEngine.Features.Codex.Domain.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Codex.Aggregates;
using AmiaReforged.PwEngine.Features.WorldEngine.Codex.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Codex.Application;

/// <summary>
/// Query service for read-only codex operations.
/// Provides DTOs and search capabilities without exposing aggregate internals.
/// </summary>
public class CodexQueryService
{
    private readonly IPlayerCodexRepository _repository;

    public CodexQueryService(IPlayerCodexRepository repository)
    {
        _repository = repository ?? throw new ArgumentNullException(nameof(repository));
    }

    #region Quest Queries

    /// <summary>
    /// Gets all quests for a character
    /// </summary>
    public async Task<IReadOnlyList<CodexQuestEntry>> GetAllQuestsAsync(CharacterId characterId, CancellationToken cancellationToken = default)
    {
        var codex = await _repository.LoadAsync(characterId, cancellationToken);
        return codex?.Quests.ToList() ?? new List<CodexQuestEntry>();
    }

    /// <summary>
    /// Gets quests by state
    /// </summary>
    public async Task<IReadOnlyList<CodexQuestEntry>> GetQuestsByStateAsync(
        CharacterId characterId,
        QuestState state,
        CancellationToken cancellationToken = default)
    {
        var codex = await _repository.LoadAsync(characterId, cancellationToken);
        return codex?.GetQuestsByState(state).ToList() ?? new List<CodexQuestEntry>();
    }

    /// <summary>
    /// Searches quests by text
    /// </summary>
    public async Task<IReadOnlyList<CodexQuestEntry>> SearchQuestsAsync(
        CharacterId characterId,
        string searchTerm,
        CancellationToken cancellationToken = default)
    {
        var codex = await _repository.LoadAsync(characterId, cancellationToken);
        return codex?.SearchQuests(searchTerm).ToList() ?? new List<CodexQuestEntry>();
    }

    #endregion

    #region Lore Queries

    /// <summary>
    /// Gets all lore for a character
    /// </summary>
    public async Task<IReadOnlyList<CodexLoreEntry>> GetAllLoreAsync(CharacterId characterId, CancellationToken cancellationToken = default)
    {
        var codex = await _repository.LoadAsync(characterId, cancellationToken);
        return codex?.Lore.ToList() ?? new List<CodexLoreEntry>();
    }

    /// <summary>
    /// Gets lore by tier
    /// </summary>
    public async Task<IReadOnlyList<CodexLoreEntry>> GetLoreByTierAsync(
        CharacterId characterId,
        LoreTier tier,
        CancellationToken cancellationToken = default)
    {
        var codex = await _repository.LoadAsync(characterId, cancellationToken);
        return codex?.GetLoreByTier(tier).ToList() ?? new List<CodexLoreEntry>();
    }

    /// <summary>
    /// Searches lore by text
    /// </summary>
    public async Task<IReadOnlyList<CodexLoreEntry>> SearchLoreAsync(
        CharacterId characterId,
        string searchTerm,
        CancellationToken cancellationToken = default)
    {
        var codex = await _repository.LoadAsync(characterId, cancellationToken);
        return codex?.SearchLore(searchTerm).ToList() ?? new List<CodexLoreEntry>();
    }

    #endregion

    #region Note Queries

    /// <summary>
    /// Gets all notes for a character
    /// </summary>
    public async Task<IReadOnlyList<CodexNoteEntry>> GetAllNotesAsync(CharacterId characterId, CancellationToken cancellationToken = default)
    {
        var codex = await _repository.LoadAsync(characterId, cancellationToken);
        return codex?.Notes.ToList() ?? new List<CodexNoteEntry>();
    }

    /// <summary>
    /// Gets notes by category
    /// </summary>
    public async Task<IReadOnlyList<CodexNoteEntry>> GetNotesByCategoryAsync(
        CharacterId characterId,
        NoteCategory category,
        CancellationToken cancellationToken = default)
    {
        var codex = await _repository.LoadAsync(characterId, cancellationToken);
        return codex?.GetNotesByCategory(category).ToList() ?? new List<CodexNoteEntry>();
    }

    /// <summary>
    /// Searches notes by text
    /// </summary>
    public async Task<IReadOnlyList<CodexNoteEntry>> SearchNotesAsync(
        CharacterId characterId,
        string searchTerm,
        CancellationToken cancellationToken = default)
    {
        var codex = await _repository.LoadAsync(characterId, cancellationToken);
        return codex?.SearchNotes(searchTerm).ToList() ?? new List<CodexNoteEntry>();
    }

    #endregion

    #region Reputation Queries

    /// <summary>
    /// Gets all faction reputations for a character
    /// </summary>
    public async Task<IReadOnlyList<FactionReputation>> GetAllReputationsAsync(CharacterId characterId, CancellationToken cancellationToken = default)
    {
        var codex = await _repository.LoadAsync(characterId, cancellationToken);
        return codex?.Reputations.ToList() ?? new List<FactionReputation>();
    }

    /// <summary>
    /// Gets reputation with a specific faction
    /// </summary>
    public async Task<FactionReputation?> GetReputationAsync(
        CharacterId characterId,
        FactionId factionId,
        CancellationToken cancellationToken = default)
    {
        var codex = await _repository.LoadAsync(characterId, cancellationToken);
        return codex?.GetReputation(factionId);
    }

    #endregion

    #region Summary Queries

    /// <summary>
    /// Gets codex statistics
    /// </summary>
    public async Task<CodexStatistics> GetStatisticsAsync(CharacterId characterId, CancellationToken cancellationToken = default)
    {
        var codex = await _repository.LoadAsync(characterId, cancellationToken);

        if (codex == null)
        {
            return new CodexStatistics(
                TotalQuests: 0,
                CompletedQuests: 0,
                ActiveQuests: 0,
                TotalLore: 0,
                TotalNotes: 0,
                TotalFactions: 0,
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
            LastUpdated: codex.LastUpdated
        );
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
    DateTime? LastUpdated
);
