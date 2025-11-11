using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Facades;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops.Commands;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation;

/// <summary>
/// Implementation of the Shop Gateway.
/// Delegates to existing command and query handlers for shop operations.
/// </summary>
[ServiceBinding(typeof(IShopFacade))]
public sealed class ShopFacade : IShopFacade
{
    private readonly ICommandHandler<ClaimPlayerStallCommand> _claimStallHandler;
    private readonly ICommandHandler<ReleasePlayerStallCommand> _releaseStallHandler;
    private readonly ICommandHandler<ListStallProductCommand> _listProductHandler;

    public ShopFacade(
        ICommandHandler<ClaimPlayerStallCommand> claimStallHandler,
        ICommandHandler<ReleasePlayerStallCommand> releaseStallHandler,
        ICommandHandler<ListStallProductCommand> listProductHandler)
    {
        _claimStallHandler = claimStallHandler;
        _releaseStallHandler = releaseStallHandler;
        _listProductHandler = listProductHandler;
    }

    /// <inheritdoc />
    public Task<CommandResult> ClaimPlayerStallAsync(ClaimPlayerStallCommand command, CancellationToken ct = default)
        => _claimStallHandler.HandleAsync(command, ct);

    /// <inheritdoc />
    public Task<CommandResult> ReleasePlayerStallAsync(ReleasePlayerStallCommand command, CancellationToken ct = default)
        => _releaseStallHandler.HandleAsync(command, ct);

    /// <inheritdoc />
    public Task<CommandResult> ListStallProductAsync(ListStallProductCommand command, CancellationToken ct = default)
        => _listProductHandler.HandleAsync(command, ct);
}

