namespace AmiaReforged.PwEngine.Features.MiniGame.Aetharn.Events;

/// <summary>
/// Event args for when dice are rolled during a turn.
/// </summary>
public sealed class DiceRolledEventArgs : EventArgs
{
    /// <summary>
    /// The player key of the player who rolled.
    /// </summary>
    public required string PlayerKey { get; init; }

    /// <summary>
    /// The values of all dice after the roll.
    /// </summary>
    public required IReadOnlyList<int> DiceValues { get; init; }

    /// <summary>
    /// Indices of dice that were held (not rolled).
    /// </summary>
    public required IReadOnlyList<int> HeldDiceIndices { get; init; }

    /// <summary>
    /// Indices of dice that were just rolled.
    /// </summary>
    public required IReadOnlyList<int> RolledDiceIndices { get; init; }

    /// <summary>
    /// The scoring result for the rolled dice.
    /// </summary>
    public required ScoringResult ScoringResult { get; init; }

    /// <summary>
    /// True if the roll resulted in a bust (no scoring dice among those rolled).
    /// </summary>
    public bool IsBust => ScoringResult.IsBust;

    /// <summary>
    /// The roll number within this turn (1 = initial roll, 2+ = subsequent rolls).
    /// </summary>
    public required int RollNumber { get; init; }
}
