namespace AmiaReforged.PwEngine.Features.MiniGame.Aetharn.Events;

/// <summary>
/// Event args for when a player's turn starts.
/// </summary>
public sealed class TurnStartedEventArgs : EventArgs
{
    /// <summary>
    /// The player key of the player whose turn is starting.
    /// </summary>
    public required string PlayerKey { get; init; }

    /// <summary>
    /// The display name of the player.
    /// </summary>
    public required string PlayerName { get; init; }

    /// <summary>
    /// The turn number (1-based, increments each time play returns to this player).
    /// </summary>
    public required int TurnNumber { get; init; }

    /// <summary>
    /// The player's current total score before this turn.
    /// </summary>
    public required int CurrentScore { get; init; }
}
