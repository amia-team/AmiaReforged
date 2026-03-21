namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;

/// <summary>
/// Result of awarding proficiency XP to an industry membership.
/// </summary>
public class ProficiencyXpResult
{
    /// <summary>
    /// Whether XP was successfully awarded.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// The new numeric proficiency level after XP was applied.
    /// </summary>
    public int NewLevel { get; init; }

    /// <summary>
    /// Remaining XP toward the next level (after any level-ups consumed XP).
    /// </summary>
    public int XpRemaining { get; init; }

    /// <summary>
    /// XP required to advance from the new level to the next level.
    /// 0 if at max level.
    /// </summary>
    public int XpRequired { get; init; }

    /// <summary>
    /// Number of proficiency levels gained from this XP award.
    /// </summary>
    public int LevelsGained { get; init; }

    /// <summary>
    /// True if the character is at the ceiling of their current tier and must rank up to continue gaining XP.
    /// </summary>
    public bool IsAtTierCeiling { get; init; }

    /// <summary>
    /// Human-readable status message.
    /// </summary>
    public string? Message { get; init; }

    public static ProficiencyXpResult Blocked(int currentLevel, string message) => new()
    {
        Success = false,
        NewLevel = currentLevel,
        XpRemaining = 0,
        XpRequired = ProficiencyXpCurve.XpForLevel(currentLevel),
        LevelsGained = 0,
        IsAtTierCeiling = true,
        Message = message
    };
}
