using Anvil.API;

namespace AmiaReforged.PwEngine.Features.AI.Core.Models;

/// <summary>
/// Represents the state of a party for XP and loot calculations.
/// Ported from AR_PartyState struct in inc_ds_ondeath.nss.
/// </summary>
public sealed class PartyState
{
    /// <summary>
    /// Number of PCs in the party (max 6 for XP calculations).
    /// </summary>
    public int PcCount { get; init; }

    /// <summary>
    /// Number of associates (henchmen, summons, etc.).
    /// </summary>
    public int HenchmenCount { get; init; }

    /// <summary>
    /// Average hit dice rating excluding associates.
    /// </summary>
    public float AverageLevel { get; init; }

    /// <summary>
    /// Difference between the highest and lowest level PC.
    /// </summary>
    public float LevelDifference { get; init; }

    /// <summary>
    /// The lowest PC level in the party.
    /// </summary>
    public float LowestLevel { get; init; }

    /// <summary>
    /// The highest PC level in the party.
    /// </summary>
    public float HighestLevel { get; init; }

    /// <summary>
    /// XP multiplier based on party size.
    /// </summary>
    public float XpMultiplier { get; init; }

    /// <summary>
    /// Current area where the party is located.
    /// </summary>
    public NwArea? Area { get; init; }

    /// <summary>
    /// List of all party members (PCs only) in the area.
    /// </summary>
    public IReadOnlyList<NwPlayer> PartyMembers { get; init; } = Array.Empty<NwPlayer>();

    /// <summary>
    /// Total party size including henchmen.
    /// </summary>
    public int TotalPartySize => PcCount + HenchmenCount;

    /// <summary>
    /// Whether the party has a level difference penalty (>5 levels apart).
    /// </summary>
    public bool HasLevelPenalty => LevelDifference > 5.0f;

    /// <summary>
    /// Calculates the penalty multiplier for level difference.
    /// </summary>
    public float GetLevelPenalty()
    {
        if (!HasLevelPenalty) return 0f;

        const float basePenalty = 0.15f;
        float penalty = ((int)LevelDifference / 5) * basePenalty;

        // Cap at 80% penalty for extreme level differences (>25)
        return LevelDifference > 25.0f ? 0.80f : penalty;
    }
}
