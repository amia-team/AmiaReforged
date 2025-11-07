using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Regions;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using Anvil.API;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Properties.Commands;

/// <summary>
/// Handles property eviction by deleting tenant placeables and clearing property state.
/// </summary>
[ServiceBinding(typeof(ICommandHandler<EvictPropertyCommand>))]
public sealed class EvictPropertyCommandHandler : ICommandHandler<EvictPropertyCommand>
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly IPersistentObjectRepository _objectRepository;
    private readonly RegionIndex _regionIndex;
    private readonly IRentablePropertyRepository _propertyRepository;

    public EvictPropertyCommandHandler(
        IPersistentObjectRepository objectRepository,
        RegionIndex regionIndex,
        IRentablePropertyRepository propertyRepository)
    {
        _objectRepository = objectRepository ?? throw new ArgumentNullException(nameof(objectRepository));
        _regionIndex = regionIndex ?? throw new ArgumentNullException(nameof(regionIndex));
        _propertyRepository = propertyRepository ?? throw new ArgumentNullException(nameof(propertyRepository));
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
            // Phase 1: Delete tenant's placeables in the property area
            await DeleteTenantPlaceablesAsync(property, cancellationToken).ConfigureAwait(false);

            // Phase 2: Clear property state and mark as vacant
            RentablePropertySnapshot evictedProperty = new(
                property.Definition,
                PropertyOccupancyStatus.Vacant,
                CurrentTenant: null,
                CurrentOwner: null,
                Residents: Array.Empty<PersonaId>(),
                ActiveRental: null);

            await _propertyRepository.PersistRentalAsync(evictedProperty, cancellationToken).ConfigureAwait(false);

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

    private async Task DeleteTenantPlaceablesAsync(
        RentablePropertySnapshot property,
        CancellationToken cancellationToken)
    {
        try
        {
            Log.Info("Starting placeable deletion for property {InternalName}, tenant {Tenant}.",
                property.Definition.InternalName,
                property.CurrentTenant);

            // Resolve the area ResRef from the property's internal name via RegionIndex
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

            // Get the character ID from the tenant persona
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

            // Delete all placeables for this character in this area
            await DeletePlaceablesForCharacterInAreaAsync(
                areaResRef,
                characterId.Value,
                cancellationToken).ConfigureAwait(false);

            Log.Info("Deleted placeables for character {CharacterId} in area {AreaResRef} (property {InternalName}).",
                characterId.Value,
                areaResRef,
                property.Definition.InternalName);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to delete tenant placeables for property {InternalName}.",
                property.Definition.InternalName);
            throw;
        }
    }

    private string? ResolveAreaResRefForProperty(string internalName)
    {
        // POI.Name matches InternalName by convention
        // Find the settlement containing this POI
        if (!_regionIndex.TryGetSettlementForPointOfInterest(internalName, out SettlementId settlementId))
        {
            return null;
        }

        // Get all POIs for this settlement
        IReadOnlyList<PlaceOfInterest> pois = _regionIndex.GetPointsOfInterestForSettlement(settlementId);
        PlaceOfInterest? poi = pois.FirstOrDefault(p => p.Name == internalName);

        // The area ResRef is stored in POI.ResRef
        return poi?.ResRef;
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

    private async Task DeletePlaceablesForCharacterInAreaAsync(
        string areaResRef,
        Guid characterId,
        CancellationToken cancellationToken)
    {
        Log.Info("Deleting ALL placeables in area {AreaResRef} for evicted property...",
            areaResRef);

        // Get all placeables for this area (not filtering by character)
        List<PersistentObject> areaPlaceables = _objectRepository.GetObjectsForArea(areaResRef)
            .Where(o => o.Type == (int)ObjectTypes.Placeable)
            .ToList();

        Log.Info("Found {TotalCount} placeables in area {AreaResRef} to delete.",
            areaPlaceables.Count,
            areaResRef);

        if (areaPlaceables.Count == 0)
        {
            Log.Info("No placeables found in area {AreaResRef}.",
                areaResRef);
            return;
        }

        // Delete each placeable
        foreach (PersistentObject plc in areaPlaceables)
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                Log.Info("Deleting placeable DB ID {DbId} from area {AreaResRef}.",
                    plc.Id,
                    areaResRef);

                // Delete from database
                await _objectRepository.DeleteObject(plc.Id).ConfigureAwait(false);

                Log.Info("Deleted placeable {PlaceableId} from database. Now attempting in-game destruction...",
                    plc.Id);

                // Try to destroy the in-game object if it exists
                await TryDestroyPlaceableInGameAsync(plc, areaResRef).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to delete placeable {PlaceableId} from area {AreaResRef}.",
                    plc.Id,
                    areaResRef);
            }
        }
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
