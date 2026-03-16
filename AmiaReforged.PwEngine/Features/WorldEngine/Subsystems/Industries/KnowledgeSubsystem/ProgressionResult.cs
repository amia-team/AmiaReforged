namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.KnowledgeSubsystem;

/// <summary>
/// Result of awarding progression points to a character.
/// </summary>
public class ProgressionResult
{
    /// <summary>
    /// Whether the operation was successful.
    /// </summary>
    public bool Success { get; init; }

    /// <summary>
    /// Number of new economy knowledge points earned from this award
    /// (may be 0 if threshold not yet reached, or &gt;1 for large awards).
    /// </summary>
    public int KnowledgePointsEarned { get; init; }

    /// <summary>
    /// New total economy-earned KP after this award.
    /// </summary>
    public int NewEconomyKnowledgePointTotal { get; init; }

    /// <summary>
    /// Total KP (economy + level-up) after this award.
    /// </summary>
    public int NewTotalKnowledgePoints { get; init; }

    /// <summary>
    /// Remaining progression points toward the next economy KP.
    /// </summary>
    public int ProgressionPointsRemaining { get; init; }

    /// <summary>
    /// Progression points required for the next economy KP.
    /// </summary>
    public int ProgressionPointsRequired { get; init; }

    /// <summary>
    /// Whether the character is now at or beyond the soft cap.
    /// </summary>
    public bool IsAtSoftCap { get; init; }

    /// <summary>
    /// Whether the character is now at the hard cap (no more economy KP can be earned).
    /// </summary>
    public bool IsAtHardCap { get; init; }

    /// <summary>
    /// Optional message for the player.
    /// </summary>
    public string? Message { get; init; }

    public static ProgressionResult Blocked(string message) => new()
    {
        Success = false,
        Message = message
    };
}
