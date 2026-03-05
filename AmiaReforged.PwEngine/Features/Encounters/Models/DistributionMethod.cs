namespace AmiaReforged.PwEngine.Features.Encounters.Models;

/// <summary>
/// Controls how and when creatures from a <see cref="SpawnGroup"/> are placed in the world.
/// Each group can have its own distribution method, allowing mixed behavior within a single area.
/// </summary>
public enum DistributionMethod
{
    /// <summary>
    /// Default behaviour. All creatures cluster at one random <c>ds_spwn</c> waypoint
    /// inside the spawn trigger. Requires a <c>db_spawntrigger</c>.
    /// </summary>
    None = 0,

    /// <summary>
    /// Spawns when a player enters the area (no trigger required). Creatures are placed
    /// at a random <c>ds_spwn</c> waypoint anywhere in the area.
    /// </summary>
    OnAreaEnter = 1,

    /// <summary>
    /// Trigger-based, but distributes creatures round-robin across all <c>ds_spwn</c>
    /// waypoints inside the trigger instead of clustering on one.
    /// </summary>
    EvenlyDistributed = 2,

    /// <summary>
    /// Trigger-based. Spawns creatures at or near the entering player's location
    /// instead of at waypoints — ideal for ambush encounters.
    /// </summary>
    PlayerProximity = 3,

    /// <summary>
    /// Same placement as <see cref="EvenlyDistributed"/>, but each creature receives
    /// <c>ActionRandomWalk()</c> after spawn so they patrol and wander the area.
    /// </summary>
    Roaming = 4
}
