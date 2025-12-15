namespace AmiaReforged.PwEngine.Features.AI.Core.Models;

/// <summary>
/// Represents the loot tier based on creature CR.
/// Ported from loot bin logic in inc_ds_ondeath.nss.
/// </summary>
public enum LootTier
{
    /// <summary>
    /// CR 1-8: Very low-level loot.
    /// </summary>
    UberLow = 0,

    /// <summary>
    /// CR 9-16: Low-level loot.
    /// </summary>
    Low = 1,

    /// <summary>
    /// CR 17-25: Medium-level loot.
    /// </summary>
    Medium = 2,

    /// <summary>
    /// CR 26-40: High-level loot.
    /// </summary>
    High = 3,

    /// <summary>
    /// CR 41+: Uber/Boss loot.
    /// </summary>
    Uber = 4
}

/// <summary>
/// Extension methods for LootTier.
/// </summary>
public static class LootTierExtensions
{
    /// <summary>
    /// Gets the loot bin tag suffix for this tier.
    /// </summary>
    public static string GetBinSuffix(this LootTier tier) => tier switch
    {
        LootTier.Uber => "UBER",
        LootTier.High => "HIGH",
        LootTier.Medium => "MEDIUM",
        LootTier.Low => "LOW",
        LootTier.UberLow => "UBERLOW",
        _ => "UBERLOW"
    };

    /// <summary>
    /// Gets the full loot bin tag for this tier.
    /// </summary>
    public static string GetBinTag(this LootTier tier) => $"CD_TREASURE_{tier.GetBinSuffix()}";

    /// <summary>
    /// Gets the mythal level suffix for this tier.
    /// </summary>
    public static string GetMythalSuffix(this LootTier tier) => tier switch
    {
        LootTier.Uber => "5",
        LootTier.High => "4",
        LootTier.Medium => "3",
        LootTier.Low => "2",
        LootTier.UberLow => "1",
        _ => "1"
    };

    /// <summary>
    /// Determines the loot tier from a creature's CR.
    /// </summary>
    public static LootTier FromChallengeRating(float cr, bool isBoss = false)
    {
        int crInt = (int)cr;

        // Cap non-boss creatures at 40 CR
        if (crInt > 40 && !isBoss)
        {
            crInt = 40;
        }

        return crInt switch
        {
            >= 41 => LootTier.Uber,
            >= 26 => LootTier.High,
            >= 17 => LootTier.Medium,
            >= 9 => LootTier.Low,
            _ => LootTier.UberLow
        };
    }
}
