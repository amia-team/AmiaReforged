namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.Persistence;

/// <summary>
/// EF Core entity for persisting recipe template definitions to the database.
/// Maps to/from the domain <see cref="RecipeTemplate"/> class.
/// Ingredients, products, knowledge, and metadata are stored as JSONB columns.
/// </summary>
public class PersistedRecipeTemplate
{
    /// <summary>
    /// Unique template tag. Primary key.
    /// </summary>
    public string Tag { get; set; } = string.Empty;

    /// <summary>
    /// Display name of the recipe template.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Optional description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// The industry tag this template belongs to.
    /// </summary>
    public string IndustryTag { get; set; } = string.Empty;

    /// <summary>
    /// Required knowledge tags, stored as a JSONB array of strings.
    /// </summary>
    public string RequiredKnowledgeJson { get; set; } = "[]";

    /// <summary>
    /// Required proficiency level as a string enum name.
    /// </summary>
    public string RequiredProficiency { get; set; } = "Novice";

    /// <summary>
    /// Template ingredient definitions, stored as JSONB.
    /// </summary>
    public string IngredientsJson { get; set; } = "[]";

    /// <summary>
    /// Template product definitions, stored as JSONB.
    /// </summary>
    public string ProductsJson { get; set; } = "[]";

    /// <summary>
    /// Optional crafting time in seconds.
    /// </summary>
    public int? CraftingTimeSeconds { get; set; }

    /// <summary>
    /// Knowledge points awarded on successful crafting.
    /// </summary>
    public int KnowledgePointsAwarded { get; set; }

    /// <summary>
    /// Optional workstation tag required.
    /// </summary>
    public string? RequiredWorkstation { get; set; }

    /// <summary>
    /// Optional process graph link.
    /// </summary>
    public string? ProcessId { get; set; }

    /// <summary>
    /// Additional metadata, stored as JSONB.
    /// </summary>
    public string MetadataJson { get; set; } = "{}";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
