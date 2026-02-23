namespace AmiaReforged.PwEngine.Features.Encounters.Models;

/// <summary>
/// Represents the 4-axis chaos state for a region or area.
/// Each axis ranges from 0 (no influence) to 100 (maximum influence).
/// </summary>
public record ChaosState
{
    /// <summary>
    /// Raw creature power scaling. Higher values produce tougher creatures.
    /// </summary>
    public int Danger { get; init; }

    /// <summary>
    /// Undead/fiend/aberration influence on spawn type selection.
    /// </summary>
    public int Corruption { get; init; }

    /// <summary>
    /// Spawn count scaling. Higher values produce more creatures per encounter.
    /// </summary>
    public int Density { get; init; }

    /// <summary>
    /// Random buff/addon chance. Higher values increase bonus effect magnitudes and frequency.
    /// </summary>
    public int Mutation { get; init; }

    /// <summary>
    /// Default state with all axes at zero — no chaos influence.
    /// </summary>
    public static ChaosState Default => new() { Danger = 0, Corruption = 0, Density = 0, Mutation = 0 };
}
