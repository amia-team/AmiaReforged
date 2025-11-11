using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Storage;

/// <summary>
/// Service for managing personal item storage at banks.
/// Players can store items at their settlement's bank for a fee.
/// Storage capacity increases in 10-slot increments with escalating costs.
/// </summary>
public interface IPersonalStorageService
{
    /// <summary>
    /// Gets or creates a personal storage warehouse for a character at a specific bank.
    /// Initial capacity is 10 slots.
    /// </summary>
    Task<Database.Entities.Storage> GetOrCreatePersonalStorageAsync(
        CoinhouseTag coinhouseTag,
        Guid characterId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Stores an item in the character's personal storage at the specified bank.
    /// </summary>
    /// <returns>Success result with message, or error if storage is full.</returns>
    Task<StorageResult> StoreItemAsync(
        CoinhouseTag coinhouseTag,
        Guid characterId,
        string itemName,
        byte[] itemData,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all stored items for a character at a specific bank.
    /// </summary>
    Task<List<Database.Entities.StoredItem>> GetStoredItemsAsync(
        CoinhouseTag coinhouseTag,
        Guid characterId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Withdraws an item from storage (removes it from the database).
    /// </summary>
    Task<Database.Entities.StoredItem?> WithdrawItemAsync(
        long storedItemId,
        Guid characterId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the current capacity and usage for a character's storage at a bank.
    /// </summary>
    Task<StorageCapacityInfo> GetStorageCapacityAsync(
        CoinhouseTag coinhouseTag,
        Guid characterId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Upgrades storage capacity by 10 slots.
    /// Cost increases by 50k for first upgrade, then +100k per subsequent upgrade.
    /// Maximum capacity is 100 slots.
    /// </summary>
    /// <returns>Success result with new capacity, or error if max reached or insufficient funds.</returns>
    Task<StorageUpgradeResult> UpgradeStorageCapacityAsync(
        CoinhouseTag coinhouseTag,
        Guid characterId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates the cost to upgrade storage from current capacity to next tier.
    /// </summary>
    int CalculateUpgradeCost(int currentCapacity);
}

/// <summary>
/// Result of a storage operation.
/// </summary>
public record StorageResult(bool Success, string Message);

/// <summary>
/// Result of a storage upgrade operation.
/// </summary>
public record StorageUpgradeResult(bool Success, string Message, int NewCapacity, int Cost);

/// <summary>
/// Information about storage capacity and usage.
/// </summary>
public record StorageCapacityInfo(int Capacity, int UsedSlots, int AvailableSlots);
