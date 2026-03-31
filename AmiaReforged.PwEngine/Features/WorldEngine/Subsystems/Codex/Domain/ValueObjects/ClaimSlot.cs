using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.ValueObjects;

/// <summary>
/// Represents a single claim on a dynamic quest posting, including the claimant
/// and any party members they have chosen to share the quest with.
/// </summary>
public sealed record ClaimSlot
{
    /// <summary>The character who claimed the posting.</summary>
    public required CharacterId ClaimantId { get; init; }

    /// <summary>When the claim was made (UTC).</summary>
    public required DateTime ClaimedAt { get; init; }

    /// <summary>
    /// Characters the claimant has invited to share this quest.
    /// Shared members contribute to the same session objectives and share rewards.
    /// </summary>
    public List<CharacterId> SharedWith { get; init; } = [];

    /// <summary>
    /// Returns all characters participating in this claim (claimant + shared members).
    /// </summary>
    public IReadOnlyList<CharacterId> AllParticipants
    {
        get
        {
            List<CharacterId> all = [ClaimantId];
            all.AddRange(SharedWith);
            return all;
        }
    }

    /// <summary>
    /// Adds a party member to this claim's shared list.
    /// </summary>
    public void AddSharedMember(CharacterId memberId)
    {
        if (memberId == ClaimantId)
            throw new InvalidOperationException("Cannot share a quest with yourself");

        if (SharedWith.Contains(memberId))
            throw new InvalidOperationException($"Character {memberId.Value} is already shared on this claim");

        SharedWith.Add(memberId);
    }

    /// <summary>
    /// Removes a party member from this claim's shared list.
    /// </summary>
    public bool RemoveSharedMember(CharacterId memberId) => SharedWith.Remove(memberId);
}
