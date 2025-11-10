using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Storage.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Storage.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Gateways;

/// <summary>
/// Gateway for storage operations.
/// Provides access to item storage, withdrawal, and capacity management.
/// </summary>
public interface IStorageGateway
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
}

