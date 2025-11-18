using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities.Economy.Shops;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops.PlayerStalls.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops.PlayerStalls.Commands;

/// <summary>
/// Handles the SuspendStallForNonPaymentCommand.
/// Suspends the stall and grants a grace period, or releases ownership if grace expired.
/// </summary>
[ServiceBinding(typeof(ICommandHandler<SuspendStallForNonPaymentCommand>))]
[ServiceBinding(typeof(ICommandHandlerMarker))]
public sealed class SuspendStallForNonPaymentCommandHandler : ICommandHandler<SuspendStallForNonPaymentCommand>
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly IPlayerShopRepository _shops;
    private readonly IEventBus _eventBus;
    private readonly IPlayerStallInventoryCustodian _inventoryCustodian;
    private readonly IReeveFundsService _reeveFunds;

    public SuspendStallForNonPaymentCommandHandler(
        IPlayerShopRepository shops,
        IEventBus eventBus,
        IPlayerStallInventoryCustodian inventoryCustodian,
        IReeveFundsService reeveFunds)
    {
        _shops = shops ?? throw new ArgumentNullException(nameof(shops));
        _eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
        _inventoryCustodian = inventoryCustodian ?? throw new ArgumentNullException(nameof(inventoryCustodian));
        _reeveFunds = reeveFunds ?? throw new ArgumentNullException(nameof(reeveFunds));
    }

    public async Task<CommandResult> HandleAsync(
        SuspendStallForNonPaymentCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            PlayerStall? stall = _shops.GetShopById(command.StallId);
            if (stall is null)
            {
                return CommandResult.Fail($"Stall {command.StallId} not found");
            }

            // Capture ownership and escrow info before potential clearing
            Guid? formerOwnerId = stall.OwnerCharacterId;
            string? formerPersonaId = stall.OwnerPersonaId;
            string areaResRef = stall.AreaResRef;
            int escrowBalance = stall.EscrowBalance;
            bool isFirstSuspension = stall.SuspendedUtc is null;
            bool shouldRelease = false;

            bool updated = _shops.UpdateShop(command.StallId, entity =>
            {
                // First suspension - start grace period
                if (entity.SuspendedUtc is null)
                {
                    entity.SuspendedUtc = command.SuspensionTimestamp;
                    entity.IsActive = true;
                    entity.NextRentDueUtc = command.SuspensionTimestamp + command.GracePeriod;
                    return;
                }

                // Already suspended - check if still in grace period
                TimeSpan delinquentDuration = command.SuspensionTimestamp - entity.SuspendedUtc.Value;
                if (delinquentDuration < command.GracePeriod)
                {
                    entity.IsActive = true;
                    entity.NextRentDueUtc = entity.SuspendedUtc.Value + command.GracePeriod;
                    return;
                }

                // Grace period expired - release ownership
                shouldRelease = true;
                entity.OwnerCharacterId = null;
                entity.OwnerPersonaId = null;
                entity.OwnerPlayerPersonaId = null;
                entity.OwnerDisplayName = null;
                entity.CoinHouseAccountId = null;
                entity.HoldEarningsInStall = false;
                entity.EscrowBalance = 0; // Zero out escrow (will deposit to vault)
                entity.IsActive = false;
                entity.DeactivatedUtc ??= command.SuspensionTimestamp;
                entity.NextRentDueUtc = command.SuspensionTimestamp + TimeSpan.FromHours(1);
            });

            if (!updated)
            {
                return CommandResult.Fail($"Failed to update stall {command.StallId}");
            }

            if (shouldRelease)
            {
                // Deposit escrow to vault before clearing inventory
                if (escrowBalance > 0 && !string.IsNullOrWhiteSpace(formerPersonaId))
                {
                    try
                    {
                        PersonaId persona = PersonaId.Parse(formerPersonaId);
                        CommandResult vaultResult = await _reeveFunds.DepositHeldFundsAsync(
                            persona,
                            areaResRef,
                            escrowBalance,
                            $"Stall {command.StallId} eviction escrow",
                            cancellationToken).ConfigureAwait(false);

                        if (vaultResult.Success)
                        {
                            Log.Info("Deposited {Amount} gp escrow to vault for evicted stall {StallId} owner {Persona}",
                                escrowBalance, command.StallId, persona);
                        }
                        else
                        {
                            Log.Warn("Failed to deposit escrow to vault for stall {StallId}: {Error}",
                                command.StallId, vaultResult.ErrorMessage);
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "Error depositing escrow to vault for stall {StallId}", command.StallId);
                    }
                }

                // Transfer inventory to market reeve custody
                try
                {
                    await _inventoryCustodian.TransferInventoryToMarketReeveAsync(stall, cancellationToken)
                        .ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Failed to transfer inventory for released stall {StallId}", command.StallId);
                }

                Log.Warn("Stall {StallId} ownership released after grace period expired", command.StallId);

                // Publish ownership released event
                StallOwnershipReleasedEvent releaseEvt = new StallOwnershipReleasedEvent
                {
                    StallId = command.StallId,
                    FormerOwnerId = formerOwnerId,
                    FormerPersonaId = formerPersonaId,
                    Reason = command.Reason
                };

                await _eventBus.PublishAsync(releaseEvt, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                DateTime gracePeriodEnds = isFirstSuspension
                    ? command.SuspensionTimestamp + command.GracePeriod
                    : stall.SuspendedUtc!.Value + command.GracePeriod;

                Log.Warn("Stall {StallId} suspended for non-payment. Grace period ends: {GraceEnds}",
                    command.StallId, gracePeriodEnds);

                // Publish suspension event
                StallSuspendedEvent suspendEvt = new StallSuspendedEvent
                {
                    StallId = command.StallId,
                    Reason = command.Reason,
                    GracePeriodEnds = gracePeriodEnds,
                    IsFirstSuspension = isFirstSuspension
                };

                await _eventBus.PublishAsync(suspendEvt, cancellationToken).ConfigureAwait(false);
            }

            return CommandResult.Ok();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error processing SuspendStallForNonPaymentCommand for stall {StallId}", command.StallId);
            return CommandResult.Fail($"Failed to suspend stall: {ex.Message}");
        }
    }
}

