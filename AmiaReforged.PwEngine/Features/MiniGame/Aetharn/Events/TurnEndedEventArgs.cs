namespace AmiaReforged.PwEngine.Features.MiniGame.Aetharn.Events;

/// <summary>
/// Event args for when a player's turn ends.
/// </summary>
public sealed class TurnEndedEventArgs : EventArgs
{
    /// <summary>
    /// The player key of the player whose turn ended.
    /// </summary>
    public required string PlayerKey { get; init; }

    /// <summary>
    /// The player key of the next player.
    /// </summary>
    public required string NextPlayerKey { get; init; }

    /// <summary>
    /// The display name of the next player.
    /// </summary>
    public required string NextPlayerName { get; init; }

    /// <summary>
    /// True if the turn ended due to a bust.
    /// </summary>
    public required bool WasBust { get; init; }

    /// <summary>
    /// True if the turn ended due to timeout.
    /// </summary>
    public required bool WasTimeout { get; init; }

    /// <summary>
    /// Points banked this turn (0 if bust or timeout).
    /// </summary>
    public required int PointsBanked { get; init; }

    /// <summary>
    /// The player's new total score after banking.
    /// </summary>
    public required int NewTotalScore { get; init; }

    /// <summary>
    /// The number of rolls made during this turn.
    /// </summary>
    public required int RollCount { get; init; }
}
