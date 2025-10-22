using AmiaReforged.PwEngine.Features.Codex.Domain.ValueObjects;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Codex.Entities;

/// <summary>
/// Entity representing a character's reputation with a faction.
/// Tracks reputation score, changes over time, and current standing.
/// </summary>
public class FactionReputation
{
    /// <summary>
    /// Identifier for the faction
    /// </summary>
    public required FactionId FactionId { get; init; }

    /// <summary>
    /// Display name of the faction
    /// </summary>
    public required string FactionName { get; init; }

    /// <summary>
    /// Current reputation score
    /// </summary>
    public ReputationScore CurrentScore { get; private set; }

    /// <summary>
    /// When reputation was first established (first interaction)
    /// </summary>
    public DateTime DateEstablished { get; init; }

    /// <summary>
    /// When reputation was last changed
    /// </summary>
    public DateTime LastChanged { get; private set; }

    /// <summary>
    /// Optional description of the faction
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// History of reputation changes (for tracking progression)
    /// </summary>
    private readonly List<ReputationChange> _history = new();

    /// <summary>
    /// Read-only view of reputation history
    /// </summary>
    public IReadOnlyList<ReputationChange> History => _history.AsReadOnly();

    /// <summary>
    /// Creates a new faction reputation with the specified initial score
    /// </summary>
    public FactionReputation(ReputationScore initialScore, DateTime establishedDate)
    {
        CurrentScore = initialScore;
        LastChanged = establishedDate;
    }

    /// <summary>
    /// Parameterless constructor for object initializer syntax (required properties must be set)
    /// </summary>
    public FactionReputation()
    {
        CurrentScore = ReputationScore.CreateNeutral();
        LastChanged = DateTime.UtcNow;
    }

    /// <summary>
    /// Adjusts the reputation by a delta value
    /// </summary>
    public void AdjustReputation(int delta, string reason, DateTime changedAt)
    {
        if (delta == 0)
            return;

        var oldScore = CurrentScore;
        CurrentScore = CurrentScore.Add(delta);
        LastChanged = changedAt;

        _history.Add(new ReputationChange(
            changedAt,
            delta,
            oldScore,
            CurrentScore,
            reason
        ));
    }

    /// <summary>
    /// Gets the current reputation standing as a descriptive string
    /// </summary>
    public string GetStanding()
    {
        return CurrentScore.Value switch
        {
            >= 75 => "Exalted",
            >= 50 => "Revered",
            >= 25 => "Honored",
            >= 10 => "Friendly",
            > -10 => "Neutral",
            > -25 => "Unfriendly",
            > -50 => "Hostile",
            > -75 => "Hated",
            _ => "Nemesis"
        };
    }

    /// <summary>
    /// Checks if reputation is at least the specified threshold
    /// </summary>
    public bool IsAtLeast(int threshold) => CurrentScore.Value >= threshold;

    /// <summary>
    /// Checks if reputation is at most the specified threshold
    /// </summary>
    public bool IsAtMost(int threshold) => CurrentScore.Value <= threshold;
}

/// <summary>
/// Record of a reputation change for history tracking
/// </summary>
public record ReputationChange(
    DateTime Timestamp,
    int Delta,
    ReputationScore OldScore,
    ReputationScore NewScore,
    string Reason
);
