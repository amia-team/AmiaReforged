using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Facades;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops.Commands;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation;

/// <summary>
/// Implementation of the Shop Gateway.
/// Routes operations through the central command dispatcher so writes get
/// logging, the exception-to-Fail contract, and CommandExecutedEvent publishing.
/// </summary>
[ServiceBinding(typeof(IShopFacade))]
public sealed class ShopFacade : IShopFacade
{
    private readonly ICommandDispatcher _commands;

    public ShopFacade(ICommandDispatcher commands)
    {
        _commands = commands;
    }

    /// <inheritdoc />
    public Task<CommandResult> ClaimPlayerStallAsync(ClaimPlayerStallCommand command, CancellationToken ct = default)
        => _commands.DispatchAsync(command, ct);

    /// <inheritdoc />
    public Task<CommandResult> ReleasePlayerStallAsync(ReleasePlayerStallCommand command, CancellationToken ct = default)
        => _commands.DispatchAsync(command, ct);

    /// <inheritdoc />
    public Task<CommandResult> ListStallProductAsync(ListStallProductCommand command, CancellationToken ct = default)
        => _commands.DispatchAsync(command, ct);
}
