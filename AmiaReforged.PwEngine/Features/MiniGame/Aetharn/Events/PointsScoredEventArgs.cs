namespace AmiaReforged.PwEngine.Features.MiniGame.Aetharn.Events;

/// <summary>
/// Event args for when points are scored and banked.
/// </summary>
public sealed class PointsScoredEventArgs : EventArgs
{
    /// <summary>
    /// The player key of the player who scored.
    /// </summary>
    public required string PlayerKey { get; init; }

    /// <summary>
    /// The display name of the player.
    /// </summary>
    public required string PlayerName { get; init; }

    /// <summary>
    /// Points scored this turn.
    /// </summary>
    public required int PointsThisTurn { get; init; }

    /// <summary>
    /// The player's new total score after banking.
    /// </summary>
    public required int TotalScore { get; init; }

    /// <summary>
    /// The player's previous score before this turn.
    /// </summary>
    public required int PreviousScore { get; init; }
}
