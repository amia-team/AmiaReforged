namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Codex.Domain.ValueObjects;

/// <summary>
/// Value object representing a reputation score with a faction.
/// Enforces valid range constraints.
/// </summary>
public readonly record struct ReputationScore
{
    public const int MinReputation = -100;
    public const int MaxReputation = 100;
    public const int Neutral = 0;

    public int Value { get; }

    public ReputationScore(int value)
    {
        if (value < MinReputation || value > MaxReputation)
            throw new ArgumentException(
                $"Reputation must be between {MinReputation} and {MaxReputation}, got {value}",
                nameof(value));

        Value = value;
    }

    /// <summary>
    /// Creates a neutral reputation score (0).
    /// </summary>
    public static ReputationScore CreateNeutral() => new(Neutral);

    /// <summary>
    /// Creates a reputation score from an integer value with validation.
    /// </summary>
    public static ReputationScore Parse(int value) => new(value);

    /// <summary>
    /// Adds a delta to this reputation score, clamping to valid range.
    /// </summary>
    public ReputationScore Add(int delta)
    {
        int newValue = Math.Clamp(Value + delta, MinReputation, MaxReputation);
        return new ReputationScore(newValue);
    }

    /// <summary>
    /// Implicit conversion from ReputationScore to int for backward compatibility.
    /// </summary>
    public static implicit operator int(ReputationScore score) => score.Value;

    /// <summary>
    /// Explicit conversion from int to ReputationScore (requires validation).
    /// </summary>
    public static explicit operator ReputationScore(int value) => new(value);

    public override string ToString() => Value.ToString();
}
