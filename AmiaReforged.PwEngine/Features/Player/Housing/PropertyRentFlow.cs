using System;
using System.Collections.Concurrent;
using System.Globalization;
using System.Threading.Tasks;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Features.WorldEngine;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Properties;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Properties.Nui;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.ValueObjects;
using Anvil.API;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.Player.Housing;

[ServiceBinding(typeof(PropertyRentFlow))]
public sealed class PropertyRentFlow
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private static readonly TimeSpan RentalConfirmationTimeout = TimeSpan.FromSeconds(60);
    private static readonly PropertyRentalPolicy RentalPolicy = new();
    private static readonly GoldAmount HouseSize1Rent = GoldAmount.Parse(50_000);
    private static readonly GoldAmount HouseSize2Rent = GoldAmount.Parse(120_000);
    private static readonly GoldAmount HouseSize3Rent = GoldAmount.Parse(300_000);

    private readonly IRentablePropertyRepository _properties;
    private readonly IRentalPaymentCapabilityService _paymentCapabilities;
    private readonly WindowDirector _windowDirector;
    private readonly IWorldEngineFacade _worldEngine;

    private readonly ConcurrentDictionary<PersonaId, PendingRentSession> _activeRentSessions = new();

    public PropertyRentFlow(
        IRentablePropertyRepository properties,
        IRentalPaymentCapabilityService paymentCapabilities,
        WindowDirector windowDirector,
        IWorldEngineFacade worldEngine)
    {
        _properties = properties;
        _paymentCapabilities = paymentCapabilities;
        _windowDirector = windowDirector;
        _worldEngine = worldEngine;
    }

    internal async Task HandleVacantPropertyInteractionAsync(
        NwDoor door,
        NwPlayer player,
        PersonaId personaId,
        PropertyId propertyId,
        RentablePropertySnapshot propertySnapshot,
        RentOfferPresentation presentation)
    {
        GoldAmount? configuredRent = await ResolveDoorRentAsync(door).ConfigureAwait(false);
        if (configuredRent is null)
        {
            Log.Warn("Door {DoorTag} is missing a valid house_size local variable for rental pricing.", door.Tag);
            await PlayerHouseService.ShowFloatingTextAsync(player,
                "This property is not configured for rental yet. Please notify a DM.").ConfigureAwait(false);
            return;
        }

        RentablePropertyDefinition definitionWithRent = propertySnapshot.Definition with
        {
            MonthlyRent = configuredRent.Value
        };

        RentablePropertySnapshot snapshotWithRent = propertySnapshot with
        {
            Definition = definitionWithRent
        };

        GoldAmount availableGold = await GetPlayerGoldAsync(player).ConfigureAwait(false);
        DateOnly evaluationDate = DateOnly.FromDateTime(DateTime.UtcNow);

        RentPropertyRequest capabilityRequest =
            new(personaId, propertyId, RentalPaymentMethod.OutOfPocket, evaluationDate);
        PaymentCapabilitySnapshot capabilities =
            await _paymentCapabilities.EvaluateAsync(capabilityRequest, snapshotWithRent).ConfigureAwait(false);

        string formattedRent = FormatGold(configuredRent.Value);
        string rentCostText = $"Monthly rent: {formattedRent} gold";

        RentPropertyPaymentOptionViewModel? directOption = BuildDirectOption(
            snapshotWithRent.Definition.AllowsDirectRental,
            capabilities.HasSufficientDirectFunds,
            availableGold,
            configuredRent.Value,
            formattedRent);

        RentPropertyPaymentOptionViewModel? coinhouseOption = BuildCoinhouseOption(
            snapshotWithRent.Definition.AllowsCoinhouseRental,
            snapshotWithRent.Definition.SettlementCoinhouseTag,
            capabilities.HasSettlementCoinhouseAccount,
            formattedRent,
            presentation.SettlementName);

        if (directOption is null && coinhouseOption is null)
        {
            Log.Warn("Property {PropertyId} does not permit any rental payment methods.", propertyId);
            await PlayerHouseService.SendServerMessageAsync(player,
                    "This property is not currently available for player rental. Please notify a DM.",
                    ColorConstants.Orange)
                .ConfigureAwait(false);
            return;
        }

        RemoveSession(personaId);

        RentPropertyWindowConfig config = new(
            Title: presentation.DisplayName,
            PropertyName: presentation.DisplayName,
            PropertyDescription: BuildPropertyDescription(presentation.Description),
            RentCostText: rentCostText,
            Timeout: RentalConfirmationTimeout,
            DirectOption: directOption,
            CoinhouseOption: coinhouseOption,
            OnConfirm: method => ProcessRentalSelectionAsync(player, personaId, method))
        {
            SettlementName = presentation.SettlementName,
            OnCancel = () => OnRentWindowCancelledAsync(personaId),
            OnTimeout = () => OnRentWindowTimedOutAsync(player, personaId, presentation.DisplayName),
            OnClosed = () => OnRentWindowClosedAsync(personaId)
        };

        PendingRentSession session = new(
            propertyId,
            configuredRent.Value,
            door,
            DateTimeOffset.UtcNow,
            presentation.DisplayName,
            presentation.Description,
            presentation.SettlementName,
            snapshotWithRent.Definition.AllowsDirectRental,
            snapshotWithRent.Definition.AllowsCoinhouseRental,
            snapshotWithRent.Definition.SettlementCoinhouseTag);

        RentPropertyWindowView view = new(player, config);

        await NwTask.SwitchToMainThread();
        _windowDirector.CloseWindow(player, typeof(RentPropertyWindowPresenter));
        _activeRentSessions[personaId] = session;
        _windowDirector.OpenWindow(view.Presenter);

        await PlayerHouseService.SendServerMessageAsync(player,
                $"Review the rental agreement for {presentation.DisplayName}.",
                ColorConstants.Orange)
            .ConfigureAwait(false);
    }

    private async Task<RentPropertySubmissionResult> ProcessRentalSelectionAsync(
        NwPlayer player,
        PersonaId personaId,
        RentalPaymentMethod method)
    {
        try
        {
            if (!_activeRentSessions.TryGetValue(personaId, out PendingRentSession? session))
            {
                await PlayerHouseService.SendServerMessageAsync(player,
                        "The rental offer has expired. Please interact with the property again.",
                        ColorConstants.Orange)
                    .ConfigureAwait(false);

                return RentPropertySubmissionResult.Error(
                    "The rental offer is no longer available.",
                    closeWindow: true);
            }

            PendingRentSession activeSession = session;

            if (DateTimeOffset.UtcNow - activeSession.CreatedAt > RentalConfirmationTimeout)
            {
                RemoveSession(personaId);
                await PlayerHouseService.SendServerMessageAsync(player,
                        "The rental offer has expired. Please interact with the door again.",
                        ColorConstants.Orange)
                    .ConfigureAwait(false);

                return RentPropertySubmissionResult.Error(
                    "This rental offer has expired.",
                    closeWindow: true);
            }

            RentablePropertySnapshot? latest = await _properties.GetSnapshotAsync(activeSession.PropertyId)
                .ConfigureAwait(false);
            if (latest is null)
            {
                RemoveSession(personaId);
                await PlayerHouseService.SendServerMessageAsync(player,
                        "We couldn't load the housing record for this property. Please try again later.",
                        ColorConstants.Red)
                    .ConfigureAwait(false);

                return RentPropertySubmissionResult.Error(
                    "The housing record could not be loaded.",
                    closeWindow: true);
            }

            if (latest.OccupancyStatus != PropertyOccupancyStatus.Vacant)
            {
                RemoveSession(personaId);
                await PlayerHouseService.SendServerMessageAsync(player,
                        "This property has already been claimed by someone else.",
                        ColorConstants.Orange)
                    .ConfigureAwait(false);

                return RentPropertySubmissionResult.Error(
                    "This property was just claimed by another player.",
                    closeWindow: true);
            }

            RentablePropertyDefinition definitionWithRent = latest.Definition with
            {
                MonthlyRent = activeSession.RentCost
            };

            RentablePropertySnapshot workingSnapshot = latest with
            {
                Definition = definitionWithRent
            };

            return method switch
            {
                RentalPaymentMethod.OutOfPocket => await HandleDirectRentalAsync(player, personaId, activeSession,
                    workingSnapshot).ConfigureAwait(false),
                RentalPaymentMethod.CoinhouseAccount => await HandleCoinhouseRentalAsync(player, personaId,
                        activeSession, workingSnapshot)
                    .ConfigureAwait(false),
                _ => RentPropertySubmissionResult.Error("Unsupported payment method.", closeWindow: false)
            };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unexpected error while processing rental selection for persona {PersonaId}.", personaId);
            await PlayerHouseService.SendServerMessageAsync(player,
                    "Something went wrong while processing the rental. Please try again.",
                    ColorConstants.Red)
                .ConfigureAwait(false);

            return RentPropertySubmissionResult.Error(
                "We couldn't process that selection. Please try again.",
                closeWindow: false);
        }
    }

    private async Task<RentPropertySubmissionResult> HandleDirectRentalAsync(
        NwPlayer player,
        PersonaId personaId,
        PendingRentSession session,
        RentablePropertySnapshot snapshot)
    {
        if (!session.AllowsDirectRental)
        {
            return RentPropertySubmissionResult.Error(
                "This property does not accept direct gold payments.",
                closeWindow: false);
        }

        GoldAmount availableGold = await GetPlayerGoldAsync(player).ConfigureAwait(false);
        if (!availableGold.CanAfford(session.RentCost))
        {
            RentPropertyPaymentOptionViewModel directUpdate = CreateDirectOptionModel(
                visible: true,
                enabled: false,
                BuildDirectShortfallMessage(session.RentCost, availableGold));

            return RentPropertySubmissionResult.Error(
                "You do not have enough gold on hand to cover the first month's rent.",
                closeWindow: false,
                directOptionUpdate: directUpdate);
        }

        bool withdrew = await TryWithdrawGoldAsync(player, session.RentCost).ConfigureAwait(false);
        if (!withdrew)
        {
            GoldAmount refreshedGold = await GetPlayerGoldAsync(player).ConfigureAwait(false);
            RentPropertyPaymentOptionViewModel directUpdate = CreateDirectOptionModel(
                visible: true,
                enabled: refreshedGold.CanAfford(session.RentCost),
                BuildDirectShortfallMessage(session.RentCost, refreshedGold));

            return RentPropertySubmissionResult.Error(
                "We couldn't withdraw the gold from your inventory. Please ensure you still have enough funds.",
                closeWindow: false,
                directOptionUpdate: directUpdate);
        }

        return await CompleteRentalAsync(player, personaId, session, snapshot, RentalPaymentMethod.OutOfPocket)
            .ConfigureAwait(false);
    }

    private async Task<RentPropertySubmissionResult> HandleCoinhouseRentalAsync(
        NwPlayer player,
        PersonaId personaId,
        PendingRentSession session,
        RentablePropertySnapshot snapshot)
    {
        if (!session.AllowsCoinhouseRental)
        {
            return RentPropertySubmissionResult.Error(
                "This property does not accept coinhouse payments.",
                closeWindow: false);
        }

        if (session.CoinhouseTag is null)
        {
            RemoveSession(personaId);
            await PlayerHouseService.SendServerMessageAsync(player,
                    "This property is missing coinhouse configuration. Please notify a DM.",
                    ColorConstants.Red)
                .ConfigureAwait(false);

            return RentPropertySubmissionResult.Error(
                "Coinhouse configuration is missing for this property.",
                closeWindow: true);
        }

        string reason = BuildCoinhouseReason(session.PropertyDisplayName);
        CommandResult withdrawalResult;

        try
        {
            WithdrawGoldCommand command = WithdrawGoldCommand.Create(
                personaId,
                session.CoinhouseTag.Value,
                session.RentCost.Value,
                reason);

            withdrawalResult = await _worldEngine.ExecuteAsync(command).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            Log.Error(ex,
                "Failed to withdraw rent via coinhouse for persona {PersonaId} at {CoinhouseTag}.",
                personaId,
                session.CoinhouseTag.Value);

            withdrawalResult = CommandResult.Fail("The coinhouse could not process the withdrawal.");
        }

        if (!withdrawalResult.Success)
        {
            string message = withdrawalResult.ErrorMessage ?? "The coinhouse could not process the withdrawal.";

            RentPropertyPaymentOptionViewModel coinhouseUpdate = CreateCoinhouseOptionModel(
                session.SettlementName,
                visible: true,
                enabled: false,
                status: message);

            await PlayerHouseService.SendServerMessageAsync(player, message, ColorConstants.Orange)
                .ConfigureAwait(false);

            return RentPropertySubmissionResult.Error(
                message,
                closeWindow: false,
                coinhouseOptionUpdate: coinhouseUpdate);
        }

        return await CompleteRentalAsync(player, personaId, session, snapshot, RentalPaymentMethod.CoinhouseAccount)
            .ConfigureAwait(false);
    }

    private async Task<RentPropertySubmissionResult> CompleteRentalAsync(
        NwPlayer player,
        PersonaId personaId,
        PendingRentSession session,
        RentablePropertySnapshot snapshot,
        RentalPaymentMethod method)
    {
        try
        {
            DateOnly startDate = DateOnly.FromDateTime(DateTime.UtcNow);
            DateOnly nextDueDate = RentalPolicy.CalculateNextDueDate(startDate);

            RentablePropertyDefinition updatedDefinition = snapshot.Definition with
            {
                MonthlyRent = session.RentCost
            };

            RentalAgreementSnapshot agreement = new(
                personaId,
                startDate,
                nextDueDate,
                session.RentCost,
                method,
                null);

            RentablePropertySnapshot updatedSnapshot = snapshot with
            {
                Definition = updatedDefinition,
                OccupancyStatus = PropertyOccupancyStatus.Rented,
                CurrentTenant = personaId,
                ActiveRental = agreement
            };

            await _properties.PersistRentalAsync(updatedSnapshot).ConfigureAwait(false);

            RemoveSession(personaId);

            string formattedRent = FormatGold(session.RentCost.Value);
            string dueDateText = FormatDueDate(nextDueDate);
            string baseMessage =
                $"You have rented {session.PropertyDisplayName}. Your next payment is due on {dueDateText}.";

            await PlayerHouseService.SendServerMessageAsync(player, baseMessage, ColorConstants.Orange)
                .ConfigureAwait(false);

            if (method == RentalPaymentMethod.OutOfPocket)
            {
                await PlayerHouseService.SendServerMessageAsync(player,
                        $"Paid {formattedRent} from your carried gold.",
                        ColorConstants.Orange)
                    .ConfigureAwait(false);
            }
            else
            {
                await PlayerHouseService.SendServerMessageAsync(player,
                        $"Charged {formattedRent} to your coinhouse account.",
                        ColorConstants.Orange)
                    .ConfigureAwait(false);
            }

            await PlayerHouseService.ShowFloatingTextAsync(player, "You have rented this property!")
                .ConfigureAwait(false);
            await PlayerHouseService.UnlockDoorAsync(session.Door).ConfigureAwait(false);

            return RentPropertySubmissionResult.SuccessResult(
                "Rental completed successfully.",
                closeWindow: true);
        }
        catch (Exception ex)
        {
            Log.Error(ex,
                "Failed to finalize rental for property {PropertyId} and persona {PersonaId}.",
                session.PropertyId,
                personaId);

            if (method == RentalPaymentMethod.OutOfPocket)
            {
                await TryReturnGoldAsync(player, session.RentCost).ConfigureAwait(false);
            }

            RemoveSession(personaId);

            await PlayerHouseService.SendServerMessageAsync(player,
                    "We couldn't finalize the rental. Please try again or contact a DM.",
                    ColorConstants.Red)
                .ConfigureAwait(false);

            return RentPropertySubmissionResult.Error(
                "The rental could not be completed.",
                closeWindow: true);
        }
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

    private static async Task<GoldAmount?> ResolveDoorRentAsync(NwDoor door)
    {
        await NwTask.SwitchToMainThread();

        if (!door.IsValid)
        {
            return null;
        }

        LocalVariableInt sizeVariable = door.GetObjectVariable<LocalVariableInt>("house_size");
        if (!sizeVariable.HasValue)
        {
            return null;
        }

        return sizeVariable.Value switch
        {
            1 => HouseSize1Rent,
            2 => HouseSize2Rent,
            3 => HouseSize3Rent,
            _ => null
        };
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

    private static string BuildPropertyDescription(string? description) =>
        string.IsNullOrWhiteSpace(description)
            ? "No description is available for this property."
            : description.Trim();

    private static RentPropertyPaymentOptionViewModel? BuildDirectOption(
        bool allowsDirectRental,
        bool hasDirectFunds,
        GoldAmount availableGold,
        GoldAmount rentCost,
        string formattedRent)
    {
        if (!allowsDirectRental)
        {
            return null;
        }

        string status = hasDirectFunds
            ? $"Pay {formattedRent} from your carried gold."
            : BuildDirectShortfallMessage(rentCost, availableGold);

        return CreateDirectOptionModel(visible: true, enabled: hasDirectFunds, status);
    }

    private static RentPropertyPaymentOptionViewModel
        CreateDirectOptionModel(bool visible, bool enabled, string status) =>
        new(RentalPaymentMethod.OutOfPocket, "Pay from Pockets", visible, enabled, status, status);

    private static string BuildDirectShortfallMessage(GoldAmount rentCost, GoldAmount availableGold)
    {
        int shortfall = Math.Max(rentCost.Value - availableGold.Value, 0);
        return shortfall <= 0
            ? "Pay the rent from your carried gold."
            : $"You need {FormatGold(shortfall)} more gold on hand.";
    }

    private static RentPropertyPaymentOptionViewModel? BuildCoinhouseOption(
        bool allowsCoinhouseRental,
        CoinhouseTag? coinhouseTag,
        bool hasAccount,
        string formattedRent,
        string? settlementName)
    {
        if (!allowsCoinhouseRental)
        {
            return null;
        }

        if (coinhouseTag is null)
        {
            return CreateCoinhouseOptionModel(
                settlementName,
                visible: true,
                enabled: false,
                status: "This property is missing a linked coinhouse. Please notify a DM.");
        }

        if (!hasAccount)
        {
            string settlementDescription = DescribeSettlement(settlementName);
            return CreateCoinhouseOptionModel(
                settlementName,
                visible: true,
                enabled: false,
                status: $"Open or join a coinhouse account in {settlementDescription} to use this option.");
        }

        return CreateCoinhouseOptionModel(
            settlementName,
            visible: true,
            enabled: true,
            status: $"Charge {formattedRent} to your coinhouse account.");
    }

    private static RentPropertyPaymentOptionViewModel CreateCoinhouseOptionModel(
        string? settlementName,
        bool visible,
        bool enabled,
        string status)
    {
        string label = !string.IsNullOrWhiteSpace(settlementName)
            ? $"Pay via Coinhouse"
            : "Pay from Coinhouse";

        return new RentPropertyPaymentOptionViewModel(
            RentalPaymentMethod.CoinhouseAccount,
            label,
            visible,
            enabled,
            status,
            status);
    }

    private static string DescribeSettlement(string? settlementName) =>
        string.IsNullOrWhiteSpace(settlementName) ? "this settlement" : settlementName;

    private static string BuildCoinhouseReason(string propertyDisplayName)
    {
        string baseReason = string.IsNullOrWhiteSpace(propertyDisplayName)
            ? "Initial housing rent payment"
            : $"Initial rent payment for {propertyDisplayName}";

        if (baseReason.Length < 3)
        {
            baseReason = "Housing rent";
        }

        return baseReason.Length <= 200 ? baseReason : baseReason[..200];
    }

    private static string FormatDueDate(DateOnly date) =>
        date.ToString("MMMM d", CultureInfo.InvariantCulture);

    private static string FormatGold(GoldAmount amount) =>
        FormatGold(amount.Value);

    private static string FormatGold(int amount) =>
        Math.Max(amount, 0).ToString("N0", CultureInfo.InvariantCulture);

    private void RemoveSession(PersonaId personaId)
    {
        _activeRentSessions.TryRemove(personaId, out _);
    }

    private Task OnRentWindowCancelledAsync(PersonaId personaId)
    {
        RemoveSession(personaId);
        return Task.CompletedTask;
    }

    private async Task OnRentWindowTimedOutAsync(NwPlayer player, PersonaId personaId, string propertyName)
    {
        RemoveSession(personaId);
        await PlayerHouseService.SendServerMessageAsync(player,
                $"The rental offer for {propertyName} has expired.",
                ColorConstants.Orange)
            .ConfigureAwait(false);
    }

    private Task OnRentWindowClosedAsync(PersonaId personaId)
    {
        RemoveSession(personaId);
        return Task.CompletedTask;
    }

    internal sealed record RentOfferPresentation(string DisplayName, string? Description, string? SettlementName);

    private sealed record PendingRentSession(
        PropertyId PropertyId,
        GoldAmount RentCost,
        NwDoor Door,
        DateTimeOffset CreatedAt,
        string PropertyDisplayName,
        string? PropertyDescription,
        string? SettlementName,
        bool AllowsDirectRental,
        bool AllowsCoinhouseRental,
        CoinhouseTag? CoinhouseTag);
}
