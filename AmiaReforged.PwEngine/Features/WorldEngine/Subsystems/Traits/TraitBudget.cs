namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Traits;

/// <summary>
/// Value object representing a character's trait point budget.
/// Manages the calculation of available trait points based on base points, earned points, and spent points.
/// </summary>
public class TraitBudget
{
    private const int BaseTraitPoints = 2;

    /// <summary>
    /// Base trait points all characters receive (always 2)
    /// </summary>
    public int BasePoints => BaseTraitPoints;

    /// <summary>
    /// Additional points earned from DM events or special unlocks
    /// </summary>
    public int EarnedPoints { get; init; }

    /// <summary>
    /// Points spent on positive traits (or gained from negative traits)
    /// </summary>
    public int SpentPoints { get; init; }

    /// <summary>
    /// Total available points to spend
    /// </summary>
    public int TotalPoints => BasePoints + EarnedPoints;

    /// <summary>
    /// Remaining points after spending
    /// </summary>
    public int AvailablePoints => TotalPoints - SpentPoints;

    /// <summary>
    /// Checks if a trait with the given point cost can be afforded
    /// </summary>
    public bool CanAfford(int pointCost)
    {
        return AvailablePoints >= pointCost;
    }

    /// <summary>
    /// Creates a new budget with additional spent points
    /// </summary>
    public TraitBudget WithSpentPoints(int newSpentPoints)
    {
        return new TraitBudget
        {
            EarnedPoints = EarnedPoints,
            SpentPoints = newSpentPoints
        };
    }

    /// <summary>
    /// Creates a new budget after spending additional points
    /// </summary>
    public TraitBudget AfterSpending(int additionalPoints)
    {
        return new TraitBudget
        {
            EarnedPoints = EarnedPoints,
            SpentPoints = SpentPoints + additionalPoints
        };
    }

    /// <summary>
    /// Creates a new budget with additional earned points
    /// </summary>
    public TraitBudget WithEarnedPoints(int additionalEarnedPoints)
    {
        return new TraitBudget
        {
            EarnedPoints = EarnedPoints + additionalEarnedPoints,
            SpentPoints = SpentPoints
        };
    }

    /// <summary>
    /// Creates a default budget for a new character
    /// </summary>
    public static TraitBudget CreateDefault()
    {
        return new TraitBudget
        {
            EarnedPoints = 0,
            SpentPoints = 0
        };
    }
}
