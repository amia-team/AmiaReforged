namespace AmiaReforged.PwEngine.Features.Encounters.Models;

/// <summary>
/// Types of conditions that control when a spawn group is eligible to activate.
/// </summary>
public enum SpawnConditionType
{
    /// <summary>
    /// Checks game time-of-day against a time window (e.g., "06:00-18:00").
    /// </summary>
    TimeOfDay = 0,

    /// <summary>
    /// Checks a chaos axis against a threshold (e.g., "Danger>=50").
    /// </summary>
    ChaosThreshold = 1,

    /// <summary>
    /// Minimum number of players in the party.
    /// </summary>
    MinPlayerCount = 2,

    /// <summary>
    /// Maximum number of players in the party.
    /// </summary>
    MaxPlayerCount = 3,

    /// <summary>
    /// Matches against a region tag.
    /// </summary>
    RegionTag = 4,

    /// <summary>
    /// Custom condition for future extensibility. Always evaluates to true.
    /// </summary>
    Custom = 99
}
