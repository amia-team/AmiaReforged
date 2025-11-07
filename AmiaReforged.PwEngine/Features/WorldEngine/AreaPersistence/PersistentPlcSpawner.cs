using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities;
using Anvil.API;
using Anvil.API.Events;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.AreaPersistence;

/// <summary>
/// Handles placement of furniture items from player inventory.
/// When a player activates a furniture item, it creates a persistent placeable
/// and stores the original item data for potential recovery (e.g., during evictions).
/// </summary>
[ServiceBinding(typeof(PersistentPlcSpawner))]
public class PersistentPlcSpawner
{
    private const string PersistPlcLocalInt = "persist_plc";
    private const string SavePlcTag = "persistent_plc_spawner";
    private const string CharacterIdLocalString = "character_id";
    private const string DatabaseIdLocalInt = "db_id";
    
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly PlaceablePersistenceService _plcPersistenceService;
    private readonly IPersistentObjectRepository _objectRepository;

    public PersistentPlcSpawner(
        PlaceablePersistenceService plcPersistenceService,
        IPersistentObjectRepository objectRepository)
    {
        _plcPersistenceService = plcPersistenceService;
        _objectRepository = objectRepository;

        NwModule.Instance.OnActivateItem += HandlePersistentSpawner;
        
        Log.Info("PersistentPlcSpawner initialized - listening for furniture placement");
    }

    private void HandlePersistentSpawner(ModuleEvents.OnActivateItem obj)
    {
        if (obj.ActivatedItem.Tag != SavePlcTag)
        {
            return;
        }

        NwCreature? activator = obj.ItemActivator;
        if (activator is null)
        {
            Log.Warn("Null activator tried to place furniture item");
            return;
        }

        NwPlayer? player = activator.ControllingPlayer;
        if (player is null)
        {
            Log.Warn("Non-player controlled creature tried to place furniture item");
            return;
        }

        NwArea? area = activator.Area;
        if (area is null)
        {
            player.SendServerMessage("You must be in a valid area to place furniture.", ColorConstants.Orange);
            return;
        }

        // Enter target mode to select where to place the placeable
        player.EnterTargetMode(targetData => HandleFurniturePlacement(
            targetData, 
            player, 
            activator, 
            obj.ActivatedItem), 
            new TargetModeSettings
            {
                CursorType = MouseCursor.Create,
                ValidTargets = ObjectTypes.Tile
            });
    }

    private void HandleFurniturePlacement(
        ModuleEvents.OnPlayerTarget targetData, 
        NwPlayer player,
        NwCreature creature,
        NwItem furnitureItem)
    {
        if (!targetData.IsCancelled && targetData.TargetPosition != null)
        {
            _ = PlaceFurnitureAsync(targetData, player, creature, furnitureItem);
        }
    }

    private async Task PlaceFurnitureAsync(
        ModuleEvents.OnPlayerTarget targetData,
        NwPlayer player,
        NwCreature creature,
        NwItem furnitureItem)
    {
        try
        {
            await NwTask.SwitchToMainThread();

            // Verify the item still exists
            if (!furnitureItem.IsValid)
            {
                player.SendServerMessage("The furniture item is no longer valid.", ColorConstants.Orange);
                return;
            }

            NwArea? area = creature.Area;
            if (area is null)
            {
                player.SendServerMessage("You are no longer in a valid area.", ColorConstants.Orange);
                return;
            }

            // Serialize the item BEFORE creating the placeable (so we can recover it later)
            byte[]? sourceItemData = furnitureItem.Serialize();
            if (sourceItemData is null)
            {
                Log.Error("Failed to serialize furniture item {ResRef} for player {PlayerName}",
                    furnitureItem.ResRef, player.PlayerName);
                player.SendServerMessage("Failed to serialize the furniture item.", ColorConstants.Orange);
                return;
            }

            // Get placeable resref from item (should be stored as a variable or in the item's description)
            string placeableResRef = furnitureItem.ResRef; // TODO: May need different logic based on your item setup
            
            Location location = Location.Create(area, targetData.TargetPosition, creature.Rotation);
            
            // Create the placeable
            NwPlaceable? placeable = NwPlaceable.Create(placeableResRef, location);
            if (placeable is null)
            {
                Log.Error("Failed to create placeable from furniture item {ResRef} for player {PlayerName}",
                    furnitureItem.ResRef, player.PlayerName);
                player.SendServerMessage("Failed to create the furniture placeable.", ColorConstants.Orange);
                return;
            }

            // Set placeable name from item
            placeable.Name = furnitureItem.Name;
            
            // Mark it as persistent and associate with character
            placeable.GetObjectVariable<LocalVariableInt>(PersistPlcLocalInt).Value = 1;
            placeable.GetObjectVariable<LocalVariableString>(CharacterIdLocalString).Value = creature.UUID.ToString();

            // Save to database WITH source item data
            PersistentObject persistentObject = new()
            {
                Type = (int)ObjectTypes.Placeable,
                Serialized = placeable.Serialize() ?? Array.Empty<byte>(),
                CharacterId = Guid.TryParse(creature.UUID.ToString(), out Guid characterId) ? characterId : null,
                SourceItemData = sourceItemData, // Store the original item for foreclosure recovery!
                Location = new SavedLocation
                {
                    AreaResRef = area.ResRef,
                    X = location.Position.X,
                    Y = location.Position.Y,
                    Z = location.Position.Z,
                    Orientation = location.Rotation
                }
            };

            await _objectRepository.SaveObject(persistentObject);
            await NwTask.SwitchToMainThread();

            // Store the DB ID on the placeable
            placeable.GetObjectVariable<LocalVariableInt>(DatabaseIdLocalInt).Value = (int)persistentObject.Id;

            // Destroy the original item
            furnitureItem.Destroy();

            player.SendServerMessage($"Placed '{placeable.Name}' furniture.", ColorConstants.Green);
            
            Log.Info("Player {PlayerName} placed furniture {PlaceableName} (ID: {Id}) with source item data stored",
                player.PlayerName, placeable.Name, persistentObject.Id);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to place furniture for player {PlayerName}", player.PlayerName);
            
            await NwTask.SwitchToMainThread();
            player.SendServerMessage("An error occurred while placing the furniture.", ColorConstants.Orange);
        }
    }
}

