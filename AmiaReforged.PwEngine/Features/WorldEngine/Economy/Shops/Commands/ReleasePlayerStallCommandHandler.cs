using System;
using System.Threading;
using System.Threading.Tasks;
using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities.Economy.Shops;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops.Commands;

/// <summary>
/// Handles releasing a stall from its current owner back to the system pool.
/// </summary>
[ServiceBinding(typeof(ICommandHandler<ReleasePlayerStallCommand>))]
public sealed class ReleasePlayerStallCommandHandler : ICommandHandler<ReleasePlayerStallCommand>
{
    private readonly IPlayerShopRepository _shops;

    public ReleasePlayerStallCommandHandler(IPlayerShopRepository shops)
    {
        _shops = shops;
    }

    public Task<CommandResult> HandleAsync(
        ReleasePlayerStallCommand command,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        PlayerStall? stall = _shops.GetShopById(command.StallId);
        if (stall == null)
        {
            return Task.FromResult(CommandResult.Fail($"Stall {command.StallId} was not found."));
        }

        string requestorKey = command.Requestor.ToString();
        bool isOwned = stall.OwnerCharacterId.HasValue || !string.IsNullOrWhiteSpace(stall.OwnerPersonaId);
        bool isOwner = string.Equals(stall.OwnerPersonaId, requestorKey, StringComparison.OrdinalIgnoreCase);

        if (!isOwned && !command.Force)
        {
            return Task.FromResult(CommandResult.Fail("Stall is not currently owned."));
        }

        if (!isOwner && !command.Force)
        {
            return Task.FromResult(CommandResult.Fail("Only the owner can release this stall."));
        }

        bool updated = _shops.UpdateShop(stall.Id, entity =>
        {
            entity.OwnerCharacterId = null;
            entity.OwnerPersonaId = null;
            entity.OwnerDisplayName = null;
            entity.CoinHouseAccountId = null;
            entity.HoldEarningsInStall = false;
            entity.SuspendedUtc = DateTime.UtcNow;
            entity.DeactivatedUtc = DateTime.UtcNow;
            entity.IsActive = false;
        });

        if (!updated)
        {
            return Task.FromResult(CommandResult.Fail("Failed to release stall ownership."));
        }

        return Task.FromResult(CommandResult.OkWith("stallId", stall.Id));
    }
}
