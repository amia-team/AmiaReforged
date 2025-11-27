using System.Diagnostics.CodeAnalysis;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Runtime;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Properties;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Regions;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.Player.Housing;

[ServiceBinding(typeof(PlayerHouseService))]
public class PlayerHouseService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private const string IsHouseDoorVariable = "is_house_door";
    private const string HouseSignTag = "house_sign";
    private const string TargetAreaTagLocalString = "target_area_tag";

    private readonly RuntimeCharacterService _characters;
    private readonly PropertyMetadataResolver _metadataResolver;
    private readonly PropertyDefinitionSynchronizer _definitionSynchronizer;
    private readonly PropertyRentFlow _rentFlow;
    private readonly PropertyRentPaymentFlow _rentPaymentFlow;

    private readonly HashSet<uint> _registeredDoorIds = new();
    private readonly HashSet<uint> _registeredSignIds = new();

    public PlayerHouseService(
        RuntimeCharacterService characters,
        PropertyMetadataResolver metadataResolver,
        PropertyDefinitionSynchronizer definitionSynchronizer,
        PropertyRentFlow rentFlow,
        PropertyRentPaymentFlow rentPaymentFlow)
    {
        _characters = characters;
        _metadataResolver = metadataResolver;
        _definitionSynchronizer = definitionSynchronizer;
        _rentFlow = rentFlow;
        _rentPaymentFlow = rentPaymentFlow;

        BindHouseDoors();
        BindHouseSigns();
        NwModule.Instance.OnModuleLoad += RegisterNewHouses;
    }

    private void BindHouseDoors()
    {
        // Search all doors in the module for the "is_house_door" local variable
        foreach (NwArea area in NwModule.Instance.Areas)
        {
            foreach (NwDoor door in area.Objects.OfType<NwDoor>())
            {
                LocalVariableInt isHouseDoor = door.GetObjectVariable<LocalVariableInt>(IsHouseDoorVariable);

                if (isHouseDoor.HasValue && isHouseDoor.Value > 0)
                {
                    if (_registeredDoorIds.Add(door.ObjectId))
                    {
                        door.OnFailToOpen += HandlePlayerInteraction;
                        Log.Debug("Registered house door: {DoorTag} in area {AreaTag}", door.Tag, area.Tag);
                    }
                }
            }
        }

        Log.Info("Registered {Count} house doors", _registeredDoorIds.Count);
    }

    private void BindHouseSigns()
    {
        IEnumerable<NwPlaceable> signs = NwObject.FindObjectsWithTag<NwPlaceable>(HouseSignTag);

        foreach (NwPlaceable sign in signs)
        {
            if (_registeredSignIds.Add(sign.ObjectId))
            {
                sign.OnUsed += HandleHouseSignInteraction;
            }
        }
    }

    private async void RegisterNewHouses(ModuleEvents.OnModuleLoad obj)
    {
        try
        {
            BindHouseDoors();
            BindHouseSigns();
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
                await ShowFloatingTextAsync(player, "This door is missing its destination. Please notify a DM.");
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
                if (!_metadataResolver.TryCapture(area, out metadata))
                {
                    Log.Warn("Area {AreaTag} did not resolve to a configured property via region metadata.",
                        area.Tag);
                    await ShowFloatingTextAsync(player,
                        "This property is not registered in the housing system. Please notify a DM.");
                    return;
                }
            }
            catch (Exception metaEx)
            {
                Log.Error(metaEx, "Failed to capture metadata for housing area {AreaTag}.", area.Tag);
                await ShowFloatingTextAsync(player, "This property is not configured correctly. Please notify a DM.");
                return;
            }

            PropertyId propertyId = _definitionSynchronizer.ResolvePropertyId(metadata);

            RentablePropertySnapshot? snapshot = await EnsurePropertySnapshotAsync(propertyId, metadata);
            if (snapshot is null)
            {
                await ShowFloatingTextAsync(player,
                    "The housing record for this property could not be loaded. Please try again later.");
                return;
            }

            // Door now only handles access - rental interactions happen at the sign
            if (CanPlayerAccess(personaId, snapshot))
            {
                await UnlockDoorAsync(obj.Door);
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

    [SuppressMessage("ReSharper", "AsyncVoidMethod")]
    private async void HandleHouseSignInteraction(PlaceableEvents.OnUsed obj)
    {
        if (!obj.UsedBy.IsPlayerControlled(out NwPlayer? player))
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
                obj.Placeable.GetObjectVariable<LocalVariableString>(TargetAreaTagLocalString).Value;

            if (string.IsNullOrWhiteSpace(targetAreaTag))
            {
                Log.Warn("House sign {SignTag} is missing the {LocalVar} local variable.", obj.Placeable.Tag,
                    TargetAreaTagLocalString);
                await ShowFloatingTextAsync(player, "This sign is not configured. Please notify a DM.");
                return;
            }

            NwArea? area = FindAreaByTag(targetAreaTag);
            if (area is null)
            {
                Log.Error("Failed to locate area with tag {AreaTag} for house sign {SignTag}.", targetAreaTag,
                    obj.Placeable.Tag);
                await ShowFloatingTextAsync(player,
                    "The property for this sign could not be located. Please notify a DM.");
                return;
            }

            PropertyAreaMetadata metadata;
            try
            {
                if (!_metadataResolver.TryCapture(area, out metadata))
                {
                    Log.Warn("Area {AreaTag} did not resolve to a configured property via region metadata.",
                        area.Tag);
                    await ShowFloatingTextAsync(player,
                        "This property is not registered in the housing system. Please notify a DM.");
                    return;
                }
            }
            catch (Exception metaEx)
            {
                Log.Error(metaEx, "Failed to capture metadata for housing area {AreaTag}.", area.Tag);
                await ShowFloatingTextAsync(player, "This property is not configured correctly. Please notify a DM.");
                return;
            }

            PropertyId propertyId = _definitionSynchronizer.ResolvePropertyId(metadata);

            RentablePropertySnapshot? snapshot = await EnsurePropertySnapshotAsync(propertyId, metadata);
            if (snapshot is null)
            {
                await ShowFloatingTextAsync(player,
                    "The housing record for this property could not be loaded. Please try again later.");
                return;
            }

            // STATE 1: House is owned/rented by someone else -> Do nothing
            if (snapshot.OccupancyStatus != PropertyOccupancyStatus.Vacant &&
                !CanPlayerAccess(personaId, snapshot))
            {
                // Silent - no message shown per requirements
                return;
            }

            // STATE 2: House is owned/rented by the player -> Show rent payment option
            if (snapshot.OccupancyStatus == PropertyOccupancyStatus.Rented &&
                snapshot.ActiveRental is not null &&
                snapshot.ActiveRental.Tenant.Equals(personaId))
            {
                PropertyRentFlow.RentOfferPresentation presentation = ResolvePropertyPresentation(metadata);

                await _rentPaymentFlow.HandleRentPaymentRequestAsync(
                        player,
                        personaId,
                        propertyId,
                        snapshot,
                        presentation.DisplayName,
                        presentation.Description,
                        presentation.SettlementName)
                    .ConfigureAwait(false);
                return;
            }

            // STATE 3 & 4: House is vacant
            if (snapshot.OccupancyStatus == PropertyOccupancyStatus.Vacant)
            {
                // Check if player already has an active rental elsewhere
                List<RentablePropertySnapshot> existingRentals =
                    await _rentFlow.GetPropertiesRentedByTenantAsync(personaId).ConfigureAwait(false);

                // STATE 3: Player already owns a house -> Refuse access
                if (existingRentals.Any())
                {
                    await ShowFloatingTextAsync(player,
                        "You already have an active rental. You can only rent one property at a time.");
                    return;
                }

                // STATE 4: Player has no rental and house is vacant -> Present rental option
                // Try to find the associated door (optional - only needed for unlocking after rental)
                // Must switch to main thread before accessing NWN VM objects
                await NwTask.SwitchToMainThread();
                NwDoor? door = FindHouseDoorForArea(targetAreaTag);

                PropertyRentFlow.RentOfferPresentation presentation = ResolvePropertyPresentation(metadata);
                await _rentFlow.HandleVacantPropertyInteractionAsync(
                        sign: obj.Placeable,
                        door: door,
                        player,
                        personaId,
                        propertyId,
                        snapshot,
                        presentation)
                    .ConfigureAwait(false);
                return;
            }

            // Fallback
            await ShowFloatingTextAsync(player, "You cannot interact with this property at this time.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Unexpected error while handling house sign interaction for sign {SignTag}.",
                obj.Placeable.Tag);

            try
            {
                await ShowFloatingTextAsync(player, "Housing system encountered an error. Please try again shortly.");
            }
            catch (Exception nested)
            {
                Log.Error(nested, "Failed to send error feedback to player during sign interaction.");
            }
        }
    }

    private async Task EnsureHouseDefinitionsAsync()
    {
        await _definitionSynchronizer.SynchronizeModuleHousingAsync().ConfigureAwait(false);
    }

    private async Task<RentablePropertySnapshot?> EnsurePropertySnapshotAsync(
        PropertyId propertyId,
        PropertyAreaMetadata metadata)
    {
        return await _definitionSynchronizer.EnsureSnapshotAsync(propertyId, metadata).ConfigureAwait(false);
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

    internal static async Task SendServerMessageAsync(NwPlayer player, string message, Color? color = null)
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

    private static NwDoor? FindHouseDoorForArea(string targetAreaTag)
    {
        // Search all doors in all areas for house doors with matching target area
        foreach (NwArea area in NwModule.Instance.Areas)
        {
            foreach (NwDoor door in area.Objects.OfType<NwDoor>())
            {
                LocalVariableInt isHouseDoor = door.GetObjectVariable<LocalVariableInt>(IsHouseDoorVariable);

                if (isHouseDoor.HasValue && isHouseDoor.Value > 0)
                {
                    string? doorTargetArea = door.GetObjectVariable<LocalVariableString>(TargetAreaTagLocalString).Value;
                    if (!string.IsNullOrWhiteSpace(doorTargetArea) &&
                        string.Equals(doorTargetArea, targetAreaTag, StringComparison.OrdinalIgnoreCase))
                    {
                        return door;
                    }
                }
            }
        }

        return null;
    }

    private PropertyRentFlow.RentOfferPresentation ResolvePropertyPresentation(PropertyAreaMetadata metadata)
    {
        string displayName = metadata.InternalName;
        string? description = null;
        string? settlementName = null;

        try
        {
            if (_metadataResolver.TryGetHousingAreaContext(metadata.AreaResRef, metadata.AreaTag,
                    out RegionDefinition? region, out _,
                    out PlaceOfInterest? poi))
            {
                settlementName ??= region?.Name;

                if (poi is not null)
                {
                    if (!string.IsNullOrWhiteSpace(poi.Name))
                    {
                        displayName = poi.Name;
                    }

                    description ??= poi.Description;
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Error resolving property presentation for metadata {Metadata}", metadata);
        }

        return new PropertyRentFlow.RentOfferPresentation(displayName, description, settlementName);
    }

    internal static async Task ShowFloatingTextAsync(NwPlayer player, string message)
    {
        await NwTask.SwitchToMainThread();
        if (!player.IsValid)
        {
            return;
        }

        player.FloatingTextString(message, false);
    }

    internal static async Task UnlockDoorAsync(NwDoor door)
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
}
