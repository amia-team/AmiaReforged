namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes.Persistence;

/// <summary>
/// EF Core entity for persisting resource node definitions to the database.
/// Maps to/from the domain <see cref="ResourceNodeData.ResourceNodeDefinition"/> record.
/// </summary>
public class PersistedResourceNodeDefinition
{
    /// <summary>
    /// Unique node tag. Primary key.
    /// </summary>
    public string Tag { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// NWN placeable appearance type constant.
    /// </summary>
    public int PlcAppearance { get; set; }

    /// <summary>
    /// Resource type, stored as a string enum value.
    /// </summary>
    public string Type { get; set; } = "Undefined";

    /// <summary>
    /// Number of times the node can be harvested.
    /// </summary>
    public int Uses { get; set; } = 50;

    /// <summary>
    /// Base harvest rounds required.
    /// </summary>
    public int BaseHarvestRounds { get; set; }

    /// <summary>
    /// Harvest requirement data, stored as JSONB.
    /// </summary>
    public string RequirementJson { get; set; } = "{}";

    /// <summary>
    /// Harvest output definitions, stored as JSONB array.
    /// </summary>
    public string OutputsJson { get; set; } = "[]";

    /// <summary>
    /// Flora-specific properties, stored as JSONB. Null for non-flora types.
    /// </summary>
    public string? FloraPropertiesJson { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
