using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;

/// <summary>
/// A recipe template defines a pattern for crafting that matches items by material category
/// and form (JobSystemItemType) rather than by specific item tags. When expanded, a single
/// template can generate many concrete <see cref="Recipe"/> instances — one per valid
/// material/item combination found in the item blueprint registry.
/// <para>
/// Example: A "Plank Making" template with ingredient {Category: Wood, Form: ResourceLog}
/// and product {Form: ResourcePlank} will auto-expand to one recipe per wood type
/// (oak_log → oak_plank, pine_log → pine_plank, etc.).
/// </para>
/// </summary>
public class RecipeTemplate
{
    /// <summary>
    /// Unique identifier for this template (e.g., "plank_making"). Primary key.
    /// </summary>
    public required string Tag { get; init; }

    /// <summary>
    /// Display name shown to players (e.g., "Plank Making").
    /// </summary>
    public required string Name { get; init; }

    /// <summary>
    /// Optional description of what this template produces.
    /// </summary>
    public string? Description { get; init; }

    /// <summary>
    /// The industry this template belongs to. Expanded recipes inherit this tag.
    /// </summary>
    public required IndustryTag IndustryTag { get; init; }

    /// <summary>
    /// Knowledge tags required to use recipes generated from this template.
    /// </summary>
    public List<string> RequiredKnowledge { get; init; } = [];

    /// <summary>
    /// Template ingredient slots. Each slot matches items by material category and form.
    /// </summary>
    public required List<TemplateIngredient> Ingredients { get; init; }

    /// <summary>
    /// Template product slots. Each slot defines an output form and which ingredient
    /// slot provides the material for the output.
    /// </summary>
    public required List<TemplateProduct> Products { get; init; }

    /// <summary>
    /// Time in seconds to complete crafting (inherited by expanded recipes).
    /// </summary>
    public int? CraftingTimeSeconds { get; init; }

    /// <summary>
    /// Knowledge points awarded on successful crafting (inherited by expanded recipes).
    /// </summary>
    public int KnowledgePointsAwarded { get; init; }

    /// <summary>
    /// The workstation type required (inherited by expanded recipes).
    /// Null means no workstation is required.
    /// </summary>
    public WorkstationTag? RequiredWorkstation { get; init; }

    /// <summary>
    /// Item tags for tools the player must possess (but are not consumed) to craft.
    /// Inherited by expanded recipes.
    /// </summary>
    public List<string> RequiredTools { get; init; } = [];

    /// <summary>
    /// Optional additional metadata (inherited by expanded recipes).
    /// </summary>
    public Dictionary<string, object> Metadata { get; init; } = new();
}
