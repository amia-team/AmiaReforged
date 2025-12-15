namespace AmiaReforged.PwEngine.Features.AI.Core.Models;

/// <summary>
/// Result of XP reward calculation and distribution.
/// </summary>
public sealed class XpRewardResult
{
    /// <summary>
    /// Total XP rewarded across all party members.
    /// </summary>
    public int TotalXpRewarded { get; init; }

    /// <summary>
    /// Total gold rewarded across all party members.
    /// </summary>
    public int TotalGoldRewarded { get; init; }

    /// <summary>
    /// Number of PCs in the party (used for loot calculations).
    /// </summary>
    public int PartyPcCount { get; init; }

    /// <summary>
    /// Whether the party was blocked from receiving XP (level range issue, etc.).
    /// </summary>
    public bool WasBlocked { get; init; }

    /// <summary>
    /// Individual rewards per party member.
    /// </summary>
    public IReadOnlyList<IndividualReward> IndividualRewards { get; init; } = Array.Empty<IndividualReward>();

    /// <summary>
    /// Creates a blocked result (party out of range, etc.).
    /// </summary>
    public static XpRewardResult Blocked() => new()
    {
        WasBlocked = true,
        PartyPcCount = 1
    };
}

/// <summary>
/// XP and gold reward for an individual party member.
/// </summary>
public sealed class IndividualReward
{
    /// <summary>
    /// The player who received the reward.
    /// </summary>
    public required Anvil.API.NwPlayer Player { get; init; }

    /// <summary>
    /// Amount of XP awarded.
    /// </summary>
    public int XpAwarded { get; init; }

    /// <summary>
    /// Amount of gold awarded.
    /// </summary>
    public int GoldAwarded { get; init; }

    /// <summary>
    /// Reason if XP was reduced or blocked.
    /// </summary>
    public string? BlockReason { get; init; }
}
