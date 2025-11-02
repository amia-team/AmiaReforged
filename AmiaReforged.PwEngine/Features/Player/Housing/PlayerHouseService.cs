using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using System.Globalization;
using AmiaReforged.PwEngine.Features.WorldEngine.Characters.Runtime;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Properties;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
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
    private readonly HashSet<uint> _registeredDoorIds = new();
    private readonly ConcurrentDictionary<PersonaId, PendingRentRequest> _pendingRentRequests = new();

    private static readonly TimeSpan RentalConfirmationTimeout = TimeSpan.FromSeconds(30);
    private static readonly PropertyRentalPolicy RentalPolicy = new();
    private static readonly GoldAmount HouseSize1Rent = GoldAmount.Parse(50_000);
    private static readonly GoldAmount HouseSize2Rent = GoldAmount.Parse(120_000);
    private static readonly GoldAmount HouseSize3Rent = GoldAmount.Parse(300_000);

    public PlayerHouseService(IRentablePropertyRepository properties, RuntimeCharacterService characters)
    {
        _properties = properties;
        _characters = characters;

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
                Log.Warn("Door {DoorTag} is missing the {LocalVar} local variable.", obj.Door.Tag,
                    TargetAreaTagLocalString);
                await ShowFloatingTextAsync(player,
                    "This door is missing its destination. Please notify a DM.");
                return;
            }

            NwArea? area = FindAreaByTag(targetAreaTag);
            if (area is null)
            {
                Log.Error("Failed to locate area with tag {AreaTag} for door {DoorTag}.", targetAreaTag, obj.Door.Tag);
                await ShowFloatingTextAsync(player,
                    "The destination for this property could not be located. Please notify a DM.");
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
                await ShowFloatingTextAsync(player,
                    "This property is not configured correctly. Please notify a DM.");
                return;
            }

            if (!TryResolvePropertyId(obj.Door, metadata, out PropertyId propertyId))
            {
                await ShowFloatingTextAsync(player,
                    "This property is missing an identifier. Please notify a DM.");
                return;
            }

            RentablePropertySnapshot? snapshot = await EnsurePropertySnapshotAsync(propertyId, metadata);
            if (snapshot is null)
            {
                await ShowFloatingTextAsync(player,
                    "The housing record for this property could not be loaded. Please try again later.");
                return;
            }

            if (CanPlayerAccess(personaId, snapshot))
            {
                await UnlockDoorAsync(obj.Door);
                return;
            }

            if (snapshot.OccupancyStatus == PropertyOccupancyStatus.Vacant)
            {
                await HandleVacantPropertyInteractionAsync(obj.Door, player, personaId, propertyId);
                return;
            }

            await ShowFloatingTextAsync(player, BuildAccessDeniedMessage(snapshot));
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unexpected error while handling housing interaction for door {DoorTag}.", obj.Door.Tag);

            try
            {
                await ShowFloatingTextAsync(player,
                    "Housing system encountered an error. Please try again shortly.");
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
        PropertyId propertyId)
    {
    GoldAmount? configuredRent = await ResolveDoorRentAsync(door).ConfigureAwait(false);
        if (configuredRent is null)
        {
            Log.Warn("Door {DoorTag} is missing a valid house_size local variable for rental pricing.", door.Tag);
            await ShowFloatingTextAsync(player,
                "This property is not configured for rental yet. Please notify a DM.");
            return;
        }

        GoldAmount availableGold = await GetPlayerGoldAsync(player);
        if (!availableGold.CanAfford(configuredRent.Value))
        {
            _pendingRentRequests.TryRemove(personaId, out _);
            await SendServerMessageAsync(player,
                $"Rent is {FormatGold(configuredRent.Value)} gold. You need more gold to rent this property.",
                ColorConstants.Red);
            await ShowFloatingTextAsync(player, "You do not have enough gold to rent this property.");
            return;
        }

        if (_pendingRentRequests.TryGetValue(personaId, out PendingRentRequest pending))
        {
            if (pending.PropertyId.Equals(propertyId) && pending.Cost.Equals(configuredRent.Value) &&
                DateTimeOffset.UtcNow - pending.RequestedAt <= RentalConfirmationTimeout)
            {
                _pendingRentRequests.TryRemove(personaId, out _);
                await FinalizeRentalAsync(player, personaId, propertyId, configuredRent.Value, door);
                return;
            }

            _pendingRentRequests.TryRemove(personaId, out _);
        }

        _pendingRentRequests[personaId] = new PendingRentRequest(propertyId, configuredRent.Value, DateTimeOffset.UtcNow);

        string formattedCost = FormatGold(configuredRent.Value);

        await SendServerMessageAsync(player,
            $"This property is vacant. Rent is {formattedCost} gold. Activate the door again within 30 seconds to confirm.",
            ColorConstants.Orange);

        await ShowFloatingTextAsync(player,
            $"Rent is {formattedCost} gold. Use the door again within 30 seconds to confirm.");
    }

    private async Task FinalizeRentalAsync(
        NwPlayer player,
        PersonaId personaId,
        PropertyId propertyId,
        GoldAmount rentCost,
        NwDoor door)
    {
        RentablePropertySnapshot? latest = await _properties.GetSnapshotAsync(propertyId);
        if (latest is null)
        {
            await ShowFloatingTextAsync(player,
                "We couldn't load the rental record for this property. Please try again shortly.");
            return;
        }

        if (latest.OccupancyStatus != PropertyOccupancyStatus.Vacant)
        {
            await ShowFloatingTextAsync(player, "This property has already been claimed by someone else.");
            return;
        }

        GoldAmount availableGold = await GetPlayerGoldAsync(player);
        if (!availableGold.CanAfford(rentCost))
        {
            await SendServerMessageAsync(player,
                $"Rent is {FormatGold(rentCost)} gold. You no longer have enough gold to rent this property.",
                ColorConstants.Red);
            await ShowFloatingTextAsync(player, "You no longer have enough gold to rent this property.");
            return;
        }

        if (!await TryWithdrawGoldAsync(player, rentCost))
        {
            await ShowFloatingTextAsync(player, "Failed to withdraw gold for the rental. Please try again.");
            return;
        }

        DateOnly startDate = DateOnly.FromDateTime(DateTime.UtcNow);
        DateOnly nextDueDate = RentalPolicy.CalculateNextDueDate(startDate);

        RentablePropertyDefinition updatedDefinition = latest.Definition with
        {
            MonthlyRent = rentCost
        };

        RentalAgreementSnapshot agreement = new(
            personaId,
            startDate,
            nextDueDate,
            rentCost,
            RentalPaymentMethod.OutOfPocket,
            null);

        RentablePropertySnapshot updated = latest with
        {
            Definition = updatedDefinition,
            OccupancyStatus = PropertyOccupancyStatus.Rented,
            CurrentTenant = personaId,
            ActiveRental = agreement
        };

        await _properties.PersistRentalAsync(updated);

        await SendServerMessageAsync(player,
            $"You have rented this property. Your next payment is due on {nextDueDate}.", ColorConstants.Orange);
        await ShowFloatingTextAsync(player, "You have rented this property!");

        await UnlockDoorAsync(door);
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
        amount.Value.ToString("N0", CultureInfo.InvariantCulture);

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

    private static SettlementTag ResolveSettlementTag(NwArea area)
    {
        LocalVariableString settlementVar = area.GetObjectVariable<LocalVariableString>("settlement_tag");
        if (settlementVar.HasValue && !string.IsNullOrWhiteSpace(settlementVar.Value))
        {
            return new SettlementTag(settlementVar.Value);
        }

        LocalVariableInt legacySettlement = area.GetObjectVariable<LocalVariableInt>("settlement");
        if (legacySettlement.HasValue && legacySettlement.Value > 0)
        {
            return new SettlementTag($"legacy:{legacySettlement.Value}");
        }

        return new SettlementTag($"legacy:{area.Tag}");
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

    private sealed record PendingRentRequest(PropertyId PropertyId, GoldAmount Cost, DateTimeOffset RequestedAt);

    private sealed record PropertyAreaMetadata(
        string AreaTag,
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
}
