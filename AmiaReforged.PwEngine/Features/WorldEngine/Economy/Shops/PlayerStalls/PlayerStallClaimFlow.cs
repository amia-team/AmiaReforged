using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Threading.Tasks;
using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities.Economy.Shops;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Accounts;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Properties;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops.PlayerStalls.Nui;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using Anvil.API;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops.PlayerStalls;

/// <summary>
/// Coordinates the interactive flow for claiming an unowned player stall.
/// </summary>
[ServiceBinding(typeof(PlayerStallClaimFlow))]
public sealed class PlayerStallClaimFlow
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private static readonly TimeSpan ClaimConfirmationTimeout = TimeSpan.FromSeconds(60);
    private static readonly TimeSpan RentInterval = TimeSpan.FromDays(1);

    private readonly WindowDirector _windowDirector;
    private readonly ICoinhouseRepository _coinhouses;
    private readonly IPlayerShopRepository _shops;
    private readonly ICommandHandler<ClaimPlayerStallCommand> _claimHandler;
    private readonly ICommandHandler<WithdrawGoldCommand> _withdrawHandler;

    private readonly ConcurrentDictionary<PersonaId, PendingClaimSession> _activeSessions = new();

    public PlayerStallClaimFlow(
        WindowDirector windowDirector,
        ICoinhouseRepository coinhouses,
        IPlayerShopRepository shops,
        ICommandHandler<ClaimPlayerStallCommand> claimHandler,
        ICommandHandler<WithdrawGoldCommand> withdrawHandler)
    {
        _windowDirector = windowDirector ?? throw new ArgumentNullException(nameof(windowDirector));
        _coinhouses = coinhouses ?? throw new ArgumentNullException(nameof(coinhouses));
        _shops = shops ?? throw new ArgumentNullException(nameof(shops));
        _claimHandler = claimHandler ?? throw new ArgumentNullException(nameof(claimHandler));
        _withdrawHandler = withdrawHandler ?? throw new ArgumentNullException(nameof(withdrawHandler));
    }

    public async Task BeginClaimAsync(
        NwPlayer player,
        NwPlaceable placeable,
        PlayerStall stall,
        PersonaId personaId)
    {
        ArgumentNullException.ThrowIfNull(player);
        ArgumentNullException.ThrowIfNull(placeable);
        ArgumentNullException.ThrowIfNull(stall);

        try
        {
            await NwTask.SwitchToMainThread();

            if (!player.IsValid || !placeable.IsValid)
            {
                return;
            }

            string placeableTag = placeable.Tag ?? stall.Tag;
            string areaResRef = placeable.Area?.ResRef ?? stall.AreaResRef;
            string? areaName = placeable.Area?.Name;
            string ownerDisplayName = ResolveOwnerDisplayName(player);

            string? settlementName = ResolveSettlementName(stall.SettlementTag, areaName);
            string stallName = ResolveStallName(stall, areaName);
            string stallDescription = ResolveStallDescription(stall, areaName);

            GoldAmount rentCost = GoldAmount.Parse(Math.Max(0, stall.DailyRent));
            string formattedRent = FormatGold(rentCost.Value);

            GoldAmount availableGold = await GetPlayerGoldAsync(player).ConfigureAwait(false);

            RentStallPaymentOptionViewModel? directOption = BuildDirectOption(availableGold, rentCost, formattedRent);
            (RentStallPaymentOptionViewModel? coinhouseOption, CoinhouseTag? coinhouseTag, Guid? coinhouseAccountId) =
                await BuildCoinhouseOptionAsync(stall, personaId, rentCost, formattedRent, settlementName)
                    .ConfigureAwait(false);

            if (directOption is null && coinhouseOption is null)
            {
                await SendServerMessageAsync(player,
                        "This stall is not configured for payments yet. Please notify a DM.",
                        ColorConstants.Orange)
                    .ConfigureAwait(false);
                return;
            }

            RemoveSession(personaId);

            RentStallWindowConfig config = new(
                Title: stallName,
                StallName: stallName,
                StallDescription: stallDescription,
                RentCostText: $"Daily rent: {formattedRent} gold",
                Timeout: ClaimConfirmationTimeout,
                DirectPaymentOption: directOption,
                CoinhousePaymentOption: coinhouseOption,
                OnConfirm: method => ProcessSelectionAsync(player, personaId, method))
            {
                SettlementName = settlementName,
                OnCancel = () => OnRentWindowCancelledAsync(personaId),
                OnTimeout = () => OnRentWindowTimedOutAsync(player, personaId, stallName),
                OnClosed = () => OnRentWindowClosedAsync(personaId)
            };

            PendingClaimSession session = new(
                stall.Id,
                placeableTag,
                areaResRef,
                rentCost,
                DateTimeOffset.UtcNow,
                stallName,
                settlementName,
                ownerDisplayName,
                directOption is not null && directOption.Visible,
                coinhouseOption is not null && coinhouseOption.Visible,
                coinhouseTag,
                coinhouseAccountId);

            RentStallWindowView view = new(player, config);

            await NwTask.SwitchToMainThread();
            _windowDirector.CloseWindow(player, typeof(RentStallWindowPresenter));
            _activeSessions[personaId] = session;
            _windowDirector.OpenWindow(view.Presenter);

            await SendServerMessageAsync(player,
                    $"Review the leasing terms for {stallName}.",
                    ColorConstants.Orange)
                .ConfigureAwait(false);

            Log.Info("Player {PlayerName} initiated stall claim for stall {StallId} ({Tag}) in area {AreaResRef}.",
                player.PlayerName,
                stall.Id,
                stall.Tag,
                areaResRef);
        }
        catch (Exception ex)
        {
            Log.Error(ex,
                "Failed to begin stall claim flow for player {PlayerName} on stall {StallId}.",
                player.PlayerName,
                stall.Id);

            await SendServerMessageAsync(player,
                    "We couldn't open the stall leasing window. Please try again or notify a DM.",
                    ColorConstants.Red)
                .ConfigureAwait(false);
        }
    }

    private async Task<RentStallSubmissionResult> ProcessSelectionAsync(
        NwPlayer player,
        PersonaId personaId,
        RentalPaymentMethod method)
    {
        try
        {
            if (!_activeSessions.TryGetValue(personaId, out PendingClaimSession? session))
            {
                await SendServerMessageAsync(player,
                        "The leasing offer has expired. Interact with the stall again to restart the claim.",
                        ColorConstants.Orange)
                    .ConfigureAwait(false);

                return RentStallSubmissionResult.Error(
                    "The leasing offer is no longer available.",
                    closeWindow: true);
            }

            PendingClaimSession activeSession = session;

            if (DateTimeOffset.UtcNow - activeSession.CreatedAt > ClaimConfirmationTimeout)
            {
                RemoveSession(personaId);
                await SendServerMessageAsync(player,
                        "The leasing offer has expired. Interact with the stall again to restart.",
                        ColorConstants.Orange)
                    .ConfigureAwait(false);

                return RentStallSubmissionResult.Error(
                    "This leasing offer has expired.",
                    closeWindow: true);
            }

            PlayerStall? latest = await Task.Run(() => _shops.GetShopById(activeSession.StallId)).ConfigureAwait(false);
            if (latest is null)
            {
                RemoveSession(personaId);
                await SendServerMessageAsync(player,
                        "We couldn't load the stall record. Please try again later.",
                        ColorConstants.Red)
                    .ConfigureAwait(false);

                return RentStallSubmissionResult.Error(
                    "The stall record could not be loaded.",
                    closeWindow: true);
            }

            if (!string.Equals(latest.AreaResRef, activeSession.AreaResRef, StringComparison.OrdinalIgnoreCase) ||
                !string.Equals(latest.Tag, activeSession.PlaceableTag, StringComparison.OrdinalIgnoreCase))
            {
                RemoveSession(personaId);
                await SendServerMessageAsync(player,
                        "The stall configuration has changed. Please notify a DM if this persists.",
                        ColorConstants.Orange)
                    .ConfigureAwait(false);

                return RentStallSubmissionResult.Error(
                    "This stall is no longer claimable.",
                    closeWindow: true);
            }

            if (!IsStallAvailable(latest))
            {
                RemoveSession(personaId);
                await SendServerMessageAsync(player,
                        "Someone else has already claimed this stall.",
                        ColorConstants.Orange)
                    .ConfigureAwait(false);

                return RentStallSubmissionResult.Error(
                    "This stall was just claimed by another player.",
                    closeWindow: true);
            }

            return method switch
            {
                RentalPaymentMethod.OutOfPocket => await HandleDirectPaymentAsync(player, personaId, activeSession, latest)
                    .ConfigureAwait(false),
                RentalPaymentMethod.CoinhouseAccount => await HandleCoinhousePaymentAsync(player, personaId, activeSession, latest)
                    .ConfigureAwait(false),
                _ => RentStallSubmissionResult.Error("Unsupported payment method.")
            };
        }
        catch (Exception ex)
        {
            Log.Error(ex,
                "Unexpected error while processing stall claim selection for persona {PersonaId}.",
                personaId);

            await SendServerMessageAsync(player,
                    "Something went wrong while processing the stall claim. Please try again.",
                    ColorConstants.Red)
                .ConfigureAwait(false);

            return RentStallSubmissionResult.Error(
                "We couldn't process that selection. Please try again.");
        }
    }

    private async Task<RentStallSubmissionResult> HandleDirectPaymentAsync(
        NwPlayer player,
        PersonaId personaId,
        PendingClaimSession session,
        PlayerStall stall)
    {
        if (!session.AllowsDirect)
        {
            return RentStallSubmissionResult.Error(
                "This stall does not accept direct gold payments.");
        }

        GoldAmount availableGold = await GetPlayerGoldAsync(player).ConfigureAwait(false);
        if (!availableGold.CanAfford(session.RentCost))
        {
            RentStallPaymentOptionViewModel directUpdate = CreateDirectOptionModel(
                visible: true,
                enabled: false,
                BuildDirectShortfallMessage(session.RentCost, availableGold));

            return RentStallSubmissionResult.Error(
                "You do not have enough gold on hand to cover the first day's rent.",
                directOptionUpdate: directUpdate);
        }

        bool withdrew = await TryWithdrawGoldAsync(player, session.RentCost).ConfigureAwait(false);
        if (!withdrew)
        {
            GoldAmount refreshedGold = await GetPlayerGoldAsync(player).ConfigureAwait(false);
            RentStallPaymentOptionViewModel directUpdate = CreateDirectOptionModel(
                visible: true,
                enabled: refreshedGold.CanAfford(session.RentCost),
                BuildDirectShortfallMessage(session.RentCost, refreshedGold));

            return RentStallSubmissionResult.Error(
                "We couldn't withdraw the gold from your inventory. Please ensure you still have enough funds.",
                directOptionUpdate: directUpdate);
        }

        return await CompleteClaimAsync(
                player,
                personaId,
                session,
                stall,
                RentalPaymentMethod.OutOfPocket,
                holdEarningsInStall: true)
            .ConfigureAwait(false);
    }

    private async Task<RentStallSubmissionResult> HandleCoinhousePaymentAsync(
        NwPlayer player,
        PersonaId personaId,
        PendingClaimSession session,
        PlayerStall stall)
    {
        if (!session.AllowsCoinhouse)
        {
            return RentStallSubmissionResult.Error(
                "This stall does not accept coinhouse payments.");
        }

        if (session.CoinhouseTag is null)
        {
            RemoveSession(personaId);
            await SendServerMessageAsync(player,
                    "This stall is missing coinhouse configuration. Please notify a DM.",
                    ColorConstants.Red)
                .ConfigureAwait(false);

            return RentStallSubmissionResult.Error(
                "Coinhouse configuration is missing for this stall.",
                closeWindow: true);
        }

        if (session.CoinhouseAccountId is null)
        {
            RentStallPaymentOptionViewModel coinhouseUpdate = CreateCoinhouseOptionModel(
                session.SettlementName,
                visible: true,
                enabled: false,
                status: $"Open a coinhouse account in {DescribeSettlement(session.SettlementName)} to use this option.");

            return RentStallSubmissionResult.Error(
                "You do not have an active coinhouse account in this settlement.",
                coinhouseOptionUpdate: coinhouseUpdate);
        }

        CoinhouseAccountDto? account = await _coinhouses.GetAccountForAsync(session.CoinhouseAccountId.Value)
            .ConfigureAwait(false);

        if (account is null)
        {
            RentStallPaymentOptionViewModel coinhouseUpdate = CreateCoinhouseOptionModel(
                session.SettlementName,
                visible: true,
                enabled: false,
                status: $"We couldn't locate your coinhouse account in {DescribeSettlement(session.SettlementName)}.");

            return RentStallSubmissionResult.Error(
                "We couldn't locate your coinhouse account. Please verify it is still active.",
                coinhouseOptionUpdate: coinhouseUpdate);
        }

        string reason = BuildCoinhouseReason(session.StallName);
        CommandResult withdrawalResult;

        try
        {
            WithdrawGoldCommand command = WithdrawGoldCommand.Create(
                personaId,
                session.CoinhouseTag.Value,
                session.RentCost.Value,
                reason);

            withdrawalResult = await _withdrawHandler.HandleAsync(command).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Error(ex,
                "Failed to withdraw stall rent via coinhouse for persona {PersonaId} at {CoinhouseTag}.",
                personaId,
                session.CoinhouseTag.Value);

            withdrawalResult = CommandResult.Fail("The coinhouse could not process the withdrawal.");
        }

        if (!withdrawalResult.Success)
        {
            string message = withdrawalResult.ErrorMessage ?? "The coinhouse could not process the withdrawal.";

            RentStallPaymentOptionViewModel coinhouseUpdate = CreateCoinhouseOptionModel(
                session.SettlementName,
                visible: true,
                enabled: false,
                status: message);

            await SendServerMessageAsync(player, message, ColorConstants.Orange).ConfigureAwait(false);

            return RentStallSubmissionResult.Error(
                message,
                coinhouseOptionUpdate: coinhouseUpdate);
        }

        return await CompleteClaimAsync(
                player,
                personaId,
                session,
                stall,
                RentalPaymentMethod.CoinhouseAccount,
                holdEarningsInStall: false)
            .ConfigureAwait(false);
    }

    private async Task<RentStallSubmissionResult> CompleteClaimAsync(
        NwPlayer player,
        PersonaId personaId,
        PendingClaimSession session,
        PlayerStall stall,
        RentalPaymentMethod method,
        bool holdEarningsInStall)
    {
        try
        {
            DateTime leaseStart = DateTime.UtcNow;

            ClaimPlayerStallCommand command = ClaimPlayerStallCommand.Create(
                stall.Id,
                session.AreaResRef,
                session.PlaceableTag,
                personaId,
                session.OwnerDisplayName,
                method == RentalPaymentMethod.CoinhouseAccount ? session.CoinhouseAccountId : null,
                holdEarningsInStall,
                rentInterval: RentInterval,
                leaseStartUtc: leaseStart);

            CommandResult claimResult = await _claimHandler.HandleAsync(command).ConfigureAwait(false);
            if (!claimResult.Success)
            {
                RentStallPaymentOptionViewModel? directUpdate = null;
                RentStallPaymentOptionViewModel? coinhouseUpdate = null;

                if (method == RentalPaymentMethod.OutOfPocket)
                {
                    await TryReturnGoldAsync(player, session.RentCost).ConfigureAwait(false);
                    GoldAmount available = await GetPlayerGoldAsync(player).ConfigureAwait(false);
                    directUpdate = CreateDirectOptionModel(
                        visible: true,
                        enabled: available.CanAfford(session.RentCost),
                        status: BuildDirectShortfallMessage(session.RentCost, available));
                }
                else
                {
                    string status = $"Charge {FormatGold(session.RentCost.Value)} to your coinhouse account.";
                    coinhouseUpdate = CreateCoinhouseOptionModel(
                        session.SettlementName,
                        visible: true,
                        enabled: true,
                        status: status);
                }

                string message = claimResult.ErrorMessage ?? "Failed to claim the stall.";
                await SendServerMessageAsync(player, message, ColorConstants.Orange).ConfigureAwait(false);

                return RentStallSubmissionResult.Error(
                    message,
                    closeWindow: false,
                    directOptionUpdate: directUpdate,
                    coinhouseOptionUpdate: coinhouseUpdate);
            }

            RemoveSession(personaId);

            string formattedRent = FormatGold(session.RentCost.Value);
            await SendServerMessageAsync(player,
                    $"You now manage {session.StallName}. Next rent of {formattedRent} is due in 24 hours.",
                    ColorConstants.Orange)
                .ConfigureAwait(false);

            if (method == RentalPaymentMethod.OutOfPocket)
            {
                await SendServerMessageAsync(player,
                        $"Paid {formattedRent} from your carried gold.",
                        ColorConstants.Orange)
                    .ConfigureAwait(false);
            }
            else
            {
                await SendServerMessageAsync(player,
                        $"Charged {formattedRent} to your coinhouse account.",
                        ColorConstants.Orange)
                    .ConfigureAwait(false);
            }

            await SendFloatingTextAsync(player, "You have claimed this stall!").ConfigureAwait(false);

            Log.Info(
                "Persona {PersonaId} claimed stall {StallId} ({Tag}) via {Method} payment.",
                personaId,
                stall.Id,
                stall.Tag,
                method);

            return RentStallSubmissionResult.SuccessResult(
                "Stall claimed successfully.",
                closeWindow: true);
        }
        catch (Exception ex)
        {
            Log.Error(ex,
                "Failed to finalize stall claim for stall {StallId} and persona {PersonaId}.",
                stall.Id,
                personaId);

            if (method == RentalPaymentMethod.OutOfPocket)
            {
                await TryReturnGoldAsync(player, session.RentCost).ConfigureAwait(false);
            }

            RemoveSession(personaId);

            await SendServerMessageAsync(player,
                    "We couldn't finalize the stall claim. Please try again or contact a DM.",
                    ColorConstants.Red)
                .ConfigureAwait(false);

            return RentStallSubmissionResult.Error(
                "The stall claim could not be completed.",
                closeWindow: true);
        }
    }

    private static bool IsStallAvailable(PlayerStall stall) =>
        stall.OwnerCharacterId is null && string.IsNullOrWhiteSpace(stall.OwnerPersonaId);

    private static RentStallPaymentOptionViewModel? BuildDirectOption(
        GoldAmount availableGold,
        GoldAmount rentCost,
        string formattedRent)
    {
        if (rentCost.Value <= 0)
        {
            return CreateDirectOptionModel(true, true, "There is no upfront rent for this stall.");
        }

        bool hasFunds = availableGold.CanAfford(rentCost);
        string status = hasFunds
            ? $"Pay {formattedRent} from your carried gold."
            : BuildDirectShortfallMessage(rentCost, availableGold);

        return CreateDirectOptionModel(true, hasFunds, status);
    }

    private async Task<(RentStallPaymentOptionViewModel? Option, CoinhouseTag? Tag, Guid? AccountId)> BuildCoinhouseOptionAsync(
        PlayerStall stall,
        PersonaId personaId,
        GoldAmount rentCost,
        string formattedRent,
        string? settlementName)
    {
        if (string.IsNullOrWhiteSpace(stall.SettlementTag))
        {
            RentStallPaymentOptionViewModel option = CreateCoinhouseOptionModel(
                settlementName,
                visible: true,
                enabled: false,
                status: "This stall is not linked to a coinhouse yet. Please notify a DM.");

            return (option, null, null);
        }

        CoinhouseTag tag;
        try
        {
            tag = CoinhouseTag.Parse(stall.SettlementTag);
        }
        catch (Exception ex)
        {
            Log.Warn(ex,
                "Stall {StallId} has an invalid settlement tag value {SettlementTag}.",
                stall.Id,
                stall.SettlementTag);

            RentStallPaymentOptionViewModel option = CreateCoinhouseOptionModel(
                settlementName,
                visible: true,
                enabled: false,
                status: "The linked coinhouse tag is invalid. Please notify a DM.");

            return (option, null, null);
        }

        Guid accountId = PersonaAccountId.ForCoinhouse(personaId, tag);
        CoinhouseAccountDto? account = await _coinhouses.GetAccountForAsync(accountId).ConfigureAwait(false);

        if (account is null)
        {
            RentStallPaymentOptionViewModel option = CreateCoinhouseOptionModel(
                settlementName,
                visible: true,
                enabled: false,
                status: $"Open a coinhouse account in {DescribeSettlement(settlementName)} to use this option.");

            return (option, tag, null);
        }

        RentStallPaymentOptionViewModel enabledOption = CreateCoinhouseOptionModel(
            settlementName,
            visible: true,
            enabled: true,
            status: $"Charge {formattedRent} to your coinhouse account.");

        return (enabledOption, tag, account.Id);
    }

    private static RentStallPaymentOptionViewModel CreateDirectOptionModel(bool visible, bool enabled, string status) =>
        new(RentalPaymentMethod.OutOfPocket, "Pay from Carried Gold", visible, enabled, status, status);

    private static RentStallPaymentOptionViewModel CreateCoinhouseOptionModel(
        string? settlementName,
        bool visible,
        bool enabled,
        string status)
    {
        string label = !string.IsNullOrWhiteSpace(settlementName)
            ? $"Pay via {settlementName} Coinhouse"
            : "Pay from Coinhouse";

        return new RentStallPaymentOptionViewModel(
            RentalPaymentMethod.CoinhouseAccount,
            label,
            visible,
            enabled,
            status,
            status);
    }

    private static string BuildDirectShortfallMessage(GoldAmount rentCost, GoldAmount availableGold)
    {
        int shortfall = Math.Max(rentCost.Value - availableGold.Value, 0);
        if (shortfall <= 0)
        {
            return "Pay the rent from your carried gold.";
        }

        return $"You need {FormatGold(shortfall)} more gold on hand.";
    }

    private void RemoveSession(PersonaId personaId)
    {
        _activeSessions.TryRemove(personaId, out _);
    }

    private Task OnRentWindowCancelledAsync(PersonaId personaId)
    {
        RemoveSession(personaId);
        return Task.CompletedTask;
    }

    private async Task OnRentWindowTimedOutAsync(NwPlayer player, PersonaId personaId, string stallName)
    {
        RemoveSession(personaId);
        await SendServerMessageAsync(player,
                $"The leasing offer for {stallName} has expired.",
                ColorConstants.Orange)
            .ConfigureAwait(false);
    }

    private Task OnRentWindowClosedAsync(PersonaId personaId)
    {
        RemoveSession(personaId);
        return Task.CompletedTask;
    }

    private static async Task<GoldAmount> GetPlayerGoldAsync(NwPlayer player)
    {
        await NwTask.SwitchToMainThread();

        if (!player.IsValid)
        {
            return GoldAmount.Zero;
        }

        NwCreature? creature = player.ControlledCreature ?? player.LoginCreature;
        if (creature is null || !creature.IsValid)
        {
            return GoldAmount.Zero;
        }

        uint rawGold = creature.Gold;
        int normalized = rawGold > int.MaxValue ? int.MaxValue : (int)rawGold;
        return GoldAmount.Parse(normalized);
    }

    private static async Task<bool> TryWithdrawGoldAsync(NwPlayer player, GoldAmount amount)
    {
        await NwTask.SwitchToMainThread();

        if (!player.IsValid)
        {
            return false;
        }

        NwCreature? creature = player.ControlledCreature ?? player.LoginCreature;
        if (creature is null || !creature.IsValid)
        {
            return false;
        }

        if (creature.Gold < (uint)amount.Value)
        {
            return false;
        }

        creature.Gold -= (uint)amount.Value;
        return true;
    }

    private static async Task TryReturnGoldAsync(NwPlayer player, GoldAmount amount)
    {
        await NwTask.SwitchToMainThread();

        if (!player.IsValid)
        {
            return;
        }

        NwCreature? creature = player.ControlledCreature ?? player.LoginCreature;
        if (creature is null || !creature.IsValid)
        {
            return;
        }

        creature.Gold += (uint)amount.Value;
    }

    private static async Task SendServerMessageAsync(NwPlayer player, string message, Color color)
    {
        await NwTask.SwitchToMainThread();

        if (player.IsValid)
        {
            player.SendServerMessage(message, color);
        }
    }

    private static async Task SendFloatingTextAsync(NwPlayer player, string message)
    {
        await NwTask.SwitchToMainThread();

        if (player.IsValid)
        {
            player.FloatingTextString(message, false);
        }
    }

    private static string ResolveOwnerDisplayName(NwPlayer player)
    {
        NwCreature? creature = player.LoginCreature ?? player.ControlledCreature;
        if (creature is not null && creature.IsValid && !string.IsNullOrWhiteSpace(creature.Name))
        {
            return creature.Name;
        }

        return string.IsNullOrWhiteSpace(player.PlayerName) ? "Unknown Proprietor" : player.PlayerName;
    }

    private static string ResolveStallName(PlayerStall stall, string? areaName)
    {
        if (!string.IsNullOrWhiteSpace(stall.Tag))
        {
            return BeautifyLabel(stall.Tag);
        }

        return !string.IsNullOrWhiteSpace(areaName)
            ? $"Market Stall ({areaName})"
            : $"Market Stall #{stall.Id}";
    }

    private static string ResolveStallDescription(PlayerStall stall, string? areaName)
    {
        string location = string.IsNullOrWhiteSpace(areaName) ? "the marketplace" : areaName;
        return $"A vacant market stall located in {location}. Claiming the stall charges the first day's rent immediately.";
    }

    private static string? ResolveSettlementName(string? settlementTag, string? areaName)
    {
        if (!string.IsNullOrWhiteSpace(settlementTag))
        {
            return BeautifyLabel(settlementTag);
        }

        return string.IsNullOrWhiteSpace(areaName) ? null : areaName;
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

    private static string DescribeSettlement(string? settlementName) =>
        string.IsNullOrWhiteSpace(settlementName) ? "this settlement" : settlementName;

    private static string BuildCoinhouseReason(string stallName)
    {
        string baseReason = string.IsNullOrWhiteSpace(stallName)
            ? "Initial stall rent payment"
            : $"Initial stall rent payment for {stallName}";

        if (baseReason.Length < 3)
        {
            baseReason = "Stall rent";
        }

        return baseReason.Length <= 200 ? baseReason : baseReason[..200];
    }

    private static string FormatGold(int amount) =>
        Math.Max(amount, 0).ToString("N0", CultureInfo.InvariantCulture);

    private sealed record PendingClaimSession(
        long StallId,
        string PlaceableTag,
        string AreaResRef,
        GoldAmount RentCost,
        DateTimeOffset CreatedAt,
        string StallName,
        string? SettlementName,
        string OwnerDisplayName,
        bool AllowsDirect,
        bool AllowsCoinhouse,
        CoinhouseTag? CoinhouseTag,
        Guid? CoinhouseAccountId);
}
