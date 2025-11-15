using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities.Economy.Shops;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops.PlayerStalls.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops.PlayerStalls.Commands;

/// <summary>
/// Handles the DepositStallRentCommand.
/// Records the rent deposit, updates the stall escrow balance, and publishes domain events.
/// </summary>
[ServiceBinding(typeof(ICommandHandler<DepositStallRentCommand>))]
[ServiceBinding(typeof(ICommandHandlerMarker))]
public sealed class DepositStallRentCommandHandler : ICommandHandler<DepositStallRentCommand>
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly IPlayerShopRepository _shops;
    private readonly IEventBus _eventBus;

    public DepositStallRentCommandHandler(
        IPlayerShopRepository shops,
        IEventBus eventBus)
    {
        _shops = shops ?? throw new ArgumentNullException(nameof(shops));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
    }

    public async Task<CommandResult> HandleAsync(
        DepositStallRentCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            PlayerStall? stall = _shops.GetShopById(command.StallId);
            if (stall is null)
            {
                return CommandResult.Fail($"Stall {command.StallId} not found");
            }

            // Validate the deposit using domain aggregate
            PlayerStallAggregate aggregate = PlayerStallAggregate.FromEntity(stall);
            PlayerStallDomainResult<PlayerStallDeposit> domainResult =
                aggregate.TryDepositToEscrow(command.DepositorPersonaId, command.DepositAmount);

            if (!domainResult.Success)
            {
                return CommandResult.Fail(domainResult.ErrorMessage ?? "Deposit validation failed");
            }

            PlayerStallDeposit deposit = domainResult.Payload!;

            bool updated = _shops.UpdateShop(command.StallId, entity =>
            {
                // Apply the deposit mutation
                deposit.Apply(entity);

                // Add ledger entry
                entity.LedgerEntries.Add(new PlayerStallLedgerEntry
                {
                    StallId = entity.Id,
                    EntryType = PlayerStallLedgerEntryType.Deposit,
                    Amount = command.DepositAmount,
                    Description = BuildLedgerDescription(command.DepositorDisplayName, command.DepositAmount),
                    OccurredUtc = command.DepositTimestamp,
                    MetadataJson = BuildLedgerMetadata(command.DepositorPersonaId, command.DepositorDisplayName)
                });

                entity.UpdatedUtc = command.DepositTimestamp;
            });

            if (!updated)
            {
                return CommandResult.Fail($"Failed to update stall {command.StallId}");
            }

            Log.Info("Stall {StallId} received deposit: {Amount} gp from {Depositor}",
                command.StallId, command.DepositAmount, command.DepositorDisplayName);

            // Publish domain event
            StallEscrowDepositedEvent evt = new()
            {
                StallId = command.StallId,
                DepositAmount = command.DepositAmount,
                DepositorPersonaId = command.DepositorPersonaId,
                DepositorDisplayName = command.DepositorDisplayName,
                NewEscrowBalance = stall.EscrowBalance,
                DepositedAt = command.DepositTimestamp
            };

            await _eventBus.PublishAsync(evt, cancellationToken).ConfigureAwait(false);

            return CommandResult.Ok();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to process deposit for stall {StallId}", command.StallId);
            return CommandResult.Fail($"Failed to process deposit: {ex.Message}");
        }
    }

    private static string BuildLedgerDescription(string depositorName, int amount)
    {
        return $"Rent deposit by {depositorName}: {amount:N0} gp";
    }

    private static string BuildLedgerMetadata(string personaId, string displayName)
    {
        return JsonSerializer.Serialize(new
        {
            depositor = new
            {
                personaId,
                displayName
            },
            type = "rent_deposit"
        });
    }
}
