using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Storage.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Storage.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Storage;

/// <summary>
/// Facade service that provides a simplified API for bank storage operations.
/// Encapsulates CQRS command/query handlers to reduce boilerplate in consumers.
/// </summary>
[ServiceBinding(typeof(IBankStorageService))]
public class BankStorageService : IBankStorageService
{
    private readonly ICommandHandler<StoreItemCommand> _storeHandler;
    private readonly ICommandHandler<WithdrawItemCommand> _withdrawHandler;
    private readonly ICommandHandler<UpgradeStorageCapacityCommand> _upgradeHandler;
    private readonly IQueryHandler<GetStoredItemsQuery, List<StoredItemDto>> _getItemsHandler;
    private readonly IQueryHandler<GetStorageCapacityQuery, GetStorageCapacityResult> _getCapacityHandler;

    public BankStorageService(
        ICommandHandler<StoreItemCommand> storeHandler,
        ICommandHandler<WithdrawItemCommand> withdrawHandler,
        ICommandHandler<UpgradeStorageCapacityCommand> upgradeHandler,
        IQueryHandler<GetStoredItemsQuery, List<StoredItemDto>> getItemsHandler,
        IQueryHandler<GetStorageCapacityQuery, GetStorageCapacityResult> getCapacityHandler)
    {
        _storeHandler = storeHandler;
        _withdrawHandler = withdrawHandler;
        _upgradeHandler = upgradeHandler;
        _getItemsHandler = getItemsHandler;
        _getCapacityHandler = getCapacityHandler;
    }

    /// <inheritdoc />
    public async Task<CommandResult> StoreItemAsync(
        CoinhouseTag coinhouseTag,
        Guid characterId,
        string itemName,
        string itemDescription,
        byte[] itemData,
        CancellationToken cancellationToken = default)
    {
        StoreItemCommand command = new(coinhouseTag, characterId, itemName, itemDescription, itemData);
        return await _storeHandler.HandleAsync(command, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<CommandResult> WithdrawItemAsync(
        long storedItemId,
        Guid characterId,
        CancellationToken cancellationToken = default)
    {
        WithdrawItemCommand command = new(storedItemId, characterId);
        return await _withdrawHandler.HandleAsync(command, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<CommandResult> UpgradeStorageCapacityAsync(
        CoinhouseTag coinhouseTag,
        Guid characterId,
        CancellationToken cancellationToken = default)
    {
        UpgradeStorageCapacityCommand command = new(coinhouseTag, characterId);
        return await _upgradeHandler.HandleAsync(command, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<List<StoredItemDto>> GetStoredItemsAsync(
        CoinhouseTag coinhouseTag,
        Guid characterId,
        CancellationToken cancellationToken = default)
    {
        GetStoredItemsQuery query = new(coinhouseTag, characterId);
        return await _getItemsHandler.HandleAsync(query, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<GetStorageCapacityResult> GetStorageCapacityAsync(
        CoinhouseTag coinhouseTag,
        Guid characterId,
        CancellationToken cancellationToken = default)
    {
        GetStorageCapacityQuery query = new(coinhouseTag, characterId);
        return await _getCapacityHandler.HandleAsync(query, cancellationToken);
    }
}
