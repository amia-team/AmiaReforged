using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;

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
    Task<ItemDefinition?> GetItemDefinitionAsync(string resref, CancellationToken ct = default);

    /// <summary>
    /// Gets an item definition (blueprint) by blueprint tag.
    /// </summary>
    Task<ItemDefinition?> GetItemDefinitionByTagAsync(string tag, CancellationToken ct = default);

    /// <summary>
    /// Gets all item definitions (blueprints).
    /// </summary>
    Task<List<ItemDefinition>> GetAllItemDefinitionsAsync(CancellationToken ct = default);

    /// <summary>
    /// Searches for item definitions (blueprints) by name or tag.
    /// </summary>
    Task<List<ItemDefinition>> SearchItemDefinitionsAsync(
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
    Task<List<ItemDefinition>> GetItemsByCategoryAsync(
        ItemCategory category,
        CancellationToken ct = default);

    /// <summary>
    /// Gets items (blueprints) by tags (e.g., "weapon", "magical", "rare").
    /// </summary>
    Task<List<ItemDefinition>> GetItemsByTagsAsync(
        List<string> tags,
        CancellationToken ct = default);
}

/// <summary>
/// Represents an item definition (WorldEngine-level item blueprint).
/// </summary>
public record ItemDefinition(
    string Resref,
    string Name,
    string Description,
    ItemCategory Category,
    int BaseValue,
    Dictionary<string, object> CustomProperties);

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
