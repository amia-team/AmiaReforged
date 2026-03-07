namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.Persistence;

/// <summary>
/// EF Core entity for persisting workstation definitions to the database.
/// Maps to/from the domain <see cref="Workstation"/> class.
/// Workstations are global — not scoped to a single industry.
/// </summary>
public class PersistedWorkstationDefinition
{
    /// <summary>
    /// Unique workstation tag. Primary key.
    /// </summary>
    public string Tag { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the workstation.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// NWN placeable blueprint ResRef for in-world placement.
    /// </summary>
    public string? PlaceableResRef { get; set; }

    /// <summary>
    /// Industry tags that can use this workstation, stored as a JSONB array of strings.
    /// </summary>
    public string SupportedIndustriesJson { get; set; } = "[]";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
