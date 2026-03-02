namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.Persistence;

/// <summary>
/// EF Core entity for persisting industry definitions to the database.
/// Maps to/from the domain <see cref="Industry"/> class.
/// Knowledge and Recipe lists are stored as JSONB columns.
/// </summary>
public class PersistedIndustryDefinition
{
    /// <summary>
    /// Unique industry tag. Primary key.
    /// </summary>
    public string Tag { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the industry.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// All knowledge articles for this industry, stored as a JSONB array.
    /// </summary>
    public string KnowledgeJson { get; set; } = "[]";

    /// <summary>
    /// All recipes for this industry, stored as a JSONB array.
    /// </summary>
    public string RecipesJson { get; set; } = "[]";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
