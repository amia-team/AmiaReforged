using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities;
using Anvil.API;
using Anvil.Services;
using NLog;
using NWN.Core;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.AreaPersistence;

[ServiceBinding(typeof(PlaceablePersistenceService))]
public class PlaceablePersistenceService
{
    private const string SavedModeLocalInt = "saved_mode";
    private const string DatabaseIdLocalInt = "db_id";
    private const string PersistPlcLocalInt = "persist_plc";
    private const string CharacterIdLocalString = "character_id";

    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly IPersistentObjectRepository _objectRepository;

    public PlaceablePersistenceService(IPersistentObjectRepository objectRepository)
    {
        _objectRepository = objectRepository;

        foreach (NwArea area in NwModule.Instance.Areas)
        {
            RespawnPlcs(area);
        }
    }

    private void RespawnPlcs(NwArea area)
    {
        List<PersistentObject> persistentObjects = _objectRepository.GetObjectsForArea(area.ResRef)
            .Where(o => o.Type == (int)ObjectTypes.Placeable).ToList();

        if (persistentObjects.Count == 0)
        {
            return;
        }

        foreach (PersistentObject obj in persistentObjects)
        {
            if (obj.Location is null)
                continue;

            Location l = Location.Create(area, obj.Location.Position, obj.Location.Orientation);
            NwPlaceable? plc = NwPlaceable.Deserialize(obj.Serialized);
            if (plc is null)
            {
                Log.Error("Failed to deserialize placeable persistent object with ID {0}", obj.Id);
                continue;
            }

            NWScript.SetLocalInt(plc, DatabaseIdLocalInt, (int)obj.Id);
            ApplyCharacterAssociation(plc, obj.CharacterId);
            plc.Location = l;
        }
    }

    public async Task SaveSinglePlaceable(NwPlaceable placeable)
    {
        await NwTask.SwitchToMainThread();

        NwArea? area = placeable.Area;
        if (area is null)
        {
            Log.Warn("Placeable {0} is not in an area, cannot save.", placeable.ResRef);
            return;
        }

        Guid? characterId = ExtractCharacterId(placeable);

        // Extract source item data if present
        byte[]? sourceItemData = ExtractSourceItemData(placeable);

        PersistentObject persistentObject = new PersistentObject
        {
            Type = (int)ObjectTypes.Placeable,
            Serialized = placeable.Serialize() ?? Array.Empty<byte>(),
            CharacterId = characterId,
            SourceItemData = sourceItemData,
            Location = new SavedLocation
            {
                AreaResRef = area.ResRef,
                X = placeable.Location.Position.X,
                Y = placeable.Location.Position.Y,
                Z = placeable.Location.Position.Z,
                Orientation = placeable.Location.Rotation
            }
        };

        // Get if it already has the db id saved to the placeable
        LocalVariableInt dbIdVar = placeable.GetObjectVariable<LocalVariableInt>(DatabaseIdLocalInt);
        long dbId = dbIdVar.Value;
        if (dbId > 0)
        {
            // Update existing
            persistentObject.Id = dbId;
        }

        await _objectRepository.SaveObject(persistentObject);
        await NwTask.SwitchToMainThread();
        placeable.GetObjectVariable<LocalVariableInt>(DatabaseIdLocalInt).Value = (int)persistentObject.Id;
    }

    public async Task SaveAreaPlaceables(NwArea area)
    {
        LocalVariableInt persistenceModeVar = area.GetObjectVariable<LocalVariableInt>(SavedModeLocalInt);
        ObjectPersistenceMode persistenceMode = (ObjectPersistenceMode)persistenceModeVar.Value;
        if (persistenceMode == ObjectPersistenceMode.None) return;

        await SavePlaceables(area, persistenceMode);
    }

    public async Task<PersistentObject?> GetPersistentObjectAsync(NwPlaceable placeable)
    {
        await NwTask.SwitchToMainThread();

        LocalVariableInt dbVar = placeable.GetObjectVariable<LocalVariableInt>(DatabaseIdLocalInt);
        long id = dbVar.Value;

        if (id <= 0)
        {
            return null;
        }

        return await _objectRepository.GetObject(id);
    }

    private async Task SavePlaceables(NwArea area, ObjectPersistenceMode persistenceMode)
    {
        IEnumerable<NwPlaceable> placeables = area.FindObjectsOfTypeInArea<NwPlaceable>();
        foreach (NwPlaceable nwPlaceable in placeables)
        {
            await NwTask.SwitchToMainThread();
            // Check that it doesn't already have an id so we know if we're updating or not.
            if (ShouldSkipPlc(nwPlaceable))
            {
                continue;
            }

            // Get if it already has the db id saved to the placeable
            LocalVariableInt dbIdVar = nwPlaceable.GetObjectVariable<LocalVariableInt>(DatabaseIdLocalInt);
            long dbId = dbIdVar.Value;

            byte[]? serialized = nwPlaceable.Serialize();
            if (serialized is null)
            {
                Log.Error("Failed to serialize placeable {0} in area {1}", nwPlaceable.ResRef, area.ResRef);
                continue;
            }

            Guid? characterId = ExtractCharacterId(nwPlaceable);

            // Extract source item data if present
            byte[]? sourceItemData = ExtractSourceItemData(nwPlaceable);

            PersistentObject persistentObject = new PersistentObject
            {
                Type = (int)ObjectTypes.Placeable,
                Serialized = serialized,
                CharacterId = characterId,
                SourceItemData = sourceItemData,
                Location = new SavedLocation
                {
                    AreaResRef = area.ResRef,
                    X = nwPlaceable.Location.Position.X,
                    Y = nwPlaceable.Location.Position.Y,
                    Z = nwPlaceable.Location.Position.Z,
                    Orientation = nwPlaceable.Location.Rotation
                }
            };

            if (dbId > 0)
            {
                // Update existing
                persistentObject.Id = dbId;
            }

            await _objectRepository.SaveObject(persistentObject);
            await NwTask.SwitchToMainThread();
            nwPlaceable.GetObjectVariable<LocalVariableInt>(DatabaseIdLocalInt).Value = (int)persistentObject.Id;
        }

        return;

        bool ShouldSkipPlc(NwPlaceable plc)
        {
            if (persistenceMode != ObjectPersistenceMode.JobSystemOnly) return false;
            LocalVariableInt isJobSystemPlcVar = plc.GetObjectVariable<LocalVariableInt>(PersistPlcLocalInt);
            return isJobSystemPlcVar.Value == 0;
        }
    }

    public async Task DeletePlaceableAsync(NwPlaceable placeable)
    {
        await NwTask.SwitchToMainThread();

        LocalVariableInt dbVar = placeable.GetObjectVariable<LocalVariableInt>(DatabaseIdLocalInt);
        long id = dbVar.Value;
        if (id <= 0)
        {
            return;
        }

        await _objectRepository.DeleteObject(id);
        await NwTask.SwitchToMainThread();
        dbVar.Delete();
        LocalVariableString characterVar = placeable.GetObjectVariable<LocalVariableString>(CharacterIdLocalString);
        if (!string.IsNullOrEmpty(characterVar.Value))
        {
            characterVar.Delete();
        }

        if (placeable.IsValid)
        {
            placeable.Destroy();
        }
    }

    private static Guid? ExtractCharacterId(NwGameObject placeable)
    {
        LocalVariableString characterVar = placeable.GetObjectVariable<LocalVariableString>(CharacterIdLocalString);
        if (Guid.TryParse(characterVar.Value, out Guid parsed))
        {
            return parsed;
        }

        return null;
    }

    private static byte[]? ExtractSourceItemData(NwGameObject placeable)
    {
        LocalVariableString sourceItemVar = placeable.GetObjectVariable<LocalVariableString>("source_item_data");
        if (string.IsNullOrWhiteSpace(sourceItemVar.Value))
        {
            return null;
        }

        try
        {
            return Convert.FromBase64String(sourceItemVar.Value);
        }
        catch
        {
            return null;
        }
    }

    private static void ApplyCharacterAssociation(NwPlaceable placeable, Guid? characterId)
    {
        LocalVariableString characterVar = placeable.GetObjectVariable<LocalVariableString>(CharacterIdLocalString);

        if (characterId.HasValue && characterId.Value != Guid.Empty)
        {
            characterVar.Value = characterId.Value.ToString();
        }
        else if (!string.IsNullOrEmpty(characterVar.Value))
        {
            characterVar.Delete();
        }
    }
}

/// <summary>
/// This determines what is saved as a persistent object in an area
/// </summary>
internal enum ObjectPersistenceMode
{
    None = 0,
    JobSystemOnly = 1,
    All = 2
}
