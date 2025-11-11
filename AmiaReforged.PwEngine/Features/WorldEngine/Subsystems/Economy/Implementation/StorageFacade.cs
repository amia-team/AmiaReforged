using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Facades;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Storage.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Storage.Queries;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation;

/// <summary>
/// Implementation of the Storage Gateway.
/// Delegates to existing command and query handlers for storage operations.
/// </summary>
[ServiceBinding(typeof(IStorageFacade))]
public sealed class StorageFacade : IStorageFacade
{
    private readonly ICommandHandler<StoreItemCommand> _storeItemHandler;
    private readonly ICommandHandler<WithdrawItemCommand> _withdrawItemHandler;
    private readonly IQueryHandler<GetStoredItemsQuery, List<StoredItemDto>> _getStoredItemsHandler;
    private readonly IQueryHandler<GetStorageCapacityQuery, GetStorageCapacityResult> _getCapacityHandler;
    private readonly ICommandHandler<UpgradeStorageCapacityCommand> _upgradeCapacityHandler;

    public StorageFacade(
        ICommandHandler<StoreItemCommand> storeItemHandler,
        ICommandHandler<WithdrawItemCommand> withdrawItemHandler,
        IQueryHandler<GetStoredItemsQuery, List<StoredItemDto>> getStoredItemsHandler,
        IQueryHandler<GetStorageCapacityQuery, GetStorageCapacityResult> getCapacityHandler,
        ICommandHandler<UpgradeStorageCapacityCommand> upgradeCapacityHandler)
    {
        _storeItemHandler = storeItemHandler;
        _withdrawItemHandler = withdrawItemHandler;
        _getStoredItemsHandler = getStoredItemsHandler;
        _getCapacityHandler = getCapacityHandler;
        _upgradeCapacityHandler = upgradeCapacityHandler;
    }

    /// <inheritdoc />
    public Task<CommandResult> StoreItemAsync(StoreItemCommand command, CancellationToken ct = default)
        => _storeItemHandler.HandleAsync(command, ct);

    /// <inheritdoc />
    public Task<CommandResult> WithdrawItemAsync(WithdrawItemCommand command, CancellationToken ct = default)
        => _withdrawItemHandler.HandleAsync(command, ct);

    /// <inheritdoc />
    public Task<List<StoredItemDto>> GetStoredItemsAsync(GetStoredItemsQuery query, CancellationToken ct = default)
        => _getStoredItemsHandler.HandleAsync(query, ct);

    /// <inheritdoc />
    public Task<GetStorageCapacityResult> GetStorageCapacityAsync(GetStorageCapacityQuery query, CancellationToken ct = default)
        => _getCapacityHandler.HandleAsync(query, ct);

    /// <inheritdoc />
    public Task<CommandResult> UpgradeStorageCapacityAsync(UpgradeStorageCapacityCommand command, CancellationToken ct = default)
        => _upgradeCapacityHandler.HandleAsync(command, ct);

    // === Convenience Overloads ===

    /// <inheritdoc />
    public Task<CommandResult> StoreItemAsync(
        CoinhouseTag coinhouseTag,
        Guid characterId,
        string itemName,
        string itemDescription,
        byte[] itemData,
    /// <inheritdoc />
        CancellationToken ct = default)
    {
        StoreItemCommand command = new StoreItemCommand(coinhouseTag, characterId, itemName, itemDescription, itemData);
        return StoreItemAsync(command, ct);
    }

    public Task<CommandResult> WithdrawItemAsync(
        long storedItemId,
    /// <inheritdoc />
        Guid characterId,
        CancellationToken ct = default)
    {
        WithdrawItemCommand command = new WithdrawItemCommand(storedItemId, characterId);
        return WithdrawItemAsync(command, ct);
    }

    public Task<List<StoredItemDto>> GetStoredItemsAsync(
    /// <inheritdoc />
        CoinhouseTag coinhouseTag,
        Guid characterId,
        CancellationToken ct = default)
    {
        GetStoredItemsQuery query = new GetStoredItemsQuery(coinhouseTag, characterId);
        return GetStoredItemsAsync(query, ct);
    }

    /// <inheritdoc />
    public Task<GetStorageCapacityResult> GetStorageCapacityAsync(
        CoinhouseTag coinhouseTag,
        Guid characterId,
        CancellationToken ct = default)
    {
        GetStorageCapacityQuery query = new GetStorageCapacityQuery(coinhouseTag, characterId);
        return GetStorageCapacityAsync(query, ct);
    }

    public Task<CommandResult> UpgradeStorageCapacityAsync(
        CoinhouseTag coinhouseTag,
        Guid characterId,
        CancellationToken ct = default)
    {
        UpgradeStorageCapacityCommand command = new UpgradeStorageCapacityCommand(coinhouseTag, characterId);
        return UpgradeStorageCapacityAsync(command, ct);
    }
}

