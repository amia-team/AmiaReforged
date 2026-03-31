using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Aggregates;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Enums;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Repositories;

/// <summary>
/// Repository interface for dynamic quest template and posting persistence.
/// Handles storage of quest blueprints (templates) and live claimable instances (postings).
/// </summary>
public interface IDynamicQuestRepository
{
    #region Template Operations

    /// <summary>
    /// Loads a dynamic quest template by its identifier.
    /// </summary>
    Task<DynamicQuestTemplate?> GetTemplateAsync(TemplateId templateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all active templates (IsActive == true).
    /// </summary>
    Task<IReadOnlyList<DynamicQuestTemplate>> GetActiveTemplatesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all active templates matching the given source type.
    /// </summary>
    Task<IReadOnlyList<DynamicQuestTemplate>> GetActiveTemplatesBySourceAsync(
        DynamicQuestSource source, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists a template (insert or update).
    /// </summary>
    Task SaveTemplateAsync(DynamicQuestTemplate template, CancellationToken cancellationToken = default);

    #endregion

    #region Posting Operations

    /// <summary>
    /// Loads a dynamic quest posting by its identifier.
    /// </summary>
    Task<DynamicQuestPosting?> GetPostingAsync(PostingId postingId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all active (non-expired) postings.
    /// </summary>
    Task<IReadOnlyList<DynamicQuestPosting>> GetActivePostingsAsync(
        DateTime now, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all active postings originating from the given source type.
    /// Requires joining back to the template to filter by source.
    /// </summary>
    Task<IReadOnlyList<DynamicQuestPosting>> GetActivePostingsForSourceAsync(
        DynamicQuestSource source, DateTime now, CancellationToken cancellationToken = default);

    /// <summary>
    /// Persists a posting (insert or update), including its claim slots.
    /// </summary>
    Task SavePostingAsync(DynamicQuestPosting posting, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes expired postings from storage.
    /// </summary>
    Task RemoveExpiredPostingsAsync(DateTime now, CancellationToken cancellationToken = default);

    #endregion

    #region Character Completion Tracking

    /// <summary>
    /// Returns how many times a character has completed quests from the given template.
    /// </summary>
    Task<int> GetCompletionCountAsync(
        CharacterId characterId, TemplateId templateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns when a character last completed a quest from the given template, or null if never.
    /// </summary>
    Task<DateTime?> GetLastCompletionTimeAsync(
        CharacterId characterId, TemplateId templateId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Records a quest completion for cooldown and max-completion tracking.
    /// </summary>
    Task RecordCompletionAsync(
        CharacterId characterId, TemplateId templateId, DateTime completedAt,
        CancellationToken cancellationToken = default);

    #endregion
}
