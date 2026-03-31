using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Aggregates;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Enums;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Objectives;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Repositories;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Application;

/// <summary>
/// Application service that orchestrates the dynamic quest lifecycle:
/// posting, claiming, sharing, unclaiming, and expiration ticks.
/// Coordinates between the <see cref="IDynamicQuestRepository"/>, <see cref="QuestSessionManager"/>,
/// and <see cref="CodexEventProcessor"/>.
/// </summary>
public class DynamicQuestService
{
    private readonly IDynamicQuestRepository _dynamicQuestRepository;
    private readonly QuestSessionManager _sessionManager;
    private readonly CodexEventProcessor _eventProcessor;

    public DynamicQuestService(
        IDynamicQuestRepository dynamicQuestRepository,
        QuestSessionManager sessionManager,
        CodexEventProcessor eventProcessor)
    {
        _dynamicQuestRepository = dynamicQuestRepository ?? throw new ArgumentNullException(nameof(dynamicQuestRepository));
        _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
        _eventProcessor = eventProcessor ?? throw new ArgumentNullException(nameof(eventProcessor));
    }

    /// <summary>
    /// Creates a new posting from a template and persists it.
    /// The posting becomes available on bounty boards, NPCs, or world events.
    /// </summary>
    /// <param name="templateId">The template to create a posting from.</param>
    /// <param name="postedBy">The character (e.g., DM or system) creating the posting. Used for event tracking.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created posting, or null if the template is not found or inactive.</returns>
    public async Task<DynamicQuestPosting?> PostQuestAsync(
        TemplateId templateId,
        CharacterId postedBy,
        CancellationToken cancellationToken = default)
    {
        DynamicQuestTemplate? template = await _dynamicQuestRepository.GetTemplateAsync(templateId, cancellationToken);
        if (template == null || !template.IsActive)
            return null;

        string? validationError = template.Validate();
        if (validationError != null)
            throw new InvalidOperationException($"Template validation failed: {validationError}");

        DateTime now = DateTime.UtcNow;
        DynamicQuestPosting posting = template.CreatePosting(now);

        await _dynamicQuestRepository.SavePostingAsync(posting, cancellationToken);

        await _eventProcessor.EnqueueEventAsync(
            new QuestPostedEvent(postedBy, now, posting.PostingId, templateId, template.Title),
            cancellationToken);

        return posting;
    }

    /// <summary>
    /// Claims a dynamic quest posting for a character.
    /// Validates cooldown, max completions, and slot availability.
    /// Creates a <see cref="CodexQuestEntry"/> and <see cref="QuestSession"/> for the character.
    /// </summary>
    /// <returns>The QuestId assigned to this character's quest instance, or null if claim was rejected.</returns>
    public async Task<QuestId?> ClaimQuestAsync(
        CharacterId characterId,
        PostingId postingId,
        CancellationToken cancellationToken = default)
    {
        DynamicQuestPosting? posting = await _dynamicQuestRepository.GetPostingAsync(postingId, cancellationToken);
        if (posting == null)
            return null;

        DateTime now = DateTime.UtcNow;

        if (posting.IsPostingExpired(now))
            return null;

        if (posting.IsFull)
            return null;

        if (posting.HasClaim(characterId))
            return null;

        // Check max completions
        DynamicQuestTemplate? template = await _dynamicQuestRepository.GetTemplateAsync(posting.SourceTemplateId, cancellationToken);
        if (template == null)
            return null;

        if (template.MaxCompletionsPerCharacter > 0)
        {
            int completionCount = await _dynamicQuestRepository.GetCompletionCountAsync(
                characterId, template.TemplateId, cancellationToken);

            if (completionCount >= template.MaxCompletionsPerCharacter)
                return null; // Max completions reached
        }

        // Check cooldown
        if (template.CooldownAfterCompletion.HasValue)
        {
            DateTime? lastCompletion = await _dynamicQuestRepository.GetLastCompletionTimeAsync(
                characterId, template.TemplateId, cancellationToken);

            if (lastCompletion.HasValue)
            {
                DateTime cooldownEnd = lastCompletion.Value + template.CooldownAfterCompletion.Value;
                if (now < cooldownEnd)
                    return null; // Still in cooldown
            }
        }

        // Claim the posting
        posting.Claim(characterId, now);
        await _dynamicQuestRepository.SavePostingAsync(posting, cancellationToken);

        // Generate a unique QuestId for this character's instance
        QuestId questId = QuestId.NewId();
        DateTime? deadline = posting.CalculateDeadline(now);

        // Collect all objective groups from all stages for the session
        List<QuestObjectiveGroup> allGroups = posting.StageTemplates
            .SelectMany(s => s.ObjectiveGroups)
            .ToList();

        // Create a quest session (with deadline if applicable)
        _sessionManager.CreateSharedSession(
            questId,
            characterId,
            [characterId],
            allGroups,
            deadline,
            now);

        // Emit claimed event → CodexEventProcessor will create the CodexQuestEntry
        await _eventProcessor.EnqueueEventAsync(
            new QuestClaimedEvent(
                characterId, now, postingId, questId, template.TemplateId,
                posting.Title, posting.Description, deadline),
            cancellationToken);

        return questId;
    }

    /// <summary>
    /// Shares a claimant's dynamic quest with a party member for co-op play.
    /// The invitee joins the claimant's session and gets their own codex entry.
    /// </summary>
    public async Task<bool> ShareQuestAsync(
        CharacterId claimantId,
        CharacterId inviteeId,
        PostingId postingId,
        QuestId questId,
        CancellationToken cancellationToken = default)
    {
        DynamicQuestPosting? posting = await _dynamicQuestRepository.GetPostingAsync(postingId, cancellationToken);
        if (posting == null)
            return false;

        // Validate the claimant has an active claim
        if (!posting.HasClaim(claimantId))
            return false;

        // Validate the invitee isn't already participating
        if (posting.IsParticipant(inviteeId))
            return false;

        DateTime now = DateTime.UtcNow;

        // Add the invitee to the posting's claim slot
        posting.ShareWith(claimantId, inviteeId);
        await _dynamicQuestRepository.SavePostingAsync(posting, cancellationToken);

        // Add the invitee to the quest session
        _sessionManager.AddToSession(questId, claimantId, inviteeId);

        // Emit events
        await _eventProcessor.EnqueueEventAsync(
            new QuestSharedEvent(claimantId, now, questId, inviteeId),
            cancellationToken);

        // Create a codex entry for the invitee
        DateTime? deadline = posting.CalculateDeadline(now);
        DynamicQuestTemplate? template = await _dynamicQuestRepository.GetTemplateAsync(posting.SourceTemplateId, cancellationToken);

        await _eventProcessor.EnqueueEventAsync(
            new QuestClaimedEvent(
                inviteeId, now, postingId, questId,
                posting.SourceTemplateId,
                posting.Title, posting.Description,
                deadline),
            cancellationToken);

        return true;
    }

    /// <summary>
    /// Releases a character's claim on a dynamic quest posting.
    /// Removes them from the session and marks the quest as abandoned in their codex.
    /// </summary>
    public async Task<bool> UnclaimQuestAsync(
        CharacterId characterId,
        PostingId postingId,
        QuestId questId,
        CancellationToken cancellationToken = default)
    {
        DynamicQuestPosting? posting = await _dynamicQuestRepository.GetPostingAsync(postingId, cancellationToken);
        if (posting == null)
            return false;

        if (!posting.Unclaim(characterId))
            return false;

        await _dynamicQuestRepository.SavePostingAsync(posting, cancellationToken);

        // Remove from session
        _sessionManager.RemoveFromSession(questId, characterId);

        DateTime now = DateTime.UtcNow;

        // Emit unclaim event → removes quest from codex
        await _eventProcessor.EnqueueEventAsync(
            new QuestUnclaimedEvent(characterId, now, postingId, questId),
            cancellationToken);

        return true;
    }

    /// <summary>
    /// Processes deadline expirations for all active sessions.
    /// Should be called periodically (e.g., from an NWN heartbeat hook or timer).
    /// </summary>
    public async Task TickExpirationsAsync(CancellationToken cancellationToken = default)
    {
        DateTime now = DateTime.UtcNow;

        IReadOnlyList<CodexDomainEvent> expirationEvents = _sessionManager.TickDeadlines(now);

        foreach (CodexDomainEvent evt in expirationEvents)
        {
            await _eventProcessor.EnqueueEventAsync(evt, cancellationToken);
        }

        // Also clean up expired postings from storage
        await _dynamicQuestRepository.RemoveExpiredPostingsAsync(now, cancellationToken);
    }

    /// <summary>
    /// Records a dynamic quest completion for a character.
    /// Increments the completion count and records the timestamp for cooldown tracking.
    /// </summary>
    public async Task RecordCompletionAsync(
        CharacterId characterId,
        TemplateId templateId,
        QuestId questId,
        CancellationToken cancellationToken = default)
    {
        DateTime now = DateTime.UtcNow;

        await _dynamicQuestRepository.RecordCompletionAsync(characterId, templateId, now, cancellationToken);

        // The quest completion event itself is handled by the normal quest system.
        // This method only handles the dynamic-quest-specific tracking.
    }

    /// <summary>
    /// Checks whether a character can currently claim a quest from the given template.
    /// Validates max completions, cooldown, and template active status.
    /// </summary>
    public async Task<(bool CanClaim, string? Reason)> CanClaimAsync(
        CharacterId characterId,
        TemplateId templateId,
        CancellationToken cancellationToken = default)
    {
        DynamicQuestTemplate? template = await _dynamicQuestRepository.GetTemplateAsync(templateId, cancellationToken);
        if (template == null)
            return (false, "Template not found");

        if (!template.IsActive)
            return (false, "Template is not active");

        if (template.MaxCompletionsPerCharacter > 0)
        {
            int completionCount = await _dynamicQuestRepository.GetCompletionCountAsync(
                characterId, templateId, cancellationToken);

            if (completionCount >= template.MaxCompletionsPerCharacter)
                return (false, $"Maximum completions reached ({completionCount}/{template.MaxCompletionsPerCharacter})");
        }

        if (template.CooldownAfterCompletion.HasValue)
        {
            DateTime? lastCompletion = await _dynamicQuestRepository.GetLastCompletionTimeAsync(
                characterId, templateId, cancellationToken);

            if (lastCompletion.HasValue)
            {
                DateTime cooldownEnd = lastCompletion.Value + template.CooldownAfterCompletion.Value;
                TimeSpan remaining = cooldownEnd - DateTime.UtcNow;

                if (remaining > TimeSpan.Zero)
                    return (false, $"Cooldown active — available in {remaining.TotalMinutes:F0} minutes");
            }
        }

        return (true, null);
    }
}
