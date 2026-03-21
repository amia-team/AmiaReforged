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
    /// Items/resources needed to craft this recipe
    /// </summary>
    public required List<Ingredient> Ingredients { get; init; }

    /// <summary>
    /// Items produced by this recipe
    /// </summary>
    public required List<Product> Products { get; init; }

    /// <summary>
    /// Time in rounds to complete crafting (optional, may vary by implementation).
    /// Progress iterates in rounds, not real-time seconds.
    /// </summary>
    public int? CraftingTimeRounds { get; init; }

    /// <summary>
    /// Progression points awarded toward the next knowledge point on successful crafting
    /// </summary>
    public int ProgressionPointsAwarded { get; init; }

    /// <summary>
    /// Proficiency XP awarded toward the industry proficiency level on successful crafting.
    /// </summary>
    public int ProficiencyXpAwarded { get; init; }

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
    /// Tool requirements the player must satisfy (tools are not consumed) to craft this recipe.
    /// Each requirement can match by exact item tag, by tool form, or by form + material.
    /// </summary>
    public List<ToolRequirement> RequiredTools { get; init; } = [];
}

