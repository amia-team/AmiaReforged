using System;
using System.Globalization;
using System.Threading.Tasks;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Features.WorldEngine;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Properties;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Properties.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Properties.Nui;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.ValueObjects;
using Anvil.API;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.Player.Housing;

[ServiceBinding(typeof(PropertyRentPaymentFlow))]
public sealed class PropertyRentPaymentFlow
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly IRentablePropertyRepository _properties;
    private readonly IRentalPaymentCapabilityService _paymentCapabilities;
    private readonly WindowDirector _windowDirector;
    private readonly IWorldEngineFacade _worldEngine;

    public PropertyRentPaymentFlow(
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

    internal async Task HandleRentPaymentRequestAsync(
        NwPlayer player,
        PersonaId personaId,
        PropertyId propertyId,
        RentablePropertySnapshot propertySnapshot,
        string propertyDisplayName,
        string? propertyDescription,
        string? settlementName)
    {
        // Verify this is a rented property with active rental
        if (propertySnapshot.OccupancyStatus != PropertyOccupancyStatus.Rented)
        {
            await PlayerHouseService.SendServerMessageAsync(player,
                    "This property is not currently rented.",
                    ColorConstants.Orange)
                .ConfigureAwait(false);
            return;
        }

        if (propertySnapshot.ActiveRental is null)
        {
            await PlayerHouseService.SendServerMessageAsync(player,
                    "This property has no active rental agreement.",
                    ColorConstants.Orange)
                .ConfigureAwait(false);
            return;
        }

        // Verify the player is the tenant
        if (!propertySnapshot.ActiveRental.Tenant.Equals(personaId))
        {
            await PlayerHouseService.SendServerMessageAsync(player,
                    "You are not the tenant of this property.",
                    ColorConstants.Orange)
                .ConfigureAwait(false);
            return;
        }

        GoldAmount rentAmount = propertySnapshot.Definition.MonthlyRent;
        GoldAmount availableGold = await GetPlayerGoldAsync(player).ConfigureAwait(false);

        // Check payment capabilities
        RentPropertyRequest capabilityRequest =
            new(personaId, propertyId, RentalPaymentMethod.OutOfPocket, DateOnly.FromDateTime(DateTime.UtcNow));
        PaymentCapabilitySnapshot capabilities =
            await _paymentCapabilities.EvaluateAsync(capabilityRequest, propertySnapshot).ConfigureAwait(false);

        string formattedRent = FormatGold(rentAmount.Value);
        string currentDueText = $"Current due date: {FormatDueDate(propertySnapshot.ActiveRental.NextPaymentDueDate)}";
        string newDueText =
            $"After payment, next due: {FormatDueDate(propertySnapshot.ActiveRental.NextPaymentDueDate.AddMonths(1))}";

        PayRentPaymentOptionViewModel? directOption = BuildDirectOption(
            propertySnapshot.Definition.AllowsDirectRental,
            capabilities.HasSufficientDirectFunds,
            availableGold,
            rentAmount,
            formattedRent);

        PayRentPaymentOptionViewModel? coinhouseOption = BuildCoinhouseOption(
            propertySnapshot.Definition.AllowsCoinhouseRental,
            propertySnapshot.Definition.SettlementCoinhouseTag,
            capabilities.HasSettlementCoinhouseAccount,
            formattedRent,
            settlementName);

        if (directOption is null && coinhouseOption is null)
        {
            Log.Warn("Property {PropertyId} does not permit any rental payment methods.", propertyId);
            await PlayerHouseService.SendServerMessageAsync(player,
                    "This property is not configured for rent payment. Please notify a DM.",
                    ColorConstants.Orange)
                .ConfigureAwait(false);
            return;
        }

        PayRentWindowConfig config = new(
            Title: $"Pay Rent: {propertyDisplayName}",
            PropertyName: propertyDisplayName,
            PropertyDescription: BuildPropertyDescription(propertyDescription),
            RentAmountText: $"Monthly rent: {formattedRent} gold",
            CurrentDueDateText: currentDueText,
            NewDueDateText: newDueText,
            DirectOption: directOption,
            CoinhouseOption: coinhouseOption,
            OnConfirm: method => ProcessPaymentAsync(player, personaId, propertyId, propertySnapshot, method))
        {
            OnCancel = () => OnPaymentWindowCancelledAsync(player)
        };

        PayRentWindowView view = new(player, config);

        await NwTask.SwitchToMainThread();
        _windowDirector.CloseWindow(player, typeof(PayRentWindowPresenter));
        _windowDirector.OpenWindow(view.Presenter);

        await PlayerHouseService.SendServerMessageAsync(player,
                $"Review rent payment options for {propertyDisplayName}.",
                ColorConstants.Orange)
            .ConfigureAwait(false);
    }

    private async Task<PayRentSubmissionResult> ProcessPaymentAsync(
        NwPlayer player,
        PersonaId personaId,
        PropertyId propertyId,
        RentablePropertySnapshot property,
        RentalPaymentMethod method)
    {
        try
        {
            // Re-fetch property to ensure we have latest state
            RentablePropertySnapshot? latest = await _properties.GetSnapshotAsync(propertyId).ConfigureAwait(false);

            if (latest is null)
            {
                return PayRentSubmissionResult.Error(
                    "Could not load property information. Please try again.",
                    closeWindow: false);
            }

            if (latest.ActiveRental is null || !latest.ActiveRental.Tenant.Equals(personaId))
            {
                return PayRentSubmissionResult.Error(
                    "You are no longer the tenant of this property.",
                    closeWindow: true);
            }

            return method switch
            {
                RentalPaymentMethod.OutOfPocket => await HandleDirectPaymentAsync(player, personaId, latest),
                RentalPaymentMethod.CoinhouseAccount => await HandleCoinhousePaymentAsync(player, personaId, latest),
                _ => PayRentSubmissionResult.Error("Invalid payment method.", closeWindow: false)
            };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error processing rent payment for property {PropertyId}.", propertyId);
            return PayRentSubmissionResult.Error(
                "An unexpected error occurred. Please try again.",
                closeWindow: false);
        }
    }

    private async Task<PayRentSubmissionResult> HandleDirectPaymentAsync(
        NwPlayer player,
        PersonaId personaId,
        RentablePropertySnapshot property)
    {
        GoldAmount rentAmount = property.Definition.MonthlyRent;
        GoldAmount availableGold = await GetPlayerGoldAsync(player).ConfigureAwait(false);

        if (availableGold < rentAmount)
        {
            string message =
                $"You need {FormatGold(rentAmount.Value)} gold but only have {FormatGold(availableGold.Value)}.";

            PayRentPaymentOptionViewModel directUpdate = CreateDirectOptionModel(
                visible: true,
                enabled: false,
                status: "Insufficient funds");

            await PlayerHouseService.SendServerMessageAsync(player, message, ColorConstants.Orange)
                .ConfigureAwait(false);

            return PayRentSubmissionResult.Error(
                message,
                closeWindow: false,
                directOptionUpdate: directUpdate);
        }

        bool withdrawn = await TryWithdrawGoldAsync(player, rentAmount).ConfigureAwait(false);

        if (!withdrawn)
        {
            string message = "Failed to withdraw gold from your inventory. Please try again.";

            await PlayerHouseService.SendServerMessageAsync(player, message, ColorConstants.Orange)
                .ConfigureAwait(false);

            return PayRentSubmissionResult.Error(
                message,
                closeWindow: false);
        }

        return await CompletePaymentAsync(player, personaId, property, RentalPaymentMethod.OutOfPocket)
            .ConfigureAwait(false);
    }

    private async Task<PayRentSubmissionResult> HandleCoinhousePaymentAsync(
        NwPlayer player,
        PersonaId personaId,
        RentablePropertySnapshot property)
    {
        if (property.Definition.SettlementCoinhouseTag is null)
        {
            string message = "This property has no associated coinhouse for payments.";
            await PlayerHouseService.SendServerMessageAsync(player, message, ColorConstants.Orange)
                .ConfigureAwait(false);

            return PayRentSubmissionResult.Error(message, closeWindow: false);
        }

        GoldAmount rentAmount = property.Definition.MonthlyRent;
        CoinhouseTag coinhouseTag = property.Definition.SettlementCoinhouseTag
                                    ?? throw new InvalidOperationException(
                                        "Property has no settlement coinhouse configured");

        WithdrawGoldCommand withdrawCommand = WithdrawGoldCommand.Create(
            personaId,
            coinhouseTag,
            rentAmount.Value,
            $"Rent payment for {property.Definition.InternalName}");

        CommandResult withdrawalResult = await _worldEngine.ExecuteAsync(withdrawCommand).ConfigureAwait(false);

        if (!withdrawalResult.Success)
        {
            string message = withdrawalResult.ErrorMessage ?? "The coinhouse could not process the withdrawal.";

            PayRentPaymentOptionViewModel coinhouseUpdate = CreateCoinhouseOptionModel(
                property.Definition.SettlementCoinhouseTag.Value.Value,
                visible: true,
                enabled: false,
                status: message);

            await PlayerHouseService.SendServerMessageAsync(player, message, ColorConstants.Orange)
                .ConfigureAwait(false);

            return PayRentSubmissionResult.Error(
                message,
                closeWindow: false,
                coinhouseOptionUpdate: coinhouseUpdate);
        }

        return await CompletePaymentAsync(player, personaId, property, RentalPaymentMethod.CoinhouseAccount)
            .ConfigureAwait(false);
    }

    private async Task<PayRentSubmissionResult> CompletePaymentAsync(
        NwPlayer player,
        PersonaId personaId,
        RentablePropertySnapshot property,
        RentalPaymentMethod method)
    {
        try
        {
            PayRentCommand payCommand = new(property, personaId, method);
            CommandResult result = await _worldEngine.ExecuteAsync(payCommand).ConfigureAwait(false);

            if (!result.Success)
            {
                // Refund the player since the payment command failed
                GoldAmount rentAmount = property.Definition.MonthlyRent;

                if (method == RentalPaymentMethod.OutOfPocket)
                {
                    await TryReturnGoldAsync(player, rentAmount).ConfigureAwait(false);
                }

                string message = result.ErrorMessage ?? "Failed to process rent payment.";
                await PlayerHouseService.SendServerMessageAsync(player, message, ColorConstants.Red)
                    .ConfigureAwait(false);

                return PayRentSubmissionResult.Error(message, closeWindow: false);
            }

            DateOnly newDueDate = (DateOnly)(result.Data?["nextDueDate"] ?? DateOnly.MinValue);
            string formattedRent = FormatGold(property.Definition.MonthlyRent.Value);
            string dueDateText = FormatDueDate(newDueDate);
            string baseMessage = $"Rent payment successful! Next payment due: {dueDateText}";

            await PlayerHouseService.SendServerMessageAsync(player, baseMessage, ColorConstants.Green)
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

            return PayRentSubmissionResult.SuccessResult(
                baseMessage,
                closeWindow: true);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to complete rent payment for property {PropertyId} ({InternalName}).",
                property.Definition.Id,
                property.Definition.InternalName);

            // Attempt refund on exception
            GoldAmount rentAmount = property.Definition.MonthlyRent;
            if (method == RentalPaymentMethod.OutOfPocket)
            {
                await TryReturnGoldAsync(player, rentAmount).ConfigureAwait(false);
            }

            string message = "Payment processing failed. Please try again.";
            await PlayerHouseService.SendServerMessageAsync(player, message, ColorConstants.Red)
                .ConfigureAwait(false);

            return PayRentSubmissionResult.Error(message, closeWindow: false);
        }
    }

    private static PayRentPaymentOptionViewModel? BuildDirectOption(
        bool propertyAllowsDirectRental,
        bool hasSufficientFunds,
        GoldAmount availableGold,
        GoldAmount rentAmount,
        string formattedRent)
    {
        if (!propertyAllowsDirectRental)
        {
            return null;
        }

        if (!hasSufficientFunds)
        {
            string statusText = $"{FormatGold(availableGold.Value)} / {formattedRent} gp";
            return CreateDirectOptionModel(
                visible: true,
                enabled: false,
                status: statusText);
        }

        return CreateDirectOptionModel(
            visible: true,
            enabled: true,
            status: $"Available: {FormatGold(availableGold.Value)} gp");
    }

    private static PayRentPaymentOptionViewModel? BuildCoinhouseOption(
        bool propertyAllowsCoinhouseRental,
        CoinhouseTag? coinhouseTag,
        bool hasCoinhouseAccount,
        string formattedRent,
        string? settlementName)
    {
        if (!propertyAllowsCoinhouseRental || coinhouseTag is null)
        {
            return null;
        }

        if (!hasCoinhouseAccount)
        {
            string statusText = $"No account in {settlementName ?? "settlement"}";
            return CreateCoinhouseOptionModel(
                coinhouseTag.Value,
                visible: true,
                enabled: false,
                status: statusText);
        }

        return CreateCoinhouseOptionModel(
            coinhouseTag.Value,
            visible: true,
            enabled: true,
            status: $"Charge: {formattedRent} gp");
    }

    private static PayRentPaymentOptionViewModel CreateDirectOptionModel(bool visible, bool enabled, string status)
    {
        return new PayRentPaymentOptionViewModel(
            RentalPaymentMethod.OutOfPocket,
            "Pay from Carried Gold",
            visible,
            enabled,
            status,
            enabled
                ? "Pay rent using gold from your inventory"
                : "You do not have enough gold");
    }

    private static PayRentPaymentOptionViewModel CreateCoinhouseOptionModel(
        string coinhouseName,
        bool visible,
        bool enabled,
        string status)
    {
        return new PayRentPaymentOptionViewModel(
            RentalPaymentMethod.CoinhouseAccount,
            "Pay from Coinhouse Account",
            visible,
            enabled,
            status,
            enabled
                ? $"Pay rent from your {coinhouseName} coinhouse account"
                : $"You need an account at {coinhouseName}");
    }

    private static async Task TryReturnGoldAsync(NwPlayer player, GoldAmount amount)
    {
        await NwTask.SwitchToMainThread();

        if (!player.IsValid)
        {
            return;
        }

        NwCreature? creature = player.LoginCreature;
        if (creature is not null)
        {
            creature.Gold += (uint)amount.Value;
        }
    }

    private static async Task<GoldAmount> GetPlayerGoldAsync(NwPlayer player)
    {
        await NwTask.SwitchToMainThread();

        if (!player.IsValid)
        {
            return GoldAmount.Zero;
        }

        NwCreature? creature = player.LoginCreature;
        return creature is not null ? GoldAmount.Parse((int)creature.Gold) : GoldAmount.Zero;
    }

    private static async Task<bool> TryWithdrawGoldAsync(NwPlayer player, GoldAmount amount)
    {
        await NwTask.SwitchToMainThread();

        if (!player.IsValid)
        {
            return false;
        }

        NwCreature? creature = player.LoginCreature;
        if (creature is null)
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
        string.IsNullOrWhiteSpace(description) ? "No description available." : description;

    private static string FormatGold(int amount) => amount.ToString("N0", CultureInfo.InvariantCulture);

    private static string FormatDueDate(DateOnly dueDate) => dueDate.ToString("MMMM d, yyyy", CultureInfo.InvariantCulture);

    private static Task OnPaymentWindowCancelledAsync(NwPlayer player)
    {
        return PlayerHouseService.SendServerMessageAsync(player,
            "Rent payment cancelled.",
            ColorConstants.Orange);
    }
}
