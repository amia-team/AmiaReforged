using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Storage.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Storage.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Gateways;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Implementations.Gateways;

/// <summary>
/// Implementation of the Storage Gateway.
/// Delegates to existing command and query handlers for storage operations.
/// </summary>
[ServiceBinding(typeof(IStorageGateway))]
public sealed class StorageGateway : IStorageGateway
{
    private readonly ICommandHandler<StoreItemCommand> _storeItemHandler;
    private readonly ICommandHandler<WithdrawItemCommand> _withdrawItemHandler;
    private readonly IQueryHandler<GetStoredItemsQuery, List<StoredItemDto>> _getStoredItemsHandler;
    private readonly IQueryHandler<GetStorageCapacityQuery, GetStorageCapacityResult> _getCapacityHandler;
    private readonly ICommandHandler<UpgradeStorageCapacityCommand> _upgradeCapacityHandler;

    public StorageGateway(
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

    public Task<CommandResult> StoreItemAsync(StoreItemCommand command, CancellationToken ct = default)
        => _storeItemHandler.HandleAsync(command, ct);

    public Task<CommandResult> WithdrawItemAsync(WithdrawItemCommand command, CancellationToken ct = default)
        => _withdrawItemHandler.HandleAsync(command, ct);

    public Task<List<StoredItemDto>> GetStoredItemsAsync(GetStoredItemsQuery query, CancellationToken ct = default)
        => _getStoredItemsHandler.HandleAsync(query, ct);

    public Task<GetStorageCapacityResult> GetStorageCapacityAsync(GetStorageCapacityQuery query, CancellationToken ct = default)
        => _getCapacityHandler.HandleAsync(query, ct);

    public Task<CommandResult> UpgradeStorageCapacityAsync(UpgradeStorageCapacityCommand command, CancellationToken ct = default)
        => _upgradeCapacityHandler.HandleAsync(command, ct);
}

