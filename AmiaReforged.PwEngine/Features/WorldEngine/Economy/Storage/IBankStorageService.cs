using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Storage.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Storage;

/// <summary>
/// Facade service for bank storage operations. 
/// Hides CQRS implementation details and provides a clean domain API.
/// </summary>
public interface IBankStorageService
{
    /// <summary>
    /// Stores an item in the character's bank storage at the specified coinhouse.
    /// </summary>
    Task<CommandResult> StoreItemAsync(
        CoinhouseTag coinhouseTag,
        Guid characterId,
        string itemName,
        string itemDescription,
        byte[] itemData,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Withdraws an item from storage.
    /// </summary>
    Task<CommandResult> WithdrawItemAsync(
        long storedItemId,
        Guid characterId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Upgrades the storage capacity at the specified coinhouse.
    /// </summary>
    Task<CommandResult> UpgradeStorageCapacityAsync(
        CoinhouseTag coinhouseTag,
        Guid characterId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all stored items for a character at a specific coinhouse.
    /// </summary>
    Task<List<StoredItemDto>> GetStoredItemsAsync(
        CoinhouseTag coinhouseTag,
        Guid characterId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets storage capacity information for a character at a specific coinhouse.
    /// </summary>
    Task<GetStorageCapacityResult> GetStorageCapacityAsync(
        CoinhouseTag coinhouseTag,
        Guid characterId,
        CancellationToken cancellationToken = default);
}
