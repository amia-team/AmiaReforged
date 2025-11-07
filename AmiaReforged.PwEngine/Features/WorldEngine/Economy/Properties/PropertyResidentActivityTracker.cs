using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Properties;

/// <summary>
/// Tracks when property residents enter their rented areas and updates LastOccupantSeenUtc.
/// This is critical for the eviction system to know if tenants are still active.
/// </summary>
[ServiceBinding(typeof(PropertyResidentActivityTracker))]
public sealed class PropertyResidentActivityTracker
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly IRentablePropertyRepository _propertyRepository;
    private readonly Dictionary<string, List<RentablePropertySnapshot>> _areaToPropertiesCache = new();
    private bool _initialized;

    public PropertyResidentActivityTracker(IRentablePropertyRepository propertyRepository)
    {
        _propertyRepository = propertyRepository ?? throw new ArgumentNullException(nameof(propertyRepository));
        
        NwModule.Instance.OnModuleLoad += OnModuleLoad;
    }

    private void OnModuleLoad(ModuleEvents.OnModuleLoad obj)
    {
        _ = InitializeAsync();
    }

    private async Task InitializeAsync()
    {
        try
        {
            Log.Info("Initializing property resident activity tracker...");

            // Load all properties and build area -> properties mapping
            List<RentablePropertySnapshot> allProperties = 
                await _propertyRepository.GetAllPropertiesAsync(default).ConfigureAwait(false);

            await NwTask.SwitchToMainThread();

            _areaToPropertiesCache.Clear();

            // Group properties by area ResRef
            foreach (RentablePropertySnapshot property in allProperties)
            {
                // Only track rented properties with active residents
                if (property.OccupancyStatus != PropertyOccupancyStatus.Rented)
                {
                    continue;
                }

                if (property.Residents.Count == 0)
                {
                    continue;
                }

                // The property's InternalName should match a POI, which has an area ResRef
                // For now, we'll use the InternalName as a key - this may need refinement
                string areaKey = property.Definition.InternalName;

                if (!_areaToPropertiesCache.ContainsKey(areaKey))
                {
                    _areaToPropertiesCache[areaKey] = new List<RentablePropertySnapshot>();
                }

                _areaToPropertiesCache[areaKey].Add(property);
            }

            // Subscribe to area enter events for all areas
            foreach (NwArea area in NwModule.Instance.Areas)
            {
                area.OnEnter += HandleAreaEnter;
            }

            _initialized = true;

            Log.Info("Property resident activity tracker initialized. Monitoring {PropertyCount} rented properties across {AreaCount} areas.",
                allProperties.Count(p => p.OccupancyStatus == PropertyOccupancyStatus.Rented),
                _areaToPropertiesCache.Count);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to initialize property resident activity tracker.");
        }
    }

    private void HandleAreaEnter(AreaEvents.OnEnter obj)
    {
        if (!_initialized)
        {
            return;
        }

        if (obj.EnteringObject is not NwCreature creature)
        {
            return;
        }

        if (!creature.IsPlayerControlled)
        {
            return;
        }

        NwPlayer? player = creature.ControllingPlayer;
        if (player == null)
        {
            return;
        }

        // Check if this area has any rented properties
        string? areaTag = obj.Area.Tag;
        string? areaResRef = obj.Area.ResRef;

        if (string.IsNullOrEmpty(areaTag) && string.IsNullOrEmpty(areaResRef))
        {
            return;
        }

        _ = ProcessAreaEnterAsync(creature, player, areaTag, areaResRef);
    }

    private async Task ProcessAreaEnterAsync(NwCreature creature, NwPlayer player, string? areaTag, string? areaResRef)
    {
        try
        {
            // Try to resolve character persona
            if (!TryGetCharacterPersona(creature, out PersonaId? personaId))
            {
                return;
            }

            // Check cache for properties in this area
            List<RentablePropertySnapshot>? properties = null;

            if (!string.IsNullOrEmpty(areaTag) && _areaToPropertiesCache.TryGetValue(areaTag, out List<RentablePropertySnapshot>? propertiesByTag))
            {
                properties = propertiesByTag;
            }
            else if (!string.IsNullOrEmpty(areaResRef) && _areaToPropertiesCache.TryGetValue(areaResRef, out List<RentablePropertySnapshot>? propertiesByResRef))
            {
                properties = propertiesByResRef;
            }

            if (properties == null || properties.Count == 0)
            {
                return;
            }

            // Check if this character is a resident of any property in this area
            foreach (RentablePropertySnapshot property in properties)
            {
                if (!IsResident(personaId.Value, property))
                {
                    continue;
                }

                // Update last seen timestamp
                await UpdateLastSeenAsync(property, personaId.Value).ConfigureAwait(false);
                
                Log.Info("Updated last seen for {Persona} entering property {PropertyId} ({InternalName}).",
                    personaId,
                    property.Definition.Id,
                    property.Definition.InternalName);
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to process area enter for property activity tracking.");
        }
    }

    private async Task UpdateLastSeenAsync(RentablePropertySnapshot property, PersonaId personaId)
    {
        try
        {
            // Refresh the property snapshot from database
            RentablePropertySnapshot? current = await _propertyRepository
                .GetSnapshotAsync(property.Definition.Id, default)
                .ConfigureAwait(false);

            if (current == null)
            {
                Log.Warn("Property {PropertyId} not found when updating last seen.", property.Definition.Id);
                return;
            }

            // Only update if still rented with an active rental agreement
            if (current.OccupancyStatus != PropertyOccupancyStatus.Rented || current.ActiveRental == null)
            {
                return;
            }

            DateTimeOffset now = DateTimeOffset.UtcNow;

            // Create updated rental agreement with new last seen timestamp
            RentalAgreementSnapshot updatedRental = current.ActiveRental with
            {
                LastOccupantSeenUtc = now
            };

            // Create updated property snapshot
            RentablePropertySnapshot updatedProperty = current with
            {
                ActiveRental = updatedRental
            };

            // Persist the update
            await _propertyRepository.PersistRentalAsync(updatedProperty, default).ConfigureAwait(false);

            // Update our cache
            await NwTask.SwitchToMainThread();
            UpdateCache(updatedProperty);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to update last seen for property {PropertyId}.", property.Definition.Id);
        }
    }

    private void UpdateCache(RentablePropertySnapshot updatedProperty)
    {
        string areaKey = updatedProperty.Definition.InternalName;
        
        if (!_areaToPropertiesCache.TryGetValue(areaKey, out List<RentablePropertySnapshot>? properties))
        {
            return;
        }

        // Replace the old snapshot with the updated one
        for (int i = 0; i < properties.Count; i++)
        {
            if (properties[i].Definition.Id.Equals(updatedProperty.Definition.Id))
            {
                properties[i] = updatedProperty;
                break;
            }
        }
    }

    private static bool IsResident(PersonaId personaId, RentablePropertySnapshot property)
    {
        // Check if tenant
        if (property.CurrentTenant != null && property.CurrentTenant.Equals(personaId))
        {
            return true;
        }

        // Check if owner
        if (property.CurrentOwner != null && property.CurrentOwner.Equals(personaId))
        {
            return true;
        }

        // Check residents list
        return property.Residents.Any(resident => resident.Equals(personaId));
    }

    private static bool TryGetCharacterPersona(NwCreature creature, out PersonaId? personaId)
    {
        personaId = null;

        try
        {
            // Try to get character ID from local variable or other mechanism
            LocalVariableString charIdVar = creature.GetObjectVariable<LocalVariableString>("character_id");
            if (!string.IsNullOrEmpty(charIdVar.Value) && Guid.TryParse(charIdVar.Value, out Guid characterGuid))
            {
                personaId = PersonaId.FromCharacter(new CharacterId(characterGuid));
                return true;
            }

            // Fallback: try to get from UUID
            string uuid = creature.UUID.ToString();
            if (Guid.TryParse(uuid, out Guid uuidGuid))
            {
                personaId = PersonaId.FromCharacter(new CharacterId(uuidGuid));
                return true;
            }

            return false;
        }
        catch
        {
            return false;
        }
    }
}
