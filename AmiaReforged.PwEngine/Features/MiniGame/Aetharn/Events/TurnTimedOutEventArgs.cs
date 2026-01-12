namespace AmiaReforged.PwEngine.Features.MiniGame.Aetharn.Events;

/// <summary>
/// Event args for when a player's turn times out.
/// </summary>
public sealed class TurnTimedOutEventArgs : EventArgs
{
    /// <summary>
    /// The player key of the player whose turn timed out.
    /// </summary>
    public required string PlayerKey { get; init; }

    /// <summary>
    /// The display name of the player.
    /// </summary>
    public required string PlayerName { get; init; }

    /// <summary>
    /// Points that were accumulated this turn but lost due to timeout.
    /// </summary>
    public required int LostPoints { get; init; }

    /// <summary>
    /// The current state of dice when timeout occurred.
    /// </summary>
    public required IReadOnlyList<int> DiceValues { get; init; }

    /// <summary>
    /// The number of rolls made before timeout.
    /// </summary>
    public required int RollCount { get; init; }
}
