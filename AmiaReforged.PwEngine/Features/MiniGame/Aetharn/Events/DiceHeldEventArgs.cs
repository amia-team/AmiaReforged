namespace AmiaReforged.PwEngine.Features.MiniGame.Aetharn.Events;

/// <summary>
/// Event args for when a player holds dice for scoring.
/// </summary>
public sealed class DiceHeldEventArgs : EventArgs
{
    /// <summary>
    /// The player key of the player who held dice.
    /// </summary>
    public required string PlayerKey { get; init; }

    /// <summary>
    /// The values of the dice that were just held.
    /// </summary>
    public required IReadOnlyList<int> HeldDiceValues { get; init; }

    /// <summary>
    /// The indices of the dice that were just held.
    /// </summary>
    public required IReadOnlyList<int> HeldDiceIndices { get; init; }

    /// <summary>
    /// Points earned from the held dice.
    /// </summary>
    public required int PointsFromHeld { get; init; }

    /// <summary>
    /// Total accumulated points this turn so far.
    /// </summary>
    public required int TurnAccumulatedPoints { get; init; }

    /// <summary>
    /// Number of dice still available to roll.
    /// </summary>
    public required int RemainingDiceCount { get; init; }

    /// <summary>
    /// The scoring combination type for the held dice.
    /// </summary>
    public required ScoringType ScoringType { get; init; }
}
