using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities.Economy.Shops;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using Anvil.API;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops.PlayerStalls;

/// <summary>
/// Periodically bills active stalls for their daily rent and suspends stalls that cannot pay.
/// </summary>
[ServiceBinding(typeof(PlayerStallRentRenewalService))]
public sealed class PlayerStallRentRenewalService : IDisposable
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private static readonly TimeSpan BillingInterval = TimeSpan.FromHours(1);
    private static readonly TimeSpan GracePeriod = TimeSpan.FromHours(1);
    private static readonly TimeSpan RentInterval = TimeSpan.FromDays(1);

    private readonly IPlayerShopRepository _shops;
    private readonly ICommandHandler<WithdrawGoldCommand> _withdrawHandler;
    private readonly IPlayerStallOwnerNotifier _notifier;
    private readonly IPlayerStallEventBroadcaster _events;
    private readonly IPlayerStallInventoryCustodian _inventoryCustodian;
    private readonly ICoinhouseRepository _coinhouses;

    private readonly CancellationTokenSource _cts = new();
    private readonly Task _runner;

    public PlayerStallRentRenewalService(
        IPlayerShopRepository shops,
        ICommandHandler<WithdrawGoldCommand> withdrawHandler,
        IPlayerStallOwnerNotifier notifier,
        IPlayerStallEventBroadcaster events,
        IPlayerStallInventoryCustodian inventoryCustodian,
        ICoinhouseRepository coinhouses
        )
    {
        _shops = shops ?? throw new ArgumentNullException(nameof(shops));
        _withdrawHandler = withdrawHandler ?? throw new ArgumentNullException(nameof(withdrawHandler));
        _notifier = notifier ?? throw new ArgumentNullException(nameof(notifier));
        _events = events ?? throw new ArgumentNullException(nameof(events));
        _inventoryCustodian = inventoryCustodian ?? throw new ArgumentNullException(nameof(inventoryCustodian));
        _coinhouses = coinhouses ?? throw new ArgumentNullException(nameof(coinhouses));

        _runner = Task.Run(() => RunAsync(_cts.Token));
    }

    public void Dispose()
    {
        try
        {
            _cts.Cancel();
            if (!_runner.IsCompleted)
            {
                _runner.Wait(TimeSpan.FromSeconds(5));
            }
        }
        catch (AggregateException ex)
        {
            Log.Debug(ex, "Rent renewal loop stopped with aggregate exception.");
        }
        catch (ObjectDisposedException)
        {
        }
        finally
        {
            _cts.Dispose();
        }
    }

    private async Task RunAsync(CancellationToken token)
    {
        try
        {
            // Give the server a minute to finish bootstrapping before the first billing cycle.
            await Task.Delay(TimeSpan.FromMinutes(1), token).ConfigureAwait(false);
        }
        catch (TaskCanceledException)
        {
            return;
        }

        PeriodicTimer timer = new(BillingInterval);

        try
        {
            do
            {
                try
                {
                    await ExecuteCycleAsync(token).ConfigureAwait(false);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Unhandled stall rent renewal failure.");
                }
            }
            while (await timer.WaitForNextTickAsync(token).ConfigureAwait(false));
        }
        catch (TaskCanceledException)
        {
            // Service shutting down.
        }
        finally
        {
            timer.Dispose();
        }
    }

    internal Task RunSingleCycleAsync(CancellationToken token) => ExecuteCycleAsync(token);

    private async Task ExecuteCycleAsync(CancellationToken token)
    {
        DateTime utcNow = DateTime.UtcNow;
        List<PlayerStall> stalls = _shops.AllShops();

        foreach (PlayerStall stall in stalls)
        {
            token.ThrowIfCancellationRequested();

            if (stall.OwnerCharacterId is null)
            {
                continue;
            }

            if (stall.NextRentDueUtc > utcNow)
            {
                continue;
            }

            await ProcessStallAsync(stall, utcNow, token).ConfigureAwait(false);
        }
    }

    private async Task ProcessStallAsync(PlayerStall stall, DateTime utcNow, CancellationToken token)
    {
        PlayerStall? refreshed = _shops.GetShopById(stall.Id);
        if (refreshed is null)
        {
            return;
        }

        if (refreshed.OwnerCharacterId is null || string.IsNullOrWhiteSpace(refreshed.OwnerPersonaId))
        {
            return;
        }

        int rentAmount = Math.Max(0, refreshed.DailyRent);
        if (rentAmount == 0)
        {
            await CompleteRentAsync(refreshed, rentAmount, RentChargeSource.None, utcNow).ConfigureAwait(false);
            return;
        }

        RentChargeResult result = await TryChargeAsync(refreshed, rentAmount, utcNow, token).ConfigureAwait(false);

        if (result.Success)
        {
            await CompleteRentAsync(refreshed, rentAmount, result.Source, utcNow).ConfigureAwait(false);
            return;
        }

        await HandleRentFailureAsync(refreshed, result.FailureReason ?? "Rent payment failed.", utcNow).ConfigureAwait(false);
    }

    private async Task<RentChargeResult> TryChargeAsync(PlayerStall stall, int rentAmount, DateTime utcNow, CancellationToken token)
    {
        // Prefer coinhouse when configured, fallback to escrow when possible.
        if (stall.CoinHouseAccountId.HasValue)
        {
            RentChargeResult coinhouse = await TryChargeCoinhouseAsync(stall, rentAmount, utcNow, token).ConfigureAwait(false);
            if (coinhouse.Success)
            {
                return coinhouse;
            }

            // Fall back to escrow when the account could not be charged.
            if (stall.EscrowBalance >= rentAmount)
            {
                return RentChargeResult.Succeeded(RentChargeSource.StallEscrow);
            }

            return coinhouse;
        }

        if (stall.EscrowBalance >= rentAmount)
        {
            return RentChargeResult.Succeeded(RentChargeSource.StallEscrow);
        }

        return RentChargeResult.Failed("Not enough stall earnings to cover rent.");
    }

    private async Task<RentChargeResult> TryChargeCoinhouseAsync(PlayerStall stall, int rentAmount, DateTime utcNow, CancellationToken token)
    {
        if (!stall.CoinHouseAccountId.HasValue)
        {
            return RentChargeResult.Failed("No coinhouse account configured.");
        }

        if (string.IsNullOrWhiteSpace(stall.SettlementTag))
        {
            return RentChargeResult.Failed("Coinhouse configuration is missing for this stall.");
        }

        if (string.IsNullOrWhiteSpace(stall.OwnerPersonaId))
        {
            return RentChargeResult.Failed("Owner persona is missing.");
        }

        CoinhouseTag coinhouseTag;
        string rawTag = stall.SettlementTag.Trim();

        if (int.TryParse(rawTag, NumberStyles.Integer, CultureInfo.InvariantCulture, out int settlementId) && settlementId > 0)
        {
            try
            {
                SettlementId settlement = SettlementId.Parse(settlementId);
                var coinhouse = _coinhouses.GetSettlementCoinhouse(settlement);
                if (coinhouse is null || string.IsNullOrWhiteSpace(coinhouse.Tag))
                {
                    Log.Warn("No coinhouse mapped to settlement {SettlementId} while charging rent for stall {StallId}.", settlementId, stall.Id);
                    return RentChargeResult.Failed("Coinhouse configuration is missing for this stall.");
                }

                coinhouseTag = CoinhouseTag.Parse(coinhouse.Tag);
            }
            catch (Exception ex)
            {
                Log.Warn(ex, "Failed to resolve coinhouse for settlement {SettlementId} on stall {StallId}.", settlementId, stall.Id);
                return RentChargeResult.Failed("Coinhouse configuration is invalid.");
            }
        }
        else
        {
            try
            {
                coinhouseTag = CoinhouseTag.Parse(rawTag);
            }
            catch (Exception ex)
            {
                Log.Warn(ex, "Invalid coinhouse tag '{Tag}' for stall {StallId}.", rawTag, stall.Id);
                return RentChargeResult.Failed("Coinhouse configuration is invalid.");
            }
        }

        PersonaId ownerPersona;
        try
        {
            ownerPersona = PersonaId.Parse(stall.OwnerPersonaId);
        }
        catch (Exception ex)
        {
            Log.Warn(ex, "Unable to parse owner persona '{PersonaId}' for stall {StallId}.", stall.OwnerPersonaId, stall.Id);
            return RentChargeResult.Failed("Owner persona could not be resolved.");
        }

        string reason = BuildCoinhouseRentReason(stall, utcNow);

        WithdrawGoldCommand command;
        try
        {
            command = WithdrawGoldCommand.Create(ownerPersona, coinhouseTag, rentAmount, reason);
        }
        catch (Exception ex)
        {
            Log.Warn(ex, "Failed to construct withdraw command for stall {StallId}.", stall.Id);
            return RentChargeResult.Failed("Rent withdrawal request was invalid.");
        }

        CommandResult result = await _withdrawHandler.HandleAsync(command, token).ConfigureAwait(false);
        if (!result.Success)
        {
            string message = result.ErrorMessage ?? "Coinhouse withdrawal failed.";
            return RentChargeResult.Failed(message);
        }

        return RentChargeResult.Succeeded(RentChargeSource.CoinhouseAccount);
    }

    private async Task CompleteRentAsync(PlayerStall stall, int rentAmount, RentChargeSource source, DateTime utcNow)
    {
        bool updated = _shops.UpdateShop(stall.Id, entity =>
        {
            DateTime nextDue = CalculateNextDue(entity.NextRentDueUtc, utcNow);

            if (source == RentChargeSource.StallEscrow)
            {
                entity.EscrowBalance = Math.Max(0, entity.EscrowBalance - rentAmount);
            }

            if (rentAmount > 0)
            {
                entity.LifetimeNetEarnings -= rentAmount;
                entity.LedgerEntries ??= new List<PlayerStallLedgerEntry>();
                entity.LedgerEntries.Add(new PlayerStallLedgerEntry
                {
                    StallId = entity.Id,
                    EntryType = PlayerStallLedgerEntryType.RentPayment,
                    Amount = -rentAmount,
                    Description = BuildLedgerDescription(source, rentAmount),
                    OccurredUtc = utcNow,
                    MetadataJson = BuildLedgerMetadata(source)
                });
            }

            entity.LastRentPaidUtc = utcNow;
            entity.NextRentDueUtc = nextDue;
            entity.SuspendedUtc = null;
            entity.DeactivatedUtc = null;
            entity.IsActive = true;
        });

        if (!updated)
        {
            Log.Warn("Failed to persist rent payment for stall {StallId}.", stall.Id);
            return;
        }

        Log.Info("Charged {Rent} gp rent for stall {StallId} via {Source}.", rentAmount, stall.Id, source);

        await _events.BroadcastSellerRefreshAsync(stall.Id).ConfigureAwait(false);

        await NotifyOwnerAsync(stall.OwnerCharacterId, BuildSuccessMessage(stall, rentAmount, source), ColorConstants.Orange).ConfigureAwait(false);
    }

    private async Task HandleRentFailureAsync(PlayerStall stall, string reason, DateTime utcNow)
    {
        RentFailureState state = RentFailureState.Unknown;
        // Capture owner info before it gets cleared for the notification message
        Guid? ownerCharacterId = stall.OwnerCharacterId;
        string? ownerPersonaId = stall.OwnerPersonaId;

        bool updated = _shops.UpdateShop(stall.Id, entity =>
        {
            if (entity.SuspendedUtc is null)
            {
                entity.SuspendedUtc = utcNow;
                entity.IsActive = true;
                entity.NextRentDueUtc = utcNow + GracePeriod;
                state = RentFailureState.GracePeriodStarted;
                return;
            }

            TimeSpan delinquentDuration = utcNow - entity.SuspendedUtc.Value;
            if (delinquentDuration < GracePeriod)
            {
                entity.IsActive = true;
                entity.NextRentDueUtc = entity.SuspendedUtc.Value + GracePeriod;
                state = RentFailureState.GracePeriodContinues;
                return;
            }

            // Release ownership when grace period expires so stall can be claimed by others
            entity.OwnerCharacterId = null;
            entity.OwnerPersonaId = null;
            entity.OwnerPlayerPersonaId = null;
            entity.OwnerDisplayName = null;
            entity.CoinHouseAccountId = null;
            entity.HoldEarningsInStall = false;
            entity.IsActive = false;
            entity.DeactivatedUtc ??= utcNow;
            entity.NextRentDueUtc = utcNow + BillingInterval;
            state = RentFailureState.Suspended;
        });

        if (!updated)
        {
            Log.Warn("Failed to persist rent failure state for stall {StallId}.", stall.Id);
        }
        else if (state == RentFailureState.Suspended)
        {
            await _inventoryCustodian.TransferInventoryToMarketReeveAsync(stall).ConfigureAwait(false);
        }

        Log.Warn("Rent charge failed for stall {StallId}: {Reason}", stall.Id, reason);

        await _events.BroadcastSellerRefreshAsync(stall.Id).ConfigureAwait(false);

        string message = BuildFailureMessage(stall, reason, state, utcNow, ownerPersonaId);
        Color color = state == RentFailureState.Suspended ? ColorConstants.Red : ColorConstants.Yellow;

        await NotifyOwnerAsync(ownerCharacterId, message, color).ConfigureAwait(false);
    }

    private Task NotifyOwnerAsync(Guid? ownerCharacterId, string message, Color color)
    {
        return _notifier.NotifyAsync(ownerCharacterId, message, color);
    }

    private static DateTime CalculateNextDue(DateTime currentDue, DateTime utcNow)
    {
        DateTime nextDue = currentDue;
        if (nextDue <= utcNow)
        {
            nextDue = utcNow;
        }

        do
        {
            nextDue = nextDue.Add(RentInterval);
        }
        while (nextDue <= utcNow);

        return nextDue;
    }

    private static string BuildCoinhouseRentReason(PlayerStall stall, DateTime utcNow)
    {
        string label = string.IsNullOrWhiteSpace(stall.Tag) ? $"Stall #{stall.Id}" : BeautifyLabel(stall.Tag);
        string reason = $"Daily stall rent for {label}";
        string timestamp = utcNow.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        string combined = $"{reason} ({timestamp})";
        return combined.Length <= 200 ? combined : combined[..200];
    }

    private static string BuildLedgerDescription(RentChargeSource source, int rentAmount)
    {
        string method = source switch
        {
            RentChargeSource.CoinhouseAccount => "coinhouse account",
            RentChargeSource.StallEscrow => "stall escrow",
            _ => "system"
        };

        return string.Format(CultureInfo.InvariantCulture, "Rent paid ({0:n0} gp via {1})", rentAmount, method);
    }

    private static string? BuildLedgerMetadata(RentChargeSource source)
    {
        var metadata = new
        {
            source = source.ToString()
        };

        return JsonSerializer.Serialize(metadata);
    }

    private static string BuildSuccessMessage(PlayerStall stall, int rentAmount, RentChargeSource source)
    {
        string stallName = BeautifyLabelOrDefault(stall.Tag, stall.Id);
        string method = source switch
        {
            RentChargeSource.CoinhouseAccount => "your coinhouse account",
            RentChargeSource.StallEscrow => "stall earnings",
            _ => "system funds"
        };

        return string.Format(CultureInfo.InvariantCulture, "Rent of {0:n0} gp for {1} was paid from {2}.", rentAmount, stallName, method);
    }

    private static string BuildFailureMessage(PlayerStall stall, string reason, RentFailureState state, DateTime utcNow, string? ownerPersonaId)
    {
        string stallName = BeautifyLabelOrDefault(stall.Tag, stall.Id);
        string baseMessage = string.Format(CultureInfo.InvariantCulture, "Rent collection failed for {0}: {1}", stallName, reason);

        return state switch
        {
            RentFailureState.GracePeriodStarted => string.Format(
                CultureInfo.InvariantCulture,
                "{0} Your stall will suspend if payment isn't received within {1}.",
                baseMessage,
                FormatDuration(GracePeriod)),
            RentFailureState.GracePeriodContinues => string.Format(
                CultureInfo.InvariantCulture,
                "{0} Payment is still overdue; automatic suspension begins shortly.",
                baseMessage),
            RentFailureState.Suspended => string.Format(
                CultureInfo.InvariantCulture,
                "{0} The stall is now suspended until rent is paid. {1}",
                baseMessage,
                BuildSuspensionFollowUp(ownerPersonaId)),
            _ => baseMessage
        };
    }

    private static string BuildSuspensionFollowUp(string? ownerPersonaId)
    {
        if (string.IsNullOrWhiteSpace(ownerPersonaId))
        {
            return "Any remaining inventory has been moved to the market reeve for safekeeping.";
        }

        return string.Format(
            CultureInfo.InvariantCulture,
            "Any remaining inventory has been moved to the market reeve; provide persona ID {0} to reclaim it.",
            ownerPersonaId);
    }

    private static string BeautifyLabelOrDefault(string? tag, long stallId)
    {
        if (string.IsNullOrWhiteSpace(tag))
        {
            return string.Format(CultureInfo.InvariantCulture, "Stall #{0}", stallId);
        }

        return BeautifyLabel(tag);
    }

    private static string BeautifyLabel(string value)
    {
        string normalized = value.Replace('_', ' ').Trim();
        if (normalized.Length == 0)
        {
            return value;
        }

        TextInfo textInfo = CultureInfo.InvariantCulture.TextInfo;
        return textInfo.ToTitleCase(normalized.ToLowerInvariant());
    }

    private sealed record RentChargeResult(bool Success, RentChargeSource Source, string? FailureReason)
    {
        public static RentChargeResult Succeeded(RentChargeSource source) => new(true, source, null);
        public static RentChargeResult Failed(string reason) => new(false, RentChargeSource.None, reason);
    }

    private enum RentChargeSource
    {
        None,
        CoinhouseAccount,
        StallEscrow
    }

    private enum RentFailureState
    {
        Unknown,
        GracePeriodStarted,
        GracePeriodContinues,
        Suspended
    }

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalHours >= 1)
        {
            double hours = Math.Round(duration.TotalHours);
            return string.Format(CultureInfo.InvariantCulture, "{0:n0} hour{1}", hours, Math.Abs(hours - 1d) < double.Epsilon ? string.Empty : "s");
        }

        if (duration.TotalMinutes >= 1)
        {
            double minutes = Math.Round(duration.TotalMinutes);
            return string.Format(CultureInfo.InvariantCulture, "{0:n0} minute{1}", minutes, Math.Abs(minutes - 1d) < double.Epsilon ? string.Empty : "s");
        }

        double seconds = Math.Round(duration.TotalSeconds);
        return string.Format(CultureInfo.InvariantCulture, "{0:n0} second{1}", seconds, Math.Abs(seconds - 1d) < double.Epsilon ? string.Empty : "s");
    }
}
