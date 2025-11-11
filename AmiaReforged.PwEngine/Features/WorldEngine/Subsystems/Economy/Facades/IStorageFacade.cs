using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Storage.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Storage.Queries;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Facades;

/// <summary>
/// Gateway for storage operations.
/// Provides access to item storage, withdrawal, and capacity management.
/// </summary>
public interface IStorageFacade
{
    // === Item Storage Operations ===

    /// <summary>
    /// Stores an item in a storage location.
    /// </summary>
    Task<CommandResult> StoreItemAsync(StoreItemCommand command, CancellationToken ct = default);

    /// <summary>
    /// Withdraws an item from a storage location.
    /// </summary>
    Task<CommandResult> WithdrawItemAsync(WithdrawItemCommand command, CancellationToken ct = default);

    /// <summary>
    /// Gets all items stored at a specific location.
    /// </summary>
    Task<List<StoredItemDto>> GetStoredItemsAsync(GetStoredItemsQuery query, CancellationToken ct = default);

    // === Capacity Management ===

    /// <summary>
    /// Gets the storage capacity for a specific location.
    /// </summary>
    Task<GetStorageCapacityResult> GetStorageCapacityAsync(GetStorageCapacityQuery query, CancellationToken ct = default);

    /// <summary>
    /// Upgrades the storage capacity for a location.
    /// </summary>
    Task<CommandResult> UpgradeStorageCapacityAsync(UpgradeStorageCapacityCommand command, CancellationToken ct = default);

    // === Convenience Overloads ===
    // These methods provide simplified APIs that internally create command/query objects.
    // Useful for consumers that don't need fine-grained control over command construction.

    /// <summary>
    /// Stores an item in the character's storage at the specified coinhouse.
    /// Convenience overload that internally creates a StoreItemCommand.
    /// </summary>
    Task<CommandResult> StoreItemAsync(
        CoinhouseTag coinhouseTag,
        Guid characterId,
        string itemName,
        string itemDescription,
        byte[] itemData,
        CancellationToken ct = default);

    /// <summary>
    /// Withdraws an item from storage.
    /// Convenience overload that internally creates a WithdrawItemCommand.
    /// </summary>
    Task<CommandResult> WithdrawItemAsync(
        long storedItemId,
        Guid characterId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets all stored items for a character at a specific coinhouse.
    /// Convenience overload that internally creates a GetStoredItemsQuery.
    /// </summary>
    Task<List<StoredItemDto>> GetStoredItemsAsync(
        CoinhouseTag coinhouseTag,
        Guid characterId,
        CancellationToken ct = default);

    /// <summary>
    /// Gets storage capacity information for a character at a specific coinhouse.
    /// Convenience overload that internally creates a GetStorageCapacityQuery.
    /// </summary>
    Task<GetStorageCapacityResult> GetStorageCapacityAsync(
        CoinhouseTag coinhouseTag,
        Guid characterId,
        CancellationToken ct = default);

    /// <summary>
    /// Upgrades the storage capacity at the specified coinhouse.
    /// Convenience overload that internally creates an UpgradeStorageCapacityCommand.
    /// </summary>
    Task<CommandResult> UpgradeStorageCapacityAsync(
        CoinhouseTag coinhouseTag,
        Guid characterId,
        CancellationToken ct = default);
}

