namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;

/// <summary>
/// Domain constants for the crafting quality scale (0–15).
/// Maps to NWN <c>IP_CONST_QUALITY_*</c> constants extended with additional tiers.
/// </summary>
public static class CraftingQuality
{
    // ─── Core quality tiers (NWN IP_CONST_QUALITY_*) ───
    public const int Unknown = 0;
    public const int Destroyed = 1;
    public const int Ruined = 2;
    public const int VeryPoor = 3;
    public const int Poor = 4;
    public const int BelowAverage = 5;
    public const int Average = 6;
    public const int AboveAverage = 7;
    public const int Good = 8;
    public const int VeryGood = 9;
    public const int Excellent = 10;
    public const int Masterwork = 11;
    public const int GodLike = 12;
    public const int Raw = 13;
    public const int Cut = 14;
    public const int Polished = 15;

    // ─── Bounds for crafting output ───

    /// <summary>
    /// Minimum quality a crafted product can have.
    /// </summary>
    public const int MinCraftable = Destroyed; // 1

    /// <summary>
    /// Maximum quality a crafted product can have through normal crafting.
    /// </summary>
    public const int MaxCraftable = Masterwork; // 11

    /// <summary>
    /// Absolute minimum of the full scale.
    /// </summary>
    public const int MinValue = Unknown; // 0

    /// <summary>
    /// Absolute maximum of the full scale.
    /// </summary>
    public const int MaxValue = Polished; // 15

    /// <summary>
    /// Default base quality used when input ingredients have no quality.
    /// Corresponds to <see cref="Average"/> (6).
    /// </summary>
    public const int DefaultBaseQuality = Average; // 6

    /// <summary>
    /// Clamps a quality value to the craftable range [<see cref="MinCraftable"/>, <see cref="MaxCraftable"/>].
    /// </summary>
    public static int Clamp(int quality) => Math.Clamp(quality, MinCraftable, MaxCraftable);

    /// <summary>
    /// Computes the base quality from a set of input ingredient qualities.
    /// Returns the floored average of all non-null values, or <see cref="DefaultBaseQuality"/> if all are null/empty.
    /// </summary>
    public static int ComputeBaseQuality(IReadOnlyList<int?> inputQualities)
    {
        if (inputQualities.Count == 0) return DefaultBaseQuality;

        List<int> nonNull = inputQualities.Where(q => q.HasValue).Select(q => q!.Value).ToList();
        if (nonNull.Count == 0) return DefaultBaseQuality;

        return (int)Math.Floor((double)nonNull.Sum() / nonNull.Count);
    }

    /// <summary>
    /// Returns the human-readable label for a given quality tier.
    /// </summary>
    public static string Label(int quality) => quality switch
    {
        Unknown => "Unknown",
        Destroyed => "Destroyed",
        Ruined => "Ruined",
        VeryPoor => "Very Poor",
        Poor => "Poor",
        BelowAverage => "Below Average",
        Average => "Average",
        AboveAverage => "Above Average",
        Good => "Good",
        VeryGood => "Very Good",
        Excellent => "Excellent",
        Masterwork => "Masterwork",
        GodLike => "God-Like",
        Raw => "Raw",
        Cut => "Cut",
        Polished => "Polished",
        _ => $"Quality {quality}"
    };
}
