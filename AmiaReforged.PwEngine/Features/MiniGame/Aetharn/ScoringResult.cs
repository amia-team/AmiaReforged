namespace AmiaReforged.PwEngine.Features.MiniGame.Aetharn;

/// <summary>
/// Describes the type of scoring combination found in a dice roll.
/// </summary>
public enum ScoringType
{
    /// <summary>No scoring combination found (bust).</summary>
    None,
    
    /// <summary>Individual 1s and/or 5s only.</summary>
    Singles,
    
    /// <summary>Three dice of the same face value.</summary>
    ThreeOfAKind,
    
    /// <summary>Four dice of the same face value.</summary>
    FourOfAKind,
    
    /// <summary>Five dice of the same face value.</summary>
    FiveOfAKind,
    
    /// <summary>Six dice of the same face value.</summary>
    SixOfAKind,
    
    /// <summary>A straight: 1-2-3-4-5-6.</summary>
    Straight,
    
    /// <summary>Three pairs of dice.</summary>
    ThreePairs
}

/// <summary>
/// Immutable result of evaluating a set of dice for scoring.
/// Contains the points earned, which dice contributed to scoring, and the combination type.
/// </summary>
public sealed class ScoringResult
{
    /// <summary>
    /// A result representing a bust (no scoring dice).
    /// </summary>
    public static readonly ScoringResult Bust = new(
        points: 0,
        scoringDiceIndices: [],
        nonScoringDiceIndices: [],
        type: ScoringType.None
    );

    /// <summary>
    /// Total points from this scoring evaluation.
    /// </summary>
    public int Points { get; }

    /// <summary>
    /// Indices of dice that contributed to scoring.
    /// </summary>
    public IReadOnlyList<int> ScoringDiceIndices { get; }

    /// <summary>
    /// Indices of dice that did not contribute to scoring.
    /// </summary>
    public IReadOnlyList<int> NonScoringDiceIndices { get; }

    /// <summary>
    /// The primary scoring combination type found.
    /// </summary>
    public ScoringType Type { get; }

    /// <summary>
    /// True if no dice scored (points = 0 and no scoring dice).
    /// </summary>
    public bool IsBust => Points == 0 && ScoringDiceIndices.Count == 0;

    /// <summary>
    /// True if all dice scored (Hot Dice - player may roll all 6 again).
    /// </summary>
    public bool IsHotDice => NonScoringDiceIndices.Count == 0 && ScoringDiceIndices.Count > 0;

    public ScoringResult(
        int points,
        IReadOnlyList<int> scoringDiceIndices,
        IReadOnlyList<int> nonScoringDiceIndices,
        ScoringType type)
    {
        Points = points;
        ScoringDiceIndices = scoringDiceIndices;
        NonScoringDiceIndices = nonScoringDiceIndices;
        Type = type;
    }

    /// <summary>
    /// Creates a new ScoringResult with the specified values.
    /// </summary>
    public static ScoringResult Create(
        int points,
        int[] scoringDiceIndices,
        int[] nonScoringDiceIndices,
        ScoringType type)
    {
        return new ScoringResult(points, scoringDiceIndices, nonScoringDiceIndices, type);
    }
}
