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
/// Handles requests to claim ownership of a player stall.
/// </summary>
[ServiceBinding(typeof(ICommandHandler<ClaimPlayerStallCommand>))]
public sealed class ClaimPlayerStallCommandHandler : ICommandHandler<ClaimPlayerStallCommand>
{
    private readonly IPlayerShopRepository _shops;

    public ClaimPlayerStallCommandHandler(IPlayerShopRepository shops)
    {
        _shops = shops;
    }

    public Task<CommandResult> HandleAsync(
        ClaimPlayerStallCommand command,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        PlayerStall? stall = _shops.GetShopById(command.StallId);
        if (stall == null)
        {
            return Task.FromResult(CommandResult.Fail($"Stall {command.StallId} was not found."));
        }

        Guid ownerGuid;
        try
        {
            ownerGuid = PersonaId.ToGuid(command.OwnerPersona);
        }
        catch (Exception ex) when (ex is InvalidOperationException or FormatException or ArgumentException)
        {
            return Task.FromResult(CommandResult.Fail("Owner persona must resolve to a GUID-backed actor."));
        }

        if (stall.OwnerCharacterId.HasValue && stall.OwnerCharacterId != ownerGuid)
        {
            return Task.FromResult(CommandResult.Fail("Stall is already claimed by a different owner."));
        }

        bool updated = _shops.UpdateShop(stall.Id, entity =>
        {
            entity.OwnerCharacterId = ownerGuid;
            entity.OwnerPersonaId = command.OwnerPersona.ToString();
            entity.OwnerDisplayName = command.OwnerDisplayName;
            entity.CoinHouseAccountId = command.CoinHouseAccountId;
            entity.HoldEarningsInStall = command.HoldEarningsInStall;
            entity.LeaseStartUtc = command.LeaseStartUtc;
            entity.NextRentDueUtc = command.NextRentDueUtc;
            entity.LastRentPaidUtc ??= command.LeaseStartUtc;
            entity.SuspendedUtc = null;
            entity.DeactivatedUtc = null;
            entity.IsActive = true;
        });

        if (!updated)
        {
            return Task.FromResult(CommandResult.Fail("Failed to persist stall ownership changes."));
        }

        return Task.FromResult(CommandResult.OkWith("stallId", stall.Id));
    }
}
