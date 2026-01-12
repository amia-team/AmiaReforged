namespace AmiaReforged.PwEngine.Features.MiniGame.Aetharn;

/// <summary>
/// Centralized constants for the Aetharn dice game.
/// All magic literals should be defined here to ensure consistency and easy tuning.
/// </summary>
public static class AetharnConstants
{
    #region Placeable Tags

    /// <summary>
    /// Tag for the trigger that defines an Aetharn game area.
    /// </summary>
    public const string TriggerTag = "game_aetharn";

    /// <summary>
    /// Tag for chair placeables that players sit in to join the game.
    /// </summary>
    public const string ChairTag = "aetharn_player_seat";

    /// <summary>
    /// Tag for observer placeables that allow spectating.
    /// </summary>
    public const string ObserverTag = "game_aetharn_observe";

    #endregion

    #region Game Configuration

    /// <summary>
    /// Score required to win the game.
    /// </summary>
    public const int WinningScore = 5000;

    /// <summary>
    /// Maximum number of players per table.
    /// </summary>
    public const int MaxPlayers = 6;

    /// <summary>
    /// Minimum number of players required to start a game.
    /// </summary>
    public const int MinPlayers = 2;

    /// <summary>
    /// Number of dice used in the game.
    /// </summary>
    public const int DiceCount = 6;

    #endregion

    #region Timers

    /// <summary>
    /// Time limit for each turn in seconds.
    /// </summary>
    public const int TurnTimeLimitSeconds = 30;

    /// <summary>
    /// Grace period for absent players to return before being removed, in seconds.
    /// </summary>
    public const int AbsenceGracePeriodSeconds = 60;

    /// <summary>
    /// Intervals (in seconds remaining) at which to warn about grace period expiring.
    /// </summary>
    public static readonly int[] GraceWarningIntervals = [45, 30, 15, 10, 5];

    #endregion

    #region Scoring Values

    /// <summary>
    /// Points for a single 1.
    /// </summary>
    public const int SingleOnePoints = 100;

    /// <summary>
    /// Points for a single 5.
    /// </summary>
    public const int SingleFivePoints = 50;

    /// <summary>
    /// Points for three 1s (special case, not face × 100).
    /// </summary>
    public const int ThreeOnesPoints = 1000;

    /// <summary>
    /// Multiplier for three of a kind (face value × this).
    /// </summary>
    public const int ThreeOfAKindMultiplier = 100;

    /// <summary>
    /// Points for a straight (1-2-3-4-5-6).
    /// </summary>
    public const int StraightPoints = 1500;

    /// <summary>
    /// Points for three pairs.
    /// </summary>
    public const int ThreePairsPoints = 1500;

    /// <summary>
    /// Multiplier applied for each additional die beyond three of a kind.
    /// Four of a kind = 2×, Five of a kind = 4×, Six of a kind = 8×.
    /// </summary>
    public const int AdditionalDieMultiplier = 2;

    #endregion
}
