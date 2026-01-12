namespace AmiaReforged.PwEngine.Features.MiniGame.Aetharn.Events;

/// <summary>
/// Event args for when a game starts.
/// </summary>
public sealed class GameStartedEventArgs : EventArgs
{
    /// <summary>
    /// The unique identifier for this game instance.
    /// </summary>
    public required Guid GameId { get; init; }

    /// <summary>
    /// The players in the game, in turn order.
    /// </summary>
    public required IReadOnlyList<PlayerInfo> Players { get; init; }

    /// <summary>
    /// The player who will go first.
    /// </summary>
    public required PlayerInfo FirstPlayer { get; init; }
}

/// <summary>
/// Basic player information for events.
/// </summary>
public sealed record PlayerInfo(string PlayerKey, string PlayerName, int Score);
