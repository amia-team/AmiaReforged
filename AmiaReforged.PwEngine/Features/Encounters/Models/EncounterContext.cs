namespace AmiaReforged.PwEngine.Features.Encounters.Models;

/// <summary>
/// Runtime context passed to the condition evaluator and spawn services.
/// Contains all information needed to decide which spawn group to select
/// and how to configure the spawned creatures.
/// </summary>
public class EncounterContext
{
    /// <summary>
    /// The resref of the area where the encounter is triggered.
    /// </summary>
    public required string AreaResRef { get; init; }

    /// <summary>
    /// Number of party members in the same area as the triggering player.
    /// </summary>
    public int PartySize { get; init; }

    /// <summary>
    /// Current NWN game time (hours and minutes).
    /// </summary>
    public TimeSpan GameTime { get; init; }

    /// <summary>
    /// The chaos state for this area's region. Null if unavailable.
    /// </summary>
    public ChaosState Chaos { get; init; } = ChaosState.Default;

    /// <summary>
    /// The region tag for the area, if known.
    /// </summary>
    public string? RegionTag { get; init; }
}
