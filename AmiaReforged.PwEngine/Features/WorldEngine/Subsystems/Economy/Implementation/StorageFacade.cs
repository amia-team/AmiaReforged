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
/// Routes operations through the central command/query dispatchers so writes get
/// logging, the exception-to-Fail contract, and CommandExecutedEvent publishing.
/// </summary>
[ServiceBinding(typeof(IStorageFacade))]
public sealed class StorageFacade : IStorageFacade
{
    private readonly ICommandDispatcher _commands;
    private readonly IQueryDispatcher _queries;

    public StorageFacade(
        ICommandDispatcher commands,
        IQueryDispatcher queries)
    {
        _commands = commands;
        _queries = queries;
    }

    /// <inheritdoc />
    public Task<CommandResult> StoreItemAsync(StoreItemCommand command, CancellationToken ct = default)
        => _commands.DispatchAsync(command, ct);

    /// <inheritdoc />
    public Task<CommandResult> WithdrawItemAsync(WithdrawItemCommand command, CancellationToken ct = default)
        => _commands.DispatchAsync(command, ct);

    /// <inheritdoc />
    public Task<List<StoredItemDto>> GetStoredItemsAsync(GetStoredItemsQuery query, CancellationToken ct = default)
        => _queries.DispatchAsync<GetStoredItemsQuery, List<StoredItemDto>>(query, ct);

    /// <inheritdoc />
    public Task<GetStorageCapacityResult> GetStorageCapacityAsync(GetStorageCapacityQuery query, CancellationToken ct = default)
        => _queries.DispatchAsync<GetStorageCapacityQuery, GetStorageCapacityResult>(query, ct);

    /// <inheritdoc />
    public Task<CommandResult> UpgradeStorageCapacityAsync(UpgradeStorageCapacityCommand command, CancellationToken ct = default)
        => _commands.DispatchAsync(command, ct);

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

