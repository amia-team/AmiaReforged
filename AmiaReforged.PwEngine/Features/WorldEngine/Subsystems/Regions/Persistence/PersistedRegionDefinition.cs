namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Regions.Persistence;

/// <summary>
/// EF Core entity for persisting region definitions to the database.
/// Maps to/from the domain <see cref="RegionDefinition"/> class.
/// The nested Areas (with Environment, POIs, etc.) are stored as a JSONB column.
/// </summary>
public class PersistedRegionDefinition
{
    /// <summary>
    /// Unique region tag. Primary key.
    /// </summary>
    public string Tag { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the region.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Default chaos state for this region, stored as JSONB. Null if not set.
    /// </summary>
    public string? DefaultChaosJson { get; set; }

    /// <summary>
    /// All area definitions for this region, stored as a JSONB array.
    /// Each area contains ResRef, DefinitionTags, Environment, PlacesOfInterest, and LinkedSettlement.
    /// </summary>
    public string AreasJson { get; set; } = "[]";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
