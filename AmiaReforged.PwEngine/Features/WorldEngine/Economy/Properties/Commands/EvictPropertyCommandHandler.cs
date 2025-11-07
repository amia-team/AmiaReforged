using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Storage;
using AmiaReforged.PwEngine.Features.WorldEngine.Regions;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using Anvil.API;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Properties.Commands;

/// <summary>
/// Handles property eviction by foreclosing tenant placeables to coinhouse storage
/// and clearing property state.
/// </summary>
[ServiceBinding(typeof(ICommandHandler<EvictPropertyCommand>))]
public sealed class EvictPropertyCommandHandler : ICommandHandler<EvictPropertyCommand>
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly IPersistentObjectRepository _objectRepository;
    private readonly RegionIndex _regionIndex;
    private readonly IRentablePropertyRepository _propertyRepository;
    private readonly IForeclosureStorageService _foreclosureStorage;

    public EvictPropertyCommandHandler(
        IPersistentObjectRepository objectRepository,
        RegionIndex regionIndex,
        IRentablePropertyRepository propertyRepository,
        IForeclosureStorageService foreclosureStorage)
    {
        _objectRepository = objectRepository ?? throw new ArgumentNullException(nameof(objectRepository));
        _regionIndex = regionIndex ?? throw new ArgumentNullException(nameof(regionIndex));
        _propertyRepository = propertyRepository ?? throw new ArgumentNullException(nameof(propertyRepository));
        _foreclosureStorage = foreclosureStorage ?? throw new ArgumentNullException(nameof(foreclosureStorage));
    }

    public async Task<CommandResult> HandleAsync(EvictPropertyCommand command, CancellationToken cancellationToken = default)
    {
        RentablePropertySnapshot property = command.Property;

        if (property.CurrentTenant is null)
        {
            return CommandResult.Fail("Cannot evict property - no current tenant.");
        }

        try
        {
            // Phase 1: Foreclose tenant placeables to coinhouse storage
            await ForeclosePlaceablesAsync(property, cancellationToken).ConfigureAwait(false);

            // Phase 2: Clear property state and mark as vacant
            RentablePropertySnapshot evictedProperty = new(
                property.Definition,
                PropertyOccupancyStatus.Vacant,
                CurrentTenant: null,
                CurrentOwner: null,
                Residents: Array.Empty<PersonaId>(),
                ActiveRental: null);

            await _propertyRepository.PersistRentalAsync(evictedProperty, cancellationToken).ConfigureAwait(false);

            // Phase 3: Notify player if online
            await NotifyEvictionAsync(property).ConfigureAwait(false);

            Log.Info("Successfully evicted property {PropertyId} ({InternalName}).",
                property.Definition.Id,
                property.Definition.InternalName);

            return CommandResult.Ok();
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to evict property {PropertyId} ({InternalName}).",
                property.Definition.Id,
                property.Definition.InternalName);

            return CommandResult.Fail($"Eviction failed: {ex.Message}");
        }
    }

    private async Task ForeclosePlaceablesAsync(
        RentablePropertySnapshot property,
        CancellationToken cancellationToken)
    {
        Log.Info("Starting foreclosure process for property {InternalName}, tenant {Tenant}.",
            property.Definition.InternalName,
            property.CurrentTenant);

        // Get coinhouse for this property's settlement
        CoinhouseTag? coinhouseTag = property.Definition.SettlementCoinhouseTag;
        if (coinhouseTag is null)
        {
            Log.Warn("Property {InternalName} has no settlement coinhouse - " +
                     "cannot foreclose items, will destroy them instead.",
                property.Definition.InternalName);

            // Fall back to destruction
            await DeletePlaceablesAsync(property, cancellationToken).ConfigureAwait(false);
            return;
        }

        // Resolve area ResRef
        string? areaResRef = ResolveAreaResRefForProperty(property.Definition.InternalName);
        if (areaResRef is null)
        {
            Log.Warn("Cannot determine area for property {InternalName} - POI not found.",
                property.Definition.InternalName);
            return;
        }

        Log.Info("Resolved area ResRef {AreaResRef} for property {InternalName}.",
            areaResRef,
            property.Definition.InternalName);

        // Get character ID from tenant
        Guid? characterId = ResolveCharacterId(property.CurrentTenant);
        if (characterId is null)
        {
            Log.Warn("Cannot determine character ID for tenant {Tenant} of property {InternalName}.",
                property.CurrentTenant,
                property.Definition.InternalName);
            return;
        }

        Log.Info("Resolved character ID {CharacterId} from tenant {Tenant}.",
            characterId.Value,
            property.CurrentTenant);

        // Get all placeables in the property area
        List<PersistentObject> placeables = _objectRepository
            .GetObjectsForArea(areaResRef)
            .Where(o => o.Type == (int)ObjectTypes.Placeable)
            .ToList();

        Log.Info("Foreclosing {Count} placeables from property {InternalName} to coinhouse {Coinhouse}.",
            placeables.Count,
            property.Definition.InternalName,
            coinhouseTag.Value);

        int foreclosedCount = 0;
        int destroyedCount = 0;

        foreach (PersistentObject placeable in placeables)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                // Try to recover the original item
                if (placeable.SourceItemData is not null)
                {
                    // Add to foreclosed storage at coinhouse
                    await _foreclosureStorage.AddForeclosedItemAsync(
                        coinhouseTag.Value,
                        characterId.Value,
                        placeable.SourceItemData,
                        cancellationToken).ConfigureAwait(false);

                    foreclosedCount++;

                    Log.Info("Foreclosed placeable {PlaceableId} as item to coinhouse {Coinhouse}.",
                        placeable.Id,
                        coinhouseTag.Value.Value);
                }
                else
                {
                    // No source item - this was probably a system placeable
                    Log.Warn("Placeable {PlaceableId} has no source item data, destroying instead of foreclosing.",
                        placeable.Id);

                    destroyedCount++;
                }

                // Delete from database
                await _objectRepository.DeleteObject(placeable.Id).ConfigureAwait(false);

                // Destroy in-game object
                await TryDestroyPlaceableInGameAsync(placeable, areaResRef).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to foreclose placeable {PlaceableId}.", placeable.Id);
            }
        }

        Log.Info("Foreclosure complete for property {InternalName}: " +
                 "{ForeclosedCount} items moved to coinhouse, {DestroyedCount} items destroyed.",
            property.Definition.InternalName,
            foreclosedCount,
            destroyedCount);
    }

    /// <summary>
    /// Fallback method: destroys placeables when foreclosure is not possible.
    /// </summary>
    private async Task DeletePlaceablesAsync(
        RentablePropertySnapshot property,
        CancellationToken cancellationToken)
    {
        string? areaResRef = ResolveAreaResRefForProperty(property.Definition.InternalName);
        if (areaResRef is null)
        {
            Log.Warn("Cannot determine area for property {InternalName}.", property.Definition.InternalName);
            return;
        }

        List<PersistentObject> placeables = _objectRepository
            .GetObjectsForArea(areaResRef)
            .Where(o => o.Type == (int)ObjectTypes.Placeable)
            .ToList();

        Log.Info("Destroying {Count} placeables from property {InternalName} (no coinhouse available).",
            placeables.Count,
            property.Definition.InternalName);

        foreach (PersistentObject placeable in placeables)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                await _objectRepository.DeleteObject(placeable.Id).ConfigureAwait(false);
                await TryDestroyPlaceableInGameAsync(placeable, areaResRef).ConfigureAwait(false);

                Log.Info("Destroyed placeable {PlaceableId}.", placeable.Id);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to destroy placeable {PlaceableId}.", placeable.Id);
            }
        }
    }

    private async Task NotifyEvictionAsync(RentablePropertySnapshot property)
    {
        Guid? characterId = ResolveCharacterId(property.CurrentTenant);
        if (characterId is null)
        {
            return;
        }

        await NwTask.SwitchToMainThread();

        try
        {
            NwPlayer? player = NwModule.Instance.Players
                .FirstOrDefault(p => GetCharacterGuid(p) == characterId);

            if (player is null)
            {
                Log.Info("Tenant {CharacterId} is not online, skipping eviction notification.",
                    characterId.Value);
                return;
            }

            string coinhouseName = property.Definition.SettlementCoinhouseTag?.Value ?? "the local coinhouse";

            player.SendServerMessage(
                $"Your rental property '{property.Definition.InternalName}' has been foreclosed due to unpaid rent. " +
                $"Your belongings have been moved to {coinhouseName} where you can reclaim them.",
                ColorConstants.Orange);

            Log.Info("Sent eviction notification to player {CharacterId}.", characterId.Value);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to send eviction notification to character {CharacterId}.", characterId.Value);
        }
    }

    private static Guid? GetCharacterGuid(NwPlayer player)
    {
        NwCreature? creature = player.ControlledCreature;
        if (creature is null)
        {
            return null;
        }

        LocalVariableString characterIdVar = creature.GetObjectVariable<LocalVariableString>("character_id");
        if (string.IsNullOrEmpty(characterIdVar.Value))
        {
            return null;
        }

        return Guid.TryParse(characterIdVar.Value, out Guid characterId) ? characterId : null;
    }

    private string? ResolveAreaResRefForProperty(string internalName)
    {
        // POI.Name matches InternalName by convention
        // Since we only have the POI Name (InternalName), we need to search all regions and their POIs
        IReadOnlyList<RegionDefinition> allRegions = _regionIndex.All();
        
        foreach (RegionDefinition region in allRegions)
        {
            foreach (AreaDefinition area in region.Areas)
            {
                if (area.PlacesOfInterest is not { Count: > 0 })
                {
                    continue;
                }
                
                PlaceOfInterest? poi = area.PlacesOfInterest.FirstOrDefault(p => 
                    string.Equals(p.Name, internalName, StringComparison.OrdinalIgnoreCase));
                
                if (poi is not null)
                {
                    // The area ResRef is stored in POI.ResRef
                    return poi.ResRef;
                }
            }
        }

        return null;
    }

    private static Guid? ResolveCharacterId(PersonaId? personaId)
    {
        if (personaId is null)
        {
            return null;
        }

        // For character personas, extract the character ID directly
        if (personaId.Value.Type == PersonaType.Character)
        {
            // PersonaId.Value is the string representation of the GUID
            if (Guid.TryParse(personaId.Value.Value, out Guid characterId))
            {
                return characterId;
            }
        }

        // For player personas, we'd need to find their active character
        // For now, only support character personas for eviction
        return null;
    }

    private static async Task TryDestroyPlaceableInGameAsync(PersistentObject plc, string areaResRef)
    {
        await NwTask.SwitchToMainThread();

        try
        {
            Log.Info("Attempting to destroy in-game placeable {PlaceableId} in area {AreaResRef}.",
                plc.Id,
                areaResRef);

            // Find the area
            NwArea? area = NwModule.Instance.Areas.FirstOrDefault(a => a.ResRef == areaResRef);
            if (area is null)
            {
                Log.Warn("Area {AreaResRef} not found in game - cannot destroy placeable {PlaceableId}.",
                    areaResRef,
                    plc.Id);
                return;
            }

            Log.Info("Found area {AreaResRef}, searching for placeable with DB ID {DbId}...",
                areaResRef,
                plc.Id);

            // Find the placeable with matching database ID
            const string DatabaseIdLocalInt = "db_id";
            bool found = false;
            foreach (NwPlaceable placeable in area.FindObjectsOfTypeInArea<NwPlaceable>())
            {
                LocalVariableInt dbVar = placeable.GetObjectVariable<LocalVariableInt>(DatabaseIdLocalInt);
                if (dbVar.Value == plc.Id)
                {
                    Log.Info("Found in-game placeable with DB ID {DbId}, tag: {Tag}, destroying...",
                        plc.Id,
                        placeable.Tag ?? "<no tag>");

                    // Clear local variables
                    dbVar.Delete();

                    LocalVariableString characterVar = placeable.GetObjectVariable<LocalVariableString>("character_id");
                    if (!string.IsNullOrEmpty(characterVar.Value))
                    {
                        characterVar.Delete();
                    }

                    // Destroy the placeable
                    if (placeable.IsValid)
                    {
                        placeable.Destroy();
                        Log.Info("Successfully destroyed in-game placeable {PlaceableId}.",
                            plc.Id);
                    }
                    else
                    {
                        Log.Warn("Placeable {PlaceableId} was not valid, could not destroy.",
                            plc.Id);
                    }

                    found = true;
                    break;
                }
            }

            if (!found)
            {
                Log.Warn("No in-game placeable found with DB ID {DbId} in area {AreaResRef}.",
                    plc.Id,
                    areaResRef);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to destroy in-game placeable {PlaceableId}.", plc.Id);
        }
    }
}
