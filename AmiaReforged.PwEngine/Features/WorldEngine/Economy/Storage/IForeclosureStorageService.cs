using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Storage;

/// <summary>
/// Service for managing foreclosed storage at coinhouses.
/// When properties are evicted, tenant belongings are moved to foreclosed storage
/// at the settlement's coinhouse where they can be reclaimed.
/// </summary>
public interface IForeclosureStorageService
{
    /// <summary>
    /// Gets or creates the foreclosed storage for a specific coinhouse.
    /// </summary>
    /// <param name="coinhouseTag">The tag identifying the coinhouse.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The storage entity for foreclosed items at this coinhouse.</returns>
    Task<Database.Entities.Storage> GetOrCreateForeclosureStorageAsync(
        CoinhouseTag coinhouseTag,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds an item to foreclosed storage for a specific player at a coinhouse.
    /// </summary>
    /// <param name="coinhouseTag">The coinhouse where the item should be stored.</param>
    /// <param name="characterId">The GUID of the character who owns the item.</param>
    /// <param name="itemData">Serialized item data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task AddForeclosedItemAsync(
        CoinhouseTag coinhouseTag,
        Guid characterId,
        byte[] itemData,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all foreclosed items for a character at a specific coinhouse.
    /// </summary>
    /// <param name="coinhouseTag">The coinhouse to query.</param>
    /// <param name="characterId">The character's GUID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>List of stored items belonging to the character.</returns>
    Task<List<StoredItem>> GetForeclosedItemsAsync(
        CoinhouseTag coinhouseTag,
        Guid characterId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a character has any foreclosed items at a coinhouse.
    /// </summary>
    /// <param name="coinhouseTag">The coinhouse to check.</param>
    /// <param name="characterId">The character's GUID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>True if the character has foreclosed items at this coinhouse.</returns>
    Task<bool> HasForeclosedItemsAsync(
        CoinhouseTag coinhouseTag,
        Guid characterId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes an item from foreclosed storage (when claimed by the player).
    /// </summary>
    /// <param name="storedItemId">The ID of the stored item to remove.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task RemoveForeclosedItemAsync(
        long storedItemId,
        CancellationToken cancellationToken = default);
}
