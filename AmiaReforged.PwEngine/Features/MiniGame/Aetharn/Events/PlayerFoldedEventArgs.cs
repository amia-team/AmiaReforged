namespace AmiaReforged.PwEngine.Features.MiniGame.Aetharn.Events;

/// <summary>
/// Event args for when a player folds (voluntarily leaves the game).
/// </summary>
public sealed class PlayerFoldedEventArgs : EventArgs
{
    /// <summary>
    /// The player key of the player who folded.
    /// </summary>
    public required string PlayerKey { get; init; }

    /// <summary>
    /// The display name of the player.
    /// </summary>
    public required string PlayerName { get; init; }

    /// <summary>
    /// The player's final score when they folded.
    /// </summary>
    public required int FinalScore { get; init; }

    /// <summary>
    /// Number of active players remaining after this fold.
    /// </summary>
    public required int RemainingPlayerCount { get; init; }
}
