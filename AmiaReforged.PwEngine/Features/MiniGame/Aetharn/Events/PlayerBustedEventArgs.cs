namespace AmiaReforged.PwEngine.Features.MiniGame.Aetharn.Events;

/// <summary>
/// Event args for when a player busts (rolls no scoring dice).
/// </summary>
public sealed class PlayerBustedEventArgs : EventArgs
{
    /// <summary>
    /// The player key of the player who busted.
    /// </summary>
    public required string PlayerKey { get; init; }

    /// <summary>
    /// The display name of the player.
    /// </summary>
    public required string PlayerName { get; init; }

    /// <summary>
    /// Points that were accumulated this turn but lost due to bust.
    /// </summary>
    public required int LostPoints { get; init; }

    /// <summary>
    /// The dice values that caused the bust.
    /// </summary>
    public required IReadOnlyList<int> BustDiceValues { get; init; }

    /// <summary>
    /// The roll number when the bust occurred.
    /// </summary>
    public required int RollNumber { get; init; }
}
