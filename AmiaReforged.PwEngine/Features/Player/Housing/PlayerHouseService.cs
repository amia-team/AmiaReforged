using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Linq;
using System.Threading.Tasks;
using AmiaReforged.PwEngine.Features.WindowingSystem.Scry;
using AmiaReforged.PwEngine.Features.WorldEngine.Characters.Runtime;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Properties;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Properties.Nui;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Regions;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.Player.Housing;

[ServiceBinding(typeof(PlayerHouseService))]
public class PlayerHouseService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private const string HouseDoorTag = "db_house_door";
    private const string TargetAreaTagLocalString = "target_area_tag";
    private const string PropertyIdScope = "player-housing-area";
    private const int DefaultEvictionGraceDays = 2;

    private static readonly string[] PropertyIdVariableNames =
    {
        "rentable_property_id",
        "property_id",
        "house_property_id"
    };

    private readonly RuntimeCharacterService _characters;
    private readonly IRentablePropertyRepository _properties;
    private readonly IRentalPaymentCapabilityService _paymentCapabilities;
    private readonly WindowDirector _windowDirector;
    private readonly ICommandHandler<WithdrawGoldCommand> _withdrawHandler;
    private readonly RegionIndex _regions;

    private readonly HashSet<uint> _registeredDoorIds = new();
    private readonly ConcurrentDictionary<PersonaId, PendingRentSession> _activeRentSessions = new();

    private static readonly TimeSpan RentalConfirmationTimeout = TimeSpan.FromSeconds(60);
    private static readonly PropertyRentalPolicy RentalPolicy = new();
    private static readonly GoldAmount HouseSize1Rent = GoldAmount.Parse(50_000);
    private static readonly GoldAmount HouseSize2Rent = GoldAmount.Parse(120_000);
    private static readonly GoldAmount HouseSize3Rent = GoldAmount.Parse(300_000);
        private static readonly Regex SettlementIdentifierPattern = new(@"^[a-z0-9_-]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public PlayerHouseService(
        IRentablePropertyRepository properties,
        RuntimeCharacterService characters,
        IRentalPaymentCapabilityService paymentCapabilities,
        WindowDirector windowDirector,
        ICommandHandler<WithdrawGoldCommand> withdrawHandler,
        RegionIndex regions)
    {
        _properties = properties;
        _characters = characters;
        _paymentCapabilities = paymentCapabilities;
        _windowDirector = windowDirector;
        _withdrawHandler = withdrawHandler;
        _regions = regions;

        BindHouseDoors();
        NwModule.Instance.OnModuleLoad += RegisterNewHouses;
    }

    private void BindHouseDoors()
    {
        IEnumerable<NwDoor> doors = NwObject.FindObjectsWithTag<NwDoor>(HouseDoorTag);

        foreach (NwDoor door in doors)
        {
            if (_registeredDoorIds.Add(door.ObjectId))
            {
                door.OnFailToOpen += HandlePlayerInteraction;
            }
        }
    }

    private async void RegisterNewHouses(ModuleEvents.OnModuleLoad obj)
    {
        try
        {
            BindHouseDoors();
            await EnsureHouseDefinitionsAsync();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to initialize player housing during module load.");
        }
    }

    [SuppressMessage("ReSharper", "AsyncVoidMethod")]
    private async void HandlePlayerInteraction(DoorEvents.OnFailToOpen obj)
    {
        if (!obj.WhoFailed.IsPlayerControlled(out NwPlayer? player))
        {
            return;
        }

        try
        {
            if (!TryResolvePersona(player, out PersonaId personaId))
            {
                await ShowFloatingTextAsync(player, "We couldn't verify your identity. Please relog and try again.");
                return;
            }

            string? targetAreaTag =
                obj.Door.GetObjectVariable<LocalVariableString>(TargetAreaTagLocalString).Value;

            if (string.IsNullOrWhiteSpace(targetAreaTag))
            {
                Log.Warn("Door {DoorTag} is missing the {LocalVar} local variable.", obj.Door.Tag, TargetAreaTagLocalString);
                await ShowFloatingTextAsync(player, "This door is missing its destination. Please notify a DM.");
                return;
            }

            NwArea? area = FindAreaByTag(targetAreaTag);
            if (area is null)
            {
                Log.Error("Failed to locate area with tag {AreaTag} for door {DoorTag}.", targetAreaTag, obj.Door.Tag);
                await ShowFloatingTextAsync(player, "The destination for this property could not be located. Please notify a DM.");
                return;
            }

            PropertyAreaMetadata metadata;
            try
            {
                metadata = CaptureAreaMetadata(area);
            }
            catch (Exception metaEx)
            {
                Log.Error(metaEx, "Failed to capture metadata for housing area {AreaTag}.", area.Tag);
                await ShowFloatingTextAsync(player, "This property is not configured correctly. Please notify a DM.");
                return;
            }

            if (!TryResolvePropertyId(obj.Door, metadata, out PropertyId propertyId))
            {
                await ShowFloatingTextAsync(player, "This property is missing an identifier. Please notify a DM.");
                return;
            }

            RentablePropertySnapshot? snapshot = await EnsurePropertySnapshotAsync(propertyId, metadata);
            if (snapshot is null)
            {
                await ShowFloatingTextAsync(player, "The housing record for this property could not be loaded. Please try again later.");
                return;
            }

            if (CanPlayerAccess(personaId, snapshot))
            {
                await UnlockDoorAsync(obj.Door);
                return;
            }

            if (snapshot.OccupancyStatus == PropertyOccupancyStatus.Vacant)
            {
                await HandleVacantPropertyInteractionAsync(obj.Door, player, personaId, propertyId, metadata, snapshot);
                return;
            }

            await ShowFloatingTextAsync(player, BuildAccessDeniedMessage(snapshot));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unexpected error while handling housing interaction for door {DoorTag}.", obj.Door.Tag);

            try
            {
                await ShowFloatingTextAsync(player, "Housing system encountered an error. Please try again shortly.");
            }
            catch (Exception nested)
            {
                Log.Error(nested, "Failed to send error feedback to player during housing interaction.");
            }
        }
    }

    private async Task HandleVacantPropertyInteractionAsync(
        NwDoor door,
        NwPlayer player,
        PersonaId personaId,
        PropertyId propertyId,
        PropertyAreaMetadata metadata,
        RentablePropertySnapshot propertySnapshot)
    {
        GoldAmount? configuredRent = await ResolveDoorRentAsync(door).ConfigureAwait(false);
        if (configuredRent is null)
        {
            Log.Warn("Door {DoorTag} is missing a valid house_size local variable for rental pricing.", door.Tag);
            await ShowFloatingTextAsync(player, "This property is not configured for rental yet. Please notify a DM.");
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

        GoldAmount availableGold = await GetPlayerGoldAsync(player);
        DateOnly evaluationDate = DateOnly.FromDateTime(DateTime.UtcNow);

        RentPropertyRequest capabilityRequest = new(personaId, propertyId, RentalPaymentMethod.OutOfPocket, evaluationDate);
        PaymentCapabilitySnapshot capabilities =
            await _paymentCapabilities.EvaluateAsync(capabilityRequest, snapshotWithRent);

        PropertyPresentation presentation = ResolvePropertyPresentation(metadata);
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
            await SendServerMessageAsync(player,
                "This property is not currently available for player rental. Please notify a DM.",
                ColorConstants.Orange);
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

        await SendServerMessageAsync(player,
            $"Review the rental agreement for {presentation.DisplayName}.",
            ColorConstants.Orange);
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
                await SendServerMessageAsync(player,
                    "The rental offer has expired. Please interact with the property again.",
                    ColorConstants.Orange);

                return RentPropertySubmissionResult.Error(
                    "The rental offer is no longer available.",
                    closeWindow: true);
            }

            PendingRentSession activeSession = session!;

            if (DateTimeOffset.UtcNow - activeSession.CreatedAt > RentalConfirmationTimeout)
            {
                RemoveSession(personaId);
                await SendServerMessageAsync(player,
                    "The rental offer has expired. Please interact with the door again.",
                    ColorConstants.Orange);

                return RentPropertySubmissionResult.Error(
                    "This rental offer has expired.",
                    closeWindow: true);
            }

            RentablePropertySnapshot? latest = await _properties.GetSnapshotAsync(activeSession.PropertyId);
            if (latest is null)
            {
                RemoveSession(personaId);
                await SendServerMessageAsync(player,
                    "We couldn't load the housing record for this property. Please try again later.",
                    ColorConstants.Red);

                return RentPropertySubmissionResult.Error(
                    "The housing record could not be loaded.",
                    closeWindow: true);
            }

            if (latest.OccupancyStatus != PropertyOccupancyStatus.Vacant)
            {
                RemoveSession(personaId);
                await SendServerMessageAsync(player,
                    "This property has already been claimed by someone else.",
                    ColorConstants.Orange);

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
                RentalPaymentMethod.OutOfPocket => await HandleDirectRentalAsync(player, personaId, activeSession, workingSnapshot),
                RentalPaymentMethod.CoinhouseAccount => await HandleCoinhouseRentalAsync(player, personaId, activeSession, workingSnapshot),
                _ => RentPropertySubmissionResult.Error("Unsupported payment method.", closeWindow: false)
            };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unexpected error while processing rental selection for persona {PersonaId}.", personaId);
            await SendServerMessageAsync(player,
                "Something went wrong while processing the rental. Please try again.",
                ColorConstants.Red);

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

        GoldAmount availableGold = await GetPlayerGoldAsync(player);
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

        bool withdrew = await TryWithdrawGoldAsync(player, session.RentCost);
        if (!withdrew)
        {
            GoldAmount refreshedGold = await GetPlayerGoldAsync(player);
            RentPropertyPaymentOptionViewModel directUpdate = CreateDirectOptionModel(
                visible: true,
                enabled: refreshedGold.CanAfford(session.RentCost),
                BuildDirectShortfallMessage(session.RentCost, refreshedGold));

            return RentPropertySubmissionResult.Error(
                "We couldn't withdraw the gold from your inventory. Please ensure you still have enough funds.",
                closeWindow: false,
                directOptionUpdate: directUpdate);
        }

        return await CompleteRentalAsync(player, personaId, session, snapshot, RentalPaymentMethod.OutOfPocket);
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
            await SendServerMessageAsync(player,
                "This property is missing coinhouse configuration. Please notify a DM.",
                ColorConstants.Red);

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

            withdrawalResult = await _withdrawHandler.HandleAsync(command);
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

            await SendServerMessageAsync(player, message, ColorConstants.Orange);

            return RentPropertySubmissionResult.Error(
                message,
                closeWindow: false,
                coinhouseOptionUpdate: coinhouseUpdate);
        }

        return await CompleteRentalAsync(player, personaId, session, snapshot, RentalPaymentMethod.CoinhouseAccount);
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

            await _properties.PersistRentalAsync(updatedSnapshot);

            RemoveSession(personaId);

            string formattedRent = FormatGold(session.RentCost.Value);
            string dueDateText = FormatDueDate(nextDueDate);
            string baseMessage = $"You have rented {session.PropertyDisplayName}. Your next payment is due on {dueDateText}.";

            await SendServerMessageAsync(player, baseMessage, ColorConstants.Orange);

            if (method == RentalPaymentMethod.OutOfPocket)
            {
                await SendServerMessageAsync(player,
                    $"Paid {formattedRent} from your carried gold.",
                    ColorConstants.Orange);
            }
            else
            {
                await SendServerMessageAsync(player,
                    $"Charged {formattedRent} to your coinhouse account.",
                    ColorConstants.Orange);
            }

            await ShowFloatingTextAsync(player, "You have rented this property!");
            await UnlockDoorAsync(session.Door);

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
                await TryReturnGoldAsync(player, session.RentCost);
            }

            RemoveSession(personaId);

            await SendServerMessageAsync(player,
                "We couldn't finalize the rental. Please try again or contact a DM.",
                ColorConstants.Red);

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

    private async Task EnsureHouseDefinitionsAsync()
    {
        List<PropertyAreaMetadata> metadataList;

        try
        {
            metadataList = NwModule.Instance.Areas
                .Where(IsHouseArea)
                .Select(CaptureAreaMetadata)
                .ToList();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to read housing metadata from module.");
            return;
        }

        foreach (PropertyAreaMetadata metadata in metadataList)
        {
            try
            {
                PropertyId propertyId = metadata.ExplicitPropertyId ?? DerivePropertyId(metadata.AreaTag);
                await EnsurePropertySnapshotAsync(propertyId, metadata);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to provision housing record for area {AreaTag}.", metadata.AreaTag);
            }
        }
    }

    private async Task<RentablePropertySnapshot?> EnsurePropertySnapshotAsync(
        PropertyId propertyId,
        PropertyAreaMetadata metadata)
    {
        RentablePropertySnapshot? existing = await _properties.GetSnapshotAsync(propertyId);
        if (existing is not null)
        {
            return existing;
        }

        RentablePropertyDefinition definition = BuildDefinition(propertyId, metadata);
        PropertyOccupancyStatus occupancy = metadata.DefaultOwner is null
            ? PropertyOccupancyStatus.Vacant
            : PropertyOccupancyStatus.Owned;

        RentablePropertySnapshot seed = new(
            definition,
            occupancy,
            CurrentTenant: null,
            CurrentOwner: metadata.DefaultOwner,
            Residents: Array.Empty<PersonaId>(),
            ActiveRental: null);

        await _properties.PersistRentalAsync(seed);

        Log.Info("Provisioned housing record for area {AreaTag} with property id {PropertyId}.",
            metadata.AreaTag,
            propertyId);

        return seed;
    }

    private static bool CanPlayerAccess(PersonaId personaId, RentablePropertySnapshot property)
    {
        if (property.CurrentOwner is { } owner && owner.Equals(personaId))
        {
            return true;
        }

        if (property.CurrentTenant is { } tenant && tenant.Equals(personaId))
        {
            return true;
        }

        if (property.ActiveRental is { } active && active.Tenant.Equals(personaId))
        {
            return true;
        }

        if (property.Residents.Any(resident => resident.Equals(personaId)))
        {
            return true;
        }

        return false;
    }

    private static string BuildAccessDeniedMessage(RentablePropertySnapshot property)
    {
        return property.OccupancyStatus switch
        {
            PropertyOccupancyStatus.Vacant =>
                "This property is currently vacant. Rent it to gain access.",
            PropertyOccupancyStatus.Rented =>
                "This property is currently rented by another resident.",
            PropertyOccupancyStatus.Owned =>
                "This property belongs to another owner.",
            _ => "You do not have access to this property."
        };
    }

    private static async Task ShowFloatingTextAsync(NwPlayer player, string message)
    {
        await NwTask.SwitchToMainThread();
        if (!player.IsValid)
        {
            return;
        }

        player.FloatingTextString(message, false);
    }

    private static async Task UnlockDoorAsync(NwDoor door)
    {
        await NwTask.SwitchToMainThread();

        if (!door.IsValid)
        {
            return;
        }

        bool wasLocked = door.Locked;
        door.Locked = false;
        await door.Open();

        if (!wasLocked)
        {
            return;
        }

        await NwTask.Delay(TimeSpan.FromSeconds(1));

        if (door.IsValid)
        {
            door.Locked = true;
        }
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

    private static async Task SendServerMessageAsync(NwPlayer player, string message, Color? color = null)
    {
        await NwTask.SwitchToMainThread();

        if (!player.IsValid)
        {
            return;
        }

        if (color is { } value)
        {
            player.SendServerMessage(message, value);
        }
        else
        {
            player.SendServerMessage(message);
        }
    }

    private static string FormatGold(GoldAmount amount) =>
        FormatGold(amount.Value);

    private static string FormatGold(int amount) =>
        Math.Max(amount, 0).ToString("N0", CultureInfo.InvariantCulture);

    private bool TryResolvePersona(NwPlayer player, out PersonaId personaId)
    {
        personaId = default;

        if (!_characters.TryGetPlayerKey(player, out Guid key) || key == Guid.Empty)
        {
            Log.Warn("Failed to resolve persistent key for player {PlayerName}.", player.PlayerName);
            return false;
        }

        try
        {
            CharacterId characterId = CharacterId.From(key);
            personaId = PersonaId.FromCharacter(characterId);
            return true;
        }
        catch (Exception ex)
        {
            Log.Warn(ex, "Failed to convert key {PlayerKey} into PersonaId for player {PlayerName}.",
                key,
                player.PlayerName);
            return false;
        }
    }

    private static NwArea? FindAreaByTag(string areaTag)
    {
        return NwModule.Instance.Areas
            .FirstOrDefault(area => string.Equals(area.Tag, areaTag, StringComparison.OrdinalIgnoreCase));
    }

    private bool TryResolvePropertyId(
        NwDoor door,
        PropertyAreaMetadata metadata,
        out PropertyId propertyId)
    {
        foreach (string variableName in PropertyIdVariableNames)
        {
            if (TryParsePropertyId(door.GetObjectVariable<LocalVariableString>(variableName), out propertyId))
            {
                return true;
            }
        }

        if (metadata.ExplicitPropertyId is { } explicitId)
        {
            propertyId = explicitId;
            return true;
        }

        propertyId = DerivePropertyId(metadata.AreaTag);
        return true;
    }

    private static bool TryParsePropertyId(LocalVariableString variable, out PropertyId propertyId)
    {
        propertyId = default;

        if (!variable.HasValue)
        {
            return false;
        }

        string? value = variable.Value;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (!Guid.TryParse(value, out Guid parsed) || parsed == Guid.Empty)
        {
            Log.Warn("Invalid property id '{Value}' encountered in local variable '{VarName}'.", value, variable.Name);
            return false;
        }

        propertyId = PropertyId.Parse(parsed);
        return true;
    }

    private static PropertyId? TryResolveExplicitPropertyId(NwArea area)
    {
        foreach (string variableName in PropertyIdVariableNames)
        {
            if (TryParsePropertyId(area.GetObjectVariable<LocalVariableString>(variableName), out PropertyId propertyId))
            {
                return propertyId;
            }
        }

        return null;
    }

    private static bool IsHouseArea(NwArea area)
    {
        return area.GetObjectVariable<LocalVariableInt>("is_house").Value > 0;
    }

    private static PropertyId DerivePropertyId(string areaTag)
    {
        if (string.IsNullOrWhiteSpace(areaTag))
        {
            throw new ArgumentException("Area tag cannot be empty when deriving property id.", nameof(areaTag));
        }

        Guid derived = DeterministicGuidFactory.Create(PropertyIdScope, areaTag);
        return PropertyId.Parse(derived);
    }

    private PropertyAreaMetadata CaptureAreaMetadata(NwArea area)
    {
        string? areaTag = area.Tag;
        if (string.IsNullOrWhiteSpace(areaTag))
        {
            throw new InvalidOperationException("House areas must be tagged to participate in housing.");
        }

    string areaResRef = area.ResRef ?? throw new InvalidOperationException("House areas must have a valid resref.");

    string internalName = ResolveInternalName(area);
        PropertyCategory category = ResolvePropertyCategory(area);
        SettlementTag settlement = ResolveSettlementTag(area);
        GoldAmount monthlyRent = ResolveMonthlyRent(area);
        bool allowsCoinhouse = ResolveBoolean(area, "allows_coinhouse_rental", defaultValue: false);
        bool allowsDirect = ResolveBoolean(area, "allows_direct_rental", defaultValue: true);
        CoinhouseTag? coinhouseTag = ResolveCoinhouseTag(area);
        GoldAmount? purchasePrice = ResolveOptionalGold(area, "purchase_price");
        GoldAmount? ownershipTax = ResolveOptionalGold(area, "monthly_ownership_tax");
        int evictionGraceDays = ResolveEvictionGraceDays(area);
        PersonaId? defaultOwner = ResolveDefaultOwner(area);
        PropertyId? explicitPropertyId = TryResolveExplicitPropertyId(area);

        return new PropertyAreaMetadata(
            areaTag,
            areaResRef,
            internalName,
            category,
            settlement,
            monthlyRent,
            allowsCoinhouse,
            allowsDirect,
            coinhouseTag,
            purchasePrice,
            ownershipTax,
            evictionGraceDays,
            defaultOwner,
            explicitPropertyId);
    }

    private static string ResolveInternalName(NwArea area)
    {
        LocalVariableString internalNameVar = area.GetObjectVariable<LocalVariableString>("property_internal_name");
        if (internalNameVar.HasValue && !string.IsNullOrWhiteSpace(internalNameVar.Value))
        {
            return internalNameVar.Value.Trim();
        }

        return area.Tag ?? throw new InvalidOperationException("House areas must have a valid tag.");
    }

    private static PropertyCategory ResolvePropertyCategory(NwArea area)
    {
        LocalVariableString categoryVar = area.GetObjectVariable<LocalVariableString>("property_category");

        if (categoryVar.HasValue && !string.IsNullOrWhiteSpace(categoryVar.Value) &&
            Enum.TryParse<PropertyCategory>(categoryVar.Value, true, out PropertyCategory parsed))
        {
            return parsed;
        }

        return PropertyCategory.Residential;
    }

    private SettlementTag ResolveSettlementTag(NwArea area)
    {
        LocalVariableString settlementVar = area.GetObjectVariable<LocalVariableString>("settlement_tag");
        if (settlementVar.HasValue && !string.IsNullOrWhiteSpace(settlementVar.Value))
        {
            return new SettlementTag(settlementVar.Value.Trim());
        }

        string? areaResRef = area.ResRef;
        string? areaTag = area.Tag;

        if (!string.IsNullOrWhiteSpace(areaResRef))
        {
            if (_regions.TryGetSettlementForPointOfInterest(areaResRef, out SettlementId poiSettlement))
            {
                string? settlementFromPoi = ResolveSettlementTagFromSettlement(poiSettlement, areaResRef, areaTag);
                if (!string.IsNullOrWhiteSpace(settlementFromPoi))
                {
                    return new SettlementTag(settlementFromPoi);
                }
            }

            try
            {
                AreaTag areaResRefTag = new(areaResRef);
                if (_regions.TryGetSettlementForArea(areaResRefTag, out SettlementId settlementForArea))
                {
                    string? settlementFromArea = ResolveSettlementTagFromSettlement(settlementForArea, areaResRef, areaTag);
                    if (!string.IsNullOrWhiteSpace(settlementFromArea))
                    {
                        return new SettlementTag(settlementFromArea);
                    }
                }
            }
            catch (ArgumentException ex)
            {
                Log.Warn(ex, "Invalid area resref '{ResRef}' while resolving settlement tag for area {AreaTag}.", areaResRef, areaTag ?? "<untagged>");
            }

            try
            {
                if (TryGetHousingAreaContext(areaResRef, areaTag, out RegionDefinition? region, out AreaDefinition? definition, out PlaceOfInterest? poi))
                {
                    string? settlementFromPoi = ExtractSettlementTagFromPoi(poi);
                    if (!string.IsNullOrWhiteSpace(settlementFromPoi))
                    {
                        return new SettlementTag(settlementFromPoi);
                    }

                    if (definition?.LinkedSettlement is { } linkedSettlement)
                    {
                        string? settlementFromLinked = ResolveSettlementTagFromSettlement(linkedSettlement, areaResRef, areaTag);
                        if (!string.IsNullOrWhiteSpace(settlementFromLinked))
                        {
                            return new SettlementTag(settlementFromLinked);
                        }
                    }

                    string? settlementFromDefinition = ResolveSettlementTagFromAreaDefinition(definition);
                    if (!string.IsNullOrWhiteSpace(settlementFromDefinition))
                    {
                        return new SettlementTag(settlementFromDefinition);
                    }

                    string? settlementFromRegion = ResolveSettlementTagFromRegion(region);
                    if (!string.IsNullOrWhiteSpace(settlementFromRegion))
                    {
                        return new SettlementTag(settlementFromRegion);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warn(ex, "Failed to derive settlement metadata for housing area {AreaTag} ({AreaResRef}).", areaTag ?? "<untagged>", areaResRef);
            }
        }

        LocalVariableInt legacySettlement = area.GetObjectVariable<LocalVariableInt>("settlement");
        if (legacySettlement.HasValue && legacySettlement.Value > 0)
        {
            return new SettlementTag($"legacy:{legacySettlement.Value}");
        }

        string fallbackToken = !string.IsNullOrWhiteSpace(areaTag)
            ? areaTag
            : !string.IsNullOrWhiteSpace(areaResRef) ? areaResRef : "unassigned";

        return new SettlementTag($"legacy:{fallbackToken}");
    }

    private static GoldAmount ResolveMonthlyRent(NwArea area)
    {
        LocalVariableInt rentVar = area.GetObjectVariable<LocalVariableInt>("monthly_rent");
        int rent = rentVar.HasValue ? rentVar.Value : 0;

        if (!rentVar.HasValue || rent <= 0)
        {
            LocalVariableInt legacyRent = area.GetObjectVariable<LocalVariableInt>("rent");
            if (legacyRent.HasValue && legacyRent.Value > 0)
            {
                rent = legacyRent.Value;
            }
        }

        rent = Math.Max(0, rent);
        return GoldAmount.Parse(rent);
    }

    private static bool ResolveBoolean(NwArea area, string variableName, bool defaultValue)
    {
        LocalVariableInt variable = area.GetObjectVariable<LocalVariableInt>(variableName);
        return variable.HasValue ? variable.Value > 0 : defaultValue;
    }

    private static CoinhouseTag? ResolveCoinhouseTag(NwArea area)
    {
        LocalVariableString coinhouseVar = area.GetObjectVariable<LocalVariableString>("settlement_coinhouse_tag");
        if (!coinhouseVar.HasValue || string.IsNullOrWhiteSpace(coinhouseVar.Value))
        {
            return null;
        }

        return new CoinhouseTag(coinhouseVar.Value);
    }

    private static GoldAmount? ResolveOptionalGold(NwArea area, string variableName)
    {
        LocalVariableInt variable = area.GetObjectVariable<LocalVariableInt>(variableName);
        if (!variable.HasValue)
        {
            return null;
        }

        int value = Math.Max(0, variable.Value);
        return value == 0 ? null : GoldAmount.Parse(value);
    }

    private static int ResolveEvictionGraceDays(NwArea area)
    {
        LocalVariableInt graceVar = area.GetObjectVariable<LocalVariableInt>("eviction_grace_days");
        if (!graceVar.HasValue || graceVar.Value <= 0)
        {
            return DefaultEvictionGraceDays;
        }

        return graceVar.Value;
    }

    private static PersonaId? ResolveDefaultOwner(NwArea area)
    {
        LocalVariableString ownerVar = area.GetObjectVariable<LocalVariableString>("default_owner_persona");
        if (!ownerVar.HasValue || string.IsNullOrWhiteSpace(ownerVar.Value))
        {
            return null;
        }

        try
        {
            return PersonaId.Parse(ownerVar.Value);
        }
        catch (Exception ex)
        {
            Log.Warn(ex, "Invalid default owner persona '{Persona}' configured for area {AreaTag}.",
                ownerVar.Value,
                area.Tag);
            return null;
        }
    }

    private static RentablePropertyDefinition BuildDefinition(PropertyId propertyId, PropertyAreaMetadata metadata)
    {
        RentablePropertyDefinition definition = new(
            propertyId,
            metadata.InternalName,
            metadata.Settlement,
            metadata.Category,
            metadata.MonthlyRent,
            metadata.AllowsCoinhouseRental,
            metadata.AllowsDirectRental,
            metadata.SettlementCoinhouseTag,
            metadata.PurchasePrice,
            metadata.MonthlyOwnershipTax,
            metadata.EvictionGraceDays)
        {
            DefaultOwner = metadata.DefaultOwner
        };

        return definition;
    }

    private PropertyPresentation ResolvePropertyPresentation(PropertyAreaMetadata metadata)
    {
        string displayName = metadata.InternalName;
        string? description = null;
        string? settlementName = null;

        try
        {
            if (TryGetHousingAreaContext(metadata.AreaResRef, metadata.AreaTag, out RegionDefinition? region, out _, out PlaceOfInterest? poi))
            {
                settlementName ??= region?.Name;

                if (poi is not null)
                {
                    if (!string.IsNullOrWhiteSpace(poi.Name))
                    {
                        displayName = poi.Name;
                    }

                    if (!string.IsNullOrWhiteSpace(poi.Description))
                    {
                        description = poi.Description;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Log.Warn(ex, "Failed to resolve place-of-interest data for housing area {AreaTag}.", metadata.AreaTag);
        }

        settlementName ??= metadata.Settlement.Value;
        return new PropertyPresentation(displayName, description, settlementName);
    }

    private static string BuildPropertyDescription(string? description) =>
        string.IsNullOrWhiteSpace(description)
            ? "No description is available for this property."
            : description.Trim();

    private bool TryGetHousingAreaContext(
        string areaResRef,
        string? areaTag,
        out RegionDefinition? regionDefinition,
        out AreaDefinition? areaDefinition,
        out PlaceOfInterest? pointOfInterest)
    {
        regionDefinition = null;
        areaDefinition = null;
        pointOfInterest = null;

        if (string.IsNullOrWhiteSpace(areaResRef))
        {
            return false;
        }

        IReadOnlyList<RegionDefinition> regions = _regions.All();
        foreach (RegionDefinition region in regions)
        {
            foreach (AreaDefinition area in region.Areas)
            {
                if (!string.Equals(area.ResRef.Value, areaResRef, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                regionDefinition = region;
                areaDefinition = area;

                pointOfInterest = area.PlacesOfInterest?
                    .FirstOrDefault(p =>
                        string.Equals(p.ResRef, areaResRef, StringComparison.OrdinalIgnoreCase) ||
                        (!string.IsNullOrWhiteSpace(areaTag) && string.Equals(p.Tag, areaTag, StringComparison.OrdinalIgnoreCase)))
                    ?? area.PlacesOfInterest?.FirstOrDefault(p => p.Type == PoiType.House);

                return true;
            }
        }

        return false;
    }

    private string? ResolveSettlementTagFromSettlement(SettlementId settlementId, string areaResRef, string? areaTag)
    {
        if (settlementId.Value <= 0)
        {
            return null;
        }

        try
        {
            IReadOnlyList<PlaceOfInterest> pois = _regions.GetPointsOfInterestForSettlement(settlementId);
            if (pois.Count == 0)
            {
                return null;
            }

            PlaceOfInterest? matchingPoi = pois.FirstOrDefault(p =>
                string.Equals(p.ResRef, areaResRef, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrWhiteSpace(areaTag) && string.Equals(p.Tag, areaTag, StringComparison.OrdinalIgnoreCase)));

            string? candidate = ExtractSettlementTagFromPoi(matchingPoi);
            if (!string.IsNullOrWhiteSpace(candidate))
            {
                return candidate;
            }

            PlaceOfInterest? settlementPoi = pois.FirstOrDefault(p => p.Type == PoiType.Bank)
                                           ?? pois.FirstOrDefault(p => p.Type == PoiType.Guild)
                                           ?? pois.FirstOrDefault(p => p.Type == PoiType.Temple)
                                           ?? pois.FirstOrDefault(p => p.Type == PoiType.Library)
                                           ?? pois.FirstOrDefault();

            candidate = ExtractSettlementTagFromPoi(settlementPoi);
            if (!string.IsNullOrWhiteSpace(candidate))
            {
                return candidate;
            }
        }
        catch (Exception ex)
        {
            Log.Warn(ex, "Failed to resolve settlement tag from points of interest for settlement {SettlementId}.", settlementId.Value);
        }

        return null;
    }

    private static string? ExtractSettlementTagFromPoi(PlaceOfInterest? poi)
    {
        if (poi is null)
        {
            return null;
        }

        string? candidate = NormalizeSettlementIdentifier(poi.Tag);
        if (!string.IsNullOrWhiteSpace(candidate))
        {
            return candidate;
        }

        return NormalizeSettlementIdentifier(poi.Name);
    }

    private static string? NormalizeSettlementIdentifier(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string trimmed = value.Trim();

        if (trimmed.StartsWith("settlement:", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed["settlement:".Length..];
        }

        int lastColon = trimmed.LastIndexOf(':');
        if (lastColon >= 0 && lastColon < trimmed.Length - 1)
        {
            trimmed = trimmed[(lastColon + 1)..];
        }

        if (trimmed.EndsWith("_coinhouse", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed[..^"_coinhouse".Length];
        }
        else if (trimmed.EndsWith("_bank", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed[..^"_bank".Length];
        }

        trimmed = trimmed.Trim('_', '-');
        if (trimmed.Length == 0)
        {
            return null;
        }

        if (!SettlementIdentifierPattern.IsMatch(trimmed))
        {
            return null;
        }

        return trimmed.ToLowerInvariant();
    }

    private static string? ResolveSettlementTagFromAreaDefinition(AreaDefinition? areaDefinition)
    {
        if (areaDefinition?.DefinitionTags is not { Count: > 0 })
        {
            return null;
        }

        foreach (string tag in areaDefinition.DefinitionTags)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                continue;
            }

            string trimmed = tag.Trim();
            int colonIndex = trimmed.IndexOf(':');
            if (colonIndex <= 0 || colonIndex >= trimmed.Length - 1)
            {
                continue;
            }

            string prefix = trimmed[..colonIndex];
            if (!prefix.Equals("settlement", StringComparison.OrdinalIgnoreCase) &&
                !prefix.Equals("municipality", StringComparison.OrdinalIgnoreCase) &&
                !prefix.Equals("town", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string? candidate = NormalizeSettlementIdentifier(trimmed[(colonIndex + 1)..]);
            if (!string.IsNullOrWhiteSpace(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static string? ResolveSettlementTagFromRegion(RegionDefinition? region)
    {
        if (region is null)
        {
            return null;
        }

        string? candidate = NormalizeSettlementIdentifier(region.Tag.Value);
        if (!string.IsNullOrWhiteSpace(candidate))
        {
            return candidate;
        }

        return NormalizeSettlementIdentifier(region.Name);
    }

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

    private static RentPropertyPaymentOptionViewModel CreateDirectOptionModel(bool visible, bool enabled, string status) =>
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
            ? $"Pay via {settlementName} Coinhouse"
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
        string.IsNullOrWhiteSpace(settlementName) ? "this settlement" : settlementName!;

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
        await SendServerMessageAsync(player,
            $"The rental offer for {propertyName} has expired.",
            ColorConstants.Orange);
    }

    private Task OnRentWindowClosedAsync(PersonaId personaId)
    {
        RemoveSession(personaId);
        return Task.CompletedTask;
    }

    private sealed record PropertyAreaMetadata(
        string AreaTag,
        string AreaResRef,
        string InternalName,
        PropertyCategory Category,
        SettlementTag Settlement,
        GoldAmount MonthlyRent,
        bool AllowsCoinhouseRental,
        bool AllowsDirectRental,
        CoinhouseTag? SettlementCoinhouseTag,
        GoldAmount? PurchasePrice,
        GoldAmount? MonthlyOwnershipTax,
        int EvictionGraceDays,
        PersonaId? DefaultOwner,
        PropertyId? ExplicitPropertyId);

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

    private sealed record PropertyPresentation(string DisplayName, string? Description, string? SettlementName);
}
