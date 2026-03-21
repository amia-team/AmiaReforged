namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;

/// <summary>
/// Defines the logarithmic XP curve for proficiency levels 1–125.
/// Each level requires an increasing amount of XP to advance.
/// The formula is: <c>Cost = Floor(BaseCost + ScalingFactor * ln(level + 1))</c>.
/// This produces a gentle logarithmic curve with a higher floor (~100 XP at level 1,
/// ~2000 XP at level 125).
/// </summary>
public static class ProficiencyXpCurve
{
    /// <summary>
    /// Maximum proficiency level achievable (Grandmaster).
    /// </summary>
    public const int MaxLevel = 125;

    /// <summary>
    /// Base XP cost added to every level.
    /// </summary>
    public const int BaseCost = 100;

    /// <summary>
    /// Scaling factor for the logarithmic component.
    /// Tuned so that level 125 costs approximately 2000 XP.
    /// </summary>
    public const double ScalingFactor = 390.0;

    /// <summary>
    /// Tier boundary: levels 1–25 = Novice.
    /// </summary>
    public const int NoviceCeiling = 25;

    /// <summary>
    /// Tier boundary: levels 26–50 = Apprentice.
    /// </summary>
    public const int ApprenticeCeiling = 50;

    /// <summary>
    /// Tier boundary: levels 51–75 = Journeyman.
    /// </summary>
    public const int JourneymanCeiling = 75;

    /// <summary>
    /// Tier boundary: levels 76–100 = Expert.
    /// </summary>
    public const int ExpertCeiling = 100;

    /// <summary>
    /// Tier boundary: levels 101–124 = Master.
    /// </summary>
    public const int MasterCeiling = 124;

    /// <summary>
    /// Returns the XP required to advance from <paramref name="level"/> to the next level.
    /// Returns 0 if already at or beyond <see cref="MaxLevel"/>.
    /// </summary>
    /// <param name="level">Current proficiency level (1-based).</param>
    public static int XpForLevel(int level)
    {
        if (level < 1 || level >= MaxLevel) return 0;

        return (int)Math.Floor(BaseCost + ScalingFactor * Math.Log(level + 1));
    }

    /// <summary>
    /// Returns the cumulative XP required to reach <paramref name="level"/> from level 1.
    /// Useful for display / progress bar calculations.
    /// </summary>
    /// <param name="level">Target proficiency level (1-based). Level 1 returns 0 (start).</param>
    public static int TotalXpForLevel(int level)
    {
        if (level <= 1) return 0;

        int total = 0;
        for (int i = 1; i < level; i++)
        {
            total += XpForLevel(i);
        }

        return total;
    }

    /// <summary>
    /// Returns the <see cref="ProficiencyLevel"/> tier for a given numeric proficiency level.
    /// </summary>
    /// <param name="level">Numeric proficiency level (0–125).</param>
    public static ProficiencyLevel TierForLevel(int level)
    {
        return level switch
        {
            <= 0 => ProficiencyLevel.Layman,
            <= NoviceCeiling => ProficiencyLevel.Novice,
            <= ApprenticeCeiling => ProficiencyLevel.Apprentice,
            <= JourneymanCeiling => ProficiencyLevel.Journeyman,
            <= ExpertCeiling => ProficiencyLevel.Expert,
            <= MasterCeiling => ProficiencyLevel.Master,
            >= MaxLevel => ProficiencyLevel.Grandmaster,
        };
    }

    /// <summary>
    /// Returns the level ceiling for the given <see cref="ProficiencyLevel"/> tier.
    /// For example, Novice returns 25 (levels 1–25 are Novice; level 25 is the ceiling).
    /// </summary>
    public static int CeilingForTier(ProficiencyLevel tier)
    {
        return tier switch
        {
            ProficiencyLevel.Novice => NoviceCeiling,
            ProficiencyLevel.Apprentice => ApprenticeCeiling,
            ProficiencyLevel.Journeyman => JourneymanCeiling,
            ProficiencyLevel.Expert => ExpertCeiling,
            ProficiencyLevel.Master => MasterCeiling,
            ProficiencyLevel.Grandmaster => MaxLevel,
            _ => 0 // Layman has no ceiling
        };
    }

    /// <summary>
    /// Returns the starting level for the given <see cref="ProficiencyLevel"/> tier.
    /// For example, Apprentice returns 26 (first level in the Apprentice tier).
    /// </summary>
    public static int FloorForTier(ProficiencyLevel tier)
    {
        return tier switch
        {
            ProficiencyLevel.Novice => 1,
            ProficiencyLevel.Apprentice => NoviceCeiling + 1,
            ProficiencyLevel.Journeyman => ApprenticeCeiling + 1,
            ProficiencyLevel.Expert => JourneymanCeiling + 1,
            ProficiencyLevel.Master => ExpertCeiling + 1,
            ProficiencyLevel.Grandmaster => MaxLevel,
            _ => 0
        };
    }
}
