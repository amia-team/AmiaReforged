using AmiaReforged.PwEngine.Database.Entities.PlayerHousing;

namespace AmiaReforged.PwEngine.Database;

/// <summary>
/// Repository interface for PLC layout configurations.
/// </summary>
public interface IPlcLayoutRepository
{
    /// <summary>
    /// Gets all layout configurations for a specific property and character.
    /// </summary>
    Task<List<PlcLayoutConfiguration>> GetLayoutsForPropertyAsync(
        Guid propertyId,
        Guid characterId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific layout configuration by ID, including all items.
    /// </summary>
    Task<PlcLayoutConfiguration?> GetLayoutByIdAsync(
        long layoutId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Saves a layout configuration (creates or updates).
    /// </summary>
    Task<PlcLayoutConfiguration> SaveLayoutAsync(
        PlcLayoutConfiguration layout,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a layout configuration and all its items.
    /// </summary>
    Task DeleteLayoutAsync(
        long layoutId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts the number of layouts a character has for a specific property.
    /// </summary>
    Task<int> CountLayoutsForPropertyAsync(
        Guid propertyId,
        Guid characterId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a layout name already exists for this property/character combination.
    /// </summary>
    Task<bool> LayoutNameExistsAsync(
        Guid propertyId,
        Guid characterId,
        string name,
        long? excludeLayoutId = null,
        CancellationToken cancellationToken = default);
}
