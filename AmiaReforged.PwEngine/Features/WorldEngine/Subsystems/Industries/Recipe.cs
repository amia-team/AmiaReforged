using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;

/// <summary>
/// Represents a crafting recipe/reaction - a process that transforms ingredients into products
/// </summary>
public class Recipe
{
    /// <summary>
    /// Unique identifier for this recipe
    /// </summary>
    public required RecipeId RecipeId { get; init; }

    /// <summary>
    /// Display name of the recipe
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Description of what this recipe creates
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// The industry this recipe belongs to
    /// </summary>
    public required IndustryTag IndustryTag { get; init; }

    /// <summary>
    /// Knowledge tags required to use this recipe
    /// </summary>
    public List<string> RequiredKnowledge { get; init; } = [];

    /// <summary>
    /// Minimum proficiency level required
    /// </summary>
    public required ProficiencyLevel RequiredProficiency { get; init; }

    /// <summary>
    /// Items/resources needed to craft this recipe
    /// </summary>
    public required List<Ingredient> Ingredients { get; init; }

    /// <summary>
    /// Items produced by this recipe
    /// </summary>
    public required List<Product> Products { get; init; }

    /// <summary>
    /// Time in seconds to complete crafting (optional, may vary by implementation)
    /// </summary>
    public int? CraftingTimeSeconds { get; init; }

    /// <summary>
    /// Knowledge points awarded on successful crafting
    /// </summary>
    public int KnowledgePointsAwarded { get; init; }

    /// <summary>
    /// Optional: Additional metadata for industry-specific logic
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();

    /// <summary>
    /// The workstation type required to craft this recipe.
    /// Null means no workstation is required (portable / instant craft).
    /// References a global <see cref="Workstation"/> by tag.
    /// </summary>
    public WorkstationTag? RequiredWorkstation { get; init; }

    /// <summary>
    /// Item tags for tools the player must possess (but are not consumed) to craft this recipe.
    /// Example: ["smiths_hammer", "tongs"]
    /// </summary>
    public List<string> RequiredTools { get; init; } = [];
}

