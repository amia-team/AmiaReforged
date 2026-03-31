using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Enums;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.Entities;

/// <summary>
/// A live, claimable quest instance created from a <see cref="Aggregates.DynamicQuestTemplate"/>.
/// Tracks which adventurers have claimed it, enforces slot limits, and manages posting-level expiry.
/// </summary>
public class DynamicQuestPosting
{
    private readonly List<ClaimSlot> _claims = [];

    /// <summary>
    /// Unique identifier for this posting.
    /// </summary>
    public required PostingId PostingId { get; init; }

    /// <summary>
    /// The template this posting was created from.
    /// </summary>
    public required TemplateId SourceTemplateId { get; init; }

    /// <summary>
    /// Display title (copied from template at posting time).
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Description (copied from template at posting time).
    /// </summary>
    public required string Description { get; init; }

    /// <summary>
    /// When this posting was created (UTC).
    /// </summary>
    public required DateTime PostedAt { get; init; }

    /// <summary>
    /// When this posting expires and is removed from boards/NPCs (UTC).
    /// Null means the posting stays available indefinitely.
    /// </summary>
    public DateTime? ExpiresAt { get; init; }

    /// <summary>
    /// How many adventurers can simultaneously claim this posting.
    /// </summary>
    public required ClaimMode ClaimMode { get; init; }

    /// <summary>
    /// Maximum number of simultaneous claimants.
    /// Only meaningful when <see cref="ClaimMode"/> is <see cref="Enums.ClaimMode.Limited"/>.
    /// </summary>
    public required int MaxClaimants { get; init; }

    /// <summary>
    /// Per-claimant time limit from claim to expiry. Null means no time limit.
    /// </summary>
    public TimeSpan? TimeLimit { get; init; }

    /// <summary>
    /// What happens when a claimant's time limit elapses.
    /// </summary>
    public ExpiryBehavior ExpiryBehavior { get; init; } = ExpiryBehavior.Fail;

    /// <summary>
    /// Reward granted upon quest completion.
    /// </summary>
    public RewardMix BaseReward { get; init; } = RewardMix.Empty;

    /// <summary>
    /// Stage definitions for quests created from this posting.
    /// </summary>
    public List<QuestStage> StageTemplates { get; init; } = [];

    /// <summary>
    /// Keywords for searching and filtering.
    /// </summary>
    public List<Keyword> Keywords { get; init; } = [];

    /// <summary>
    /// Read-only view of all current claims on this posting.
    /// </summary>
    public IReadOnlyList<ClaimSlot> Claims => _claims;

    /// <summary>
    /// Number of remaining claim slots. Returns <see cref="int.MaxValue"/> for unlimited postings.
    /// </summary>
    public int RemainingSlots => ClaimMode switch
    {
        ClaimMode.Unlimited => int.MaxValue,
        ClaimMode.Limited => Math.Max(0, MaxClaimants - _claims.Count),
        ClaimMode.Exclusive => _claims.Count == 0 ? 1 : 0,
        _ => 0
    };

    /// <summary>
    /// Whether all claim slots are taken.
    /// </summary>
    public bool IsFull => ClaimMode switch
    {
        ClaimMode.Unlimited => false,
        ClaimMode.Limited => _claims.Count >= MaxClaimants,
        ClaimMode.Exclusive => _claims.Count >= 1,
        _ => true
    };

    /// <summary>
    /// Whether this posting has expired (posting-level expiry, not per-claimant).
    /// </summary>
    public bool IsPostingExpired(DateTime now) => ExpiresAt.HasValue && now >= ExpiresAt.Value;

    /// <summary>
    /// Claims this posting for a character. Returns the created claim slot.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the posting is full or already claimed by this character.</exception>
    public ClaimSlot Claim(CharacterId characterId, DateTime claimedAt)
    {
        if (IsFull)
            throw new InvalidOperationException(
                $"Posting {PostingId} is full ({_claims.Count}/{MaxClaimants} slots taken)");

        if (_claims.Any(c => c.ClaimantId == characterId))
            throw new InvalidOperationException(
                $"Character {characterId.Value} has already claimed posting {PostingId}");

        ClaimSlot slot = new()
        {
            ClaimantId = characterId,
            ClaimedAt = claimedAt
        };

        _claims.Add(slot);
        return slot;
    }

    /// <summary>
    /// Shares a claimant's quest with another character for co-op play.
    /// The invitee joins the claimant's quest session with shared objective progress.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the claimant has no active claim.</exception>
    public void ShareWith(CharacterId claimantId, CharacterId inviteeId)
    {
        ClaimSlot? slot = _claims.FirstOrDefault(c => c.ClaimantId == claimantId);
        if (slot == null)
            throw new InvalidOperationException(
                $"Character {claimantId.Value} has no active claim on posting {PostingId}");

        slot.AddSharedMember(inviteeId);
    }

    /// <summary>
    /// Releases a character's claim on this posting, freeing the slot.
    /// Also removes the character from any shared claims they are part of.
    /// </summary>
    /// <returns>True if a claim was removed; false if the character had no claim.</returns>
    public bool Unclaim(CharacterId characterId)
    {
        // Remove as primary claimant
        ClaimSlot? ownClaim = _claims.FirstOrDefault(c => c.ClaimantId == characterId);
        if (ownClaim != null)
        {
            _claims.Remove(ownClaim);
            return true;
        }

        // Remove as a shared member from someone else's claim
        foreach (ClaimSlot slot in _claims)
        {
            if (slot.RemoveSharedMember(characterId))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Gets the claim slot for a specific character (as claimant), or null.
    /// </summary>
    public ClaimSlot? GetClaim(CharacterId characterId)
        => _claims.FirstOrDefault(c => c.ClaimantId == characterId);

    /// <summary>
    /// Checks if a character has an active claim (as primary claimant) on this posting.
    /// </summary>
    public bool HasClaim(CharacterId characterId)
        => _claims.Any(c => c.ClaimantId == characterId);

    /// <summary>
    /// Checks if a character is participating in any claim (as claimant or shared member).
    /// </summary>
    public bool IsParticipant(CharacterId characterId)
        => _claims.Any(c => c.ClaimantId == characterId || c.SharedWith.Contains(characterId));

    /// <summary>
    /// Calculates the deadline for a character who claims this posting at the given time.
    /// Returns null if there is no time limit.
    /// </summary>
    public DateTime? CalculateDeadline(DateTime claimedAt)
        => TimeLimit.HasValue ? claimedAt + TimeLimit.Value : null;

    /// <summary>
    /// Restores a claim slot during aggregate reconstitution (e.g., from persistence).
    /// Bypasses validation — only for use by repositories.
    /// </summary>
    internal void RestoreClaim(ClaimSlot slot) => _claims.Add(slot);
}
