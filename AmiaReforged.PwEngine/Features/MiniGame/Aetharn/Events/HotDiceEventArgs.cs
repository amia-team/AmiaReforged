namespace AmiaReforged.PwEngine.Features.MiniGame.Aetharn.Events;

/// <summary>
/// Event args for when a player achieves Hot Dice (all dice scored).
/// </summary>
public sealed class HotDiceEventArgs : EventArgs
{
    /// <summary>
    /// The player key of the player who achieved Hot Dice.
    /// </summary>
    public required string PlayerKey { get; init; }

    /// <summary>
    /// The display name of the player.
    /// </summary>
    public required string PlayerName { get; init; }

    /// <summary>
    /// Total points accumulated so far this turn.
    /// </summary>
    public required int AccumulatedPoints { get; init; }

    /// <summary>
    /// Whether this is the player's first Hot Dice this turn.
    /// If false, they cannot use it (only one Hot Dice per turn).
    /// </summary>
    public required bool CanUseHotDice { get; init; }

    /// <summary>
    /// The dice values that completed the Hot Dice.
    /// </summary>
    public required IReadOnlyList<int> DiceValues { get; init; }

    /// <summary>
    /// The scoring combination that completed the Hot Dice.
    /// </summary>
    public required ScoringType ScoringType { get; init; }
}
