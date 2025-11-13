using System;
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
/// Handles the PayStallRentCommand.
/// Records the rent payment, updates the stall state, and publishes domain events.
/// </summary>
[ServiceBinding(typeof(ICommandHandler<PayStallRentCommand>))]
[ServiceBinding(typeof(ICommandHandlerMarker))]
public sealed class PayStallRentCommandHandler : ICommandHandler<PayStallRentCommand>
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private static readonly TimeSpan RentInterval = TimeSpan.FromDays(1);

    private readonly IPlayerShopRepository _shops;
    private readonly IEventBus _eventBus;

    public PayStallRentCommandHandler(
        IPlayerShopRepository shops,
        IEventBus eventBus)
    {
        _shops = shops ?? throw new ArgumentNullException(nameof(shops));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
    }

    public async Task<CommandResult> HandleAsync(
        PayStallRentCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            PlayerStall? stall = _shops.GetShopById(command.StallId);
            if (stall is null)
            {
                return CommandResult.Fail($"Stall {command.StallId} not found");
            }

            bool updated = _shops.UpdateShop(command.StallId, entity =>
            {
                DateTime nextDue = CalculateNextDue(entity.NextRentDueUtc, command.PaymentTimestamp);

                // Deduct from escrow if that's the source
                if (command.Source == RentChargeSource.StallEscrow)
                {
                    entity.EscrowBalance = Math.Max(0, entity.EscrowBalance - command.RentAmount);
                }

                // Update lifetime net earnings
                if (command.RentAmount > 0)
                {
                    entity.LifetimeNetEarnings -= command.RentAmount;

                    // Add ledger entry
                    entity.LedgerEntries.Add(new PlayerStallLedgerEntry
                    {
                        StallId = entity.Id,
                        EntryType = PlayerStallLedgerEntryType.RentPayment,
                        Amount = -command.RentAmount,
                        Description = BuildLedgerDescription(command.Source, command.RentAmount),
                        OccurredUtc = command.PaymentTimestamp,
                        MetadataJson = BuildLedgerMetadata(command.Source)
                    });
                }

                // Update rent tracking
                entity.LastRentPaidUtc = command.PaymentTimestamp;
                entity.NextRentDueUtc = nextDue;
                entity.SuspendedUtc = null;
                entity.DeactivatedUtc = null;
                entity.IsActive = true;
            });

            if (!updated)
            {
                return CommandResult.Fail($"Failed to update stall {command.StallId}");
            }

            Log.Info("Stall {StallId} rent paid: {Amount} gp via {Source}",
                command.StallId, command.RentAmount, command.Source);

            // Publish domain event
            StallRentPaidEvent evt = new StallRentPaidEvent
            {
                StallId = command.StallId,
                RentAmount = command.RentAmount,
                Source = command.Source,
                NextDueDate = CalculateNextDue(stall.NextRentDueUtc, command.PaymentTimestamp),
                PaidAt = command.PaymentTimestamp
            };

            await _eventBus.PublishAsync(evt, cancellationToken).ConfigureAwait(false);

            return CommandResult.Ok();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error processing PayStallRentCommand for stall {StallId}", command.StallId);
            return CommandResult.Fail($"Failed to process rent payment: {ex.Message}");
        }
    }

    private static DateTime CalculateNextDue(DateTime currentNextDue, DateTime now)
    {
        // If we're already past due, start from now
        if (currentNextDue < now)
        {
            return now + RentInterval;
        }

        // Otherwise, add the interval to the current due date
        return currentNextDue + RentInterval;
    }

    private static string BuildLedgerDescription(RentChargeSource source, int amount)
    {
        return source switch
        {
            RentChargeSource.StallEscrow => $"Rent deducted from stall earnings: {amount} gp",
            RentChargeSource.CoinhouseAccount => $"Rent paid from coinhouse: {amount} gp",
            RentChargeSource.None => "Rent waived (no charge)",
            _ => $"Rent payment: {amount} gp"
        };
    }

    private static string BuildLedgerMetadata(RentChargeSource source)
    {
        return System.Text.Json.JsonSerializer.Serialize(new
        {
            source = source.ToString(),
            type = "automatic_rent_payment"
        });
    }
}

