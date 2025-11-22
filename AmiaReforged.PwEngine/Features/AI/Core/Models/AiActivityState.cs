namespace AmiaReforged.PwEngine.Features.AI.Core.Models;

/// <summary>
/// Tracks activity state of a creature for performance optimization.
/// Inactive creatures can skip expensive AI calculations.
/// Based on L_INACTIVE tracking from ds_ai_heartbeat.nss (lines 19-46).
/// </summary>
public class AiActivityState
{
    /// <summary>
    /// Last time this creature performed an AI action.
    /// </summary>
    public DateTime LastActivityTime { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Number of consecutive heartbeats with no action performed.
    /// After 5 heartbeats, creature enters sleep mode.
    /// </summary>
    public int InactiveHeartbeats { get; set; }

    /// <summary>
    /// Whether the creature is in sleep mode (inactive for >5 heartbeats).
    /// </summary>
    public bool IsSleeping => InactiveHeartbeats > 5;

    /// <summary>
    /// Whether the creature should be warned to DMs (inactive for 10+ minutes).
    /// </summary>
    public bool ShouldWarnDm => InactiveHeartbeats >= 100;
}

