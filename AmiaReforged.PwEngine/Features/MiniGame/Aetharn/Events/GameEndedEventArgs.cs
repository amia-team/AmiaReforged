namespace AmiaReforged.PwEngine.Features.MiniGame.Aetharn.Events;

/// <summary>
/// Describes how a game ended.
/// </summary>
public enum GameEndReason
{
    /// <summary>A player reached the winning score of 5000 points.</summary>
    WinnerReached5000,

    /// <summary>All other players folded or were removed, leaving one player.</summary>
    LastPlayerStanding,

    /// <summary>All players left the game before it concluded.</summary>
    AllPlayersLeft
}

/// <summary>
/// Event args for when a game ends.
/// </summary>
public sealed class GameEndedEventArgs : EventArgs
{
    /// <summary>
    /// The unique identifier for this game instance.
    /// </summary>
    public required Guid GameId { get; init; }

    /// <summary>
    /// The winning player, if any.
    /// </summary>
    public PlayerInfo? Winner { get; init; }

    /// <summary>
    /// The reason the game ended.
    /// </summary>
    public required GameEndReason EndReason { get; init; }

    /// <summary>
    /// Final standings of all players, ordered by score descending.
    /// </summary>
    public required IReadOnlyList<PlayerInfo> FinalStandings { get; init; }

    /// <summary>
    /// Total number of rounds played.
    /// </summary>
    public required int TotalRounds { get; init; }
}
