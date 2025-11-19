using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.ItemData;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems;

/// <summary>
/// Provides access to item-related operations including item definitions (blueprints) and properties.
/// </summary>
public interface IItemSubsystem
{
    // === Item Definition Management ===
    // ItemDefinition represents a concrete, reusable item blueprint that can be referenced by tag or resref.

    /// <summary>
    /// Gets an item definition (blueprint) by resref.
    /// </summary>
    Task<ItemBlueprint?> GetItemDefinitionAsync(string resref, CancellationToken ct = default);

    /// <summary>
    /// Gets an item definition (blueprint) by blueprint tag.
    /// </summary>
    Task<ItemBlueprint?> GetItemDefinitionByTagAsync(string tag, CancellationToken ct = default);

    /// <summary>
    /// Gets all item definitions (blueprints).
    /// </summary>
    Task<List<ItemBlueprint>> GetAllItemDefinitionsAsync(CancellationToken ct = default);

    /// <summary>
    /// Searches for item definitions (blueprints) by name or tag.
    /// </summary>
    Task<List<ItemBlueprint>> SearchItemDefinitionsAsync(
        string searchTerm,
        CancellationToken ct = default);

    // === Item Properties ===

    /// <summary>
    /// Gets custom properties for an item definition.
    /// </summary>
    Task<Dictionary<string, object>> GetItemPropertiesAsync(
        string resref,
        CancellationToken ct = default);

    /// <summary>
    /// Updates custom properties for an item definition.
    /// </summary>
    Task<CommandResult> UpdateItemPropertiesAsync(
        string resref,
        Dictionary<string, object> properties,
        CancellationToken ct = default);

    // === Item Categories ===

    /// <summary>
    /// Gets items (blueprints) by category.
    /// </summary>
    Task<List<ItemBlueprint>> GetItemsByCategoryAsync(
        ItemCategory category,
        CancellationToken ct = default);

    /// <summary>
    /// Gets items (blueprints) by tags (e.g., "weapon", "magical", "rare").
    /// </summary>
    Task<List<ItemBlueprint>> GetItemsByTagsAsync(
        List<string> tags,
        CancellationToken ct = default);
}

/// <summary>
/// Categories of items.
/// </summary>
public enum ItemCategory
{
    Weapon,
    Armor,
    Potion,
    Scroll,
    Wand,
    Miscellaneous,
    Crafting,
    Resource,
    Container,
    Book,
    Food,
    Jewelry,
    Clothing
}
