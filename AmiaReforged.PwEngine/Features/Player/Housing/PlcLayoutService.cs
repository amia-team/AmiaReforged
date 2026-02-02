using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities.PlayerHousing;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.AreaPersistence;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Runtime;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Properties;
using Anvil.API;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.Player.Housing;

/// <summary>
/// Service for managing PLC layout configurations.
/// Allows players to save and restore their house furnishing arrangements.
/// </summary>
[ServiceBinding(typeof(PlcLayoutService))]
public sealed class PlcLayoutService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Maximum number of layouts a character can save per property.
    /// </summary>
    public const int MaxLayoutsPerProperty = 10;

    private const string PersistPlcLocalInt = "persist_plc";
    private const string CharacterIdLocalString = "character_id";

    private readonly IPlcLayoutRepository _layoutRepository;
    private readonly IRentablePropertyRepository _propertyRepository;
    private readonly RuntimeCharacterService _characterService;
    private readonly PlaceablePersistenceService _persistenceService;

    public PlcLayoutService(
        IPlcLayoutRepository layoutRepository,
        IRentablePropertyRepository propertyRepository,
        RuntimeCharacterService characterService,
        PlaceablePersistenceService persistenceService)
    {
        _layoutRepository = layoutRepository;
        _propertyRepository = propertyRepository;
        _characterService = characterService;
        _persistenceService = persistenceService;
    }

    /// <summary>
    /// Gets all layouts for a property that belong to the specified character.
    /// </summary>
    public async Task<List<PlcLayoutConfiguration>> GetLayoutsAsync(
        PropertyId propertyId,
        Guid characterId,
        CancellationToken cancellationToken = default)
    {
        return await _layoutRepository.GetLayoutsForPropertyAsync(propertyId.Value, characterId, cancellationToken);
    }

    /// <summary>
    /// Gets a specific layout by ID.
    /// </summary>
    public async Task<PlcLayoutConfiguration?> GetLayoutAsync(
        long layoutId,
        CancellationToken cancellationToken = default)
    {
        return await _layoutRepository.GetLayoutByIdAsync(layoutId, cancellationToken);
    }

    /// <summary>
    /// Saves the current PLC layout in an area as a named configuration.
    /// </summary>
    public async Task<LayoutSaveResult> SaveCurrentLayoutAsync(
        PropertyId propertyId,
        Guid characterId,
        string layoutName,
        NwArea area,
        long? existingLayoutId = null,
        CancellationToken cancellationToken = default)
    {
        // Validate ownership
        RentablePropertySnapshot? snapshot = await _propertyRepository.GetSnapshotAsync(propertyId, cancellationToken);
        if (snapshot is null)
        {
            return LayoutSaveResult.Failed("Property not found.");
        }

        if (!CanManageLayouts(characterId, snapshot))
        {
            return LayoutSaveResult.Failed("You must be the tenant or owner of this property to save layouts.");
        }

        // Check layout limit (only for new layouts)
        if (existingLayoutId is null)
        {
            int currentCount = await _layoutRepository.CountLayoutsForPropertyAsync(
                propertyId.Value, characterId, cancellationToken);

            if (currentCount >= MaxLayoutsPerProperty)
            {
                return LayoutSaveResult.Failed(
                    $"You have reached the maximum of {MaxLayoutsPerProperty} layouts for this property.");
            }
        }

        // Check for duplicate name
        bool nameExists = await _layoutRepository.LayoutNameExistsAsync(
            propertyId.Value, characterId, layoutName, existingLayoutId, cancellationToken);

        if (nameExists)
        {
            return LayoutSaveResult.Failed($"A layout named '{layoutName}' already exists.");
        }

        // Collect PLCs in the area belonging to this character
        await NwTask.SwitchToMainThread();

        List<PlcLayoutItem> items = [];
        foreach (NwPlaceable placeable in area.FindObjectsOfTypeInArea<NwPlaceable>())
        {
            // Check if it's a persisted PLC
            LocalVariableInt persistVar = placeable.GetObjectVariable<LocalVariableInt>(PersistPlcLocalInt);
            if (!persistVar.HasValue || persistVar.Value <= 0)
            {
                continue;
            }

            // Check if it belongs to this character
            LocalVariableString characterVar = placeable.GetObjectVariable<LocalVariableString>(CharacterIdLocalString);
            if (!Guid.TryParse(characterVar.Value, out Guid plcOwnerId) || plcOwnerId != characterId)
            {
                continue;
            }

            items.Add(new PlcLayoutItem
            {
                PlcResRef = placeable.ResRef,
                PlcName = placeable.Name,
                X = placeable.Position.X,
                Y = placeable.Position.Y,
                Z = placeable.Position.Z,
                Orientation = placeable.Rotation,
                Scale = placeable.VisualTransform.Scale,
                Appearance = placeable.Appearance?.RowIndex ?? 0,
                HealthOverride = placeable.MaxHP,
                IsPlot = placeable.PlotFlag,
                IsStatic = placeable.IsStatic
            });
        }

        if (items.Count == 0)
        {
            return LayoutSaveResult.Failed("No placeables found in this area that belong to you.");
        }

        // Create or update the layout
        PlcLayoutConfiguration layout = new()
        {
            Id = existingLayoutId ?? 0,
            PropertyId = propertyId.Value,
            CharacterId = characterId,
            Name = layoutName,
            Items = items
        };

        try
        {
            await _layoutRepository.SaveLayoutAsync(layout, cancellationToken);
            return LayoutSaveResult.Success(layout.Id, items.Count);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to save layout configuration");
            return LayoutSaveResult.Failed("An error occurred while saving the layout.");
        }
    }

    /// <summary>
    /// Restores a saved layout by matching inventory items to the configuration.
    /// </summary>
    public async Task<LayoutRestoreResult> RestoreLayoutAsync(
        long layoutId,
        NwPlayer player,
        NwArea area,
        CancellationToken cancellationToken = default)
    {
        PlcLayoutConfiguration? layout = await _layoutRepository.GetLayoutByIdAsync(layoutId, cancellationToken);
        if (layout is null)
        {
            return LayoutRestoreResult.Failed("Layout not found.");
        }

        // Validate ownership
        RentablePropertySnapshot? snapshot = await _propertyRepository.GetSnapshotAsync(
            PropertyId.Parse(layout.PropertyId), cancellationToken);

        if (snapshot is null)
        {
            return LayoutRestoreResult.Failed("Property not found.");
        }

        if (!CanManageLayouts(layout.CharacterId, snapshot))
        {
            return LayoutRestoreResult.Failed("You must be the tenant or owner of this property to restore layouts.");
        }

        // Get the current player's character ID - this is who will own the spawned placeables
        if (!_characterService.TryGetPlayerKey(player, out Guid currentCharacterId))
        {
            return LayoutRestoreResult.Failed("Unable to determine your character. Please relog.");
        }

        await NwTask.SwitchToMainThread();

        NwCreature? creature = player.ControlledCreature;
        if (creature is null)
        {
            return LayoutRestoreResult.Failed("No controlled creature found.");
        }

        // Collect available furniture items from inventory
        List<FurnitureItem> availableItems = CollectFurnitureItems(creature);

        int placedCount = 0;
        List<string> missingItems = [];

        foreach (PlcLayoutItem layoutItem in layout.Items)
        {
            // Find a matching item in inventory
            FurnitureItem? matchingItem = FindMatchingItem(availableItems, layoutItem.PlcResRef, layoutItem.PlcName);

            if (matchingItem is null)
            {
                missingItems.Add($"{layoutItem.PlcName} ({layoutItem.PlcResRef})");
                continue;
            }

            // Remove from available items so it won't be used again
            availableItems.Remove(matchingItem);

            // Spawn the placeable
            Location spawnLocation = Location.Create(area, new System.Numerics.Vector3(
                layoutItem.X, layoutItem.Y, layoutItem.Z), layoutItem.Orientation);

            NwPlaceable? placeable = NwPlaceable.Create(matchingItem.ResRef, spawnLocation);
            if (placeable is null)
            {
                missingItems.Add($"{layoutItem.PlcName} (spawn failed)");
                continue;
            }

            // Configure the placeable
            placeable.Name = layoutItem.PlcName;
            placeable.PlotFlag = layoutItem.IsPlot;
            placeable.IsStatic = layoutItem.IsStatic;

            if (layoutItem.HealthOverride > 0)
            {
                placeable.HP = layoutItem.HealthOverride;
            }

            // Apply appearance
            if (layoutItem.Appearance > 0)
            {
                try
                {
                    PlaceableTableEntry appearanceRow = NwGameTables.PlaceableTable.GetRow(layoutItem.Appearance);
                    placeable.Appearance = appearanceRow;
                }
                catch
                {
                    // Ignore invalid appearance rows
                }
            }

            // Apply scale
            if (Math.Abs(layoutItem.Scale - 1.0f) > 0.001f)
            {
                placeable.VisualTransform.Scale = layoutItem.Scale;
            }

            // Mark as persistent and set character association to the CURRENT player
            placeable.GetObjectVariable<LocalVariableInt>(PersistPlcLocalInt).Value = 1;
            placeable.GetObjectVariable<LocalVariableString>(CharacterIdLocalString).Value = currentCharacterId.ToString();

            // Store source item data for recovery
            if (matchingItem.SerializedData is not null && matchingItem.SerializedData.Length > 0)
            {
                string base64Data = Convert.ToBase64String(matchingItem.SerializedData);
                placeable.GetObjectVariable<LocalVariableString>("source_item_data").Value = base64Data;
            }

            // Persist the placeable to the database
            try
            {
                await _persistenceService.SaveSinglePlaceable(placeable);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to persist placeable {PlaceableName} during layout restore", placeable.Name);
                // Continue with the rest of the layout even if one fails to persist
            }

            // Destroy the inventory item
            matchingItem.Item.Destroy();

            placedCount++;
        }

        return new LayoutRestoreResult(
            IsSuccess: true,
            Message: BuildRestoreResultMessage(placedCount, missingItems),
            PlacedCount: placedCount,
            MissingItems: missingItems);
    }

    /// <summary>
    /// Deletes a layout configuration.
    /// </summary>
    public async Task<bool> DeleteLayoutAsync(
        long layoutId,
        Guid requestingCharacterId,
        CancellationToken cancellationToken = default)
    {
        PlcLayoutConfiguration? layout = await _layoutRepository.GetLayoutByIdAsync(layoutId, cancellationToken);
        if (layout is null)
        {
            return false;
        }

        // Only the owner can delete their layouts
        if (layout.CharacterId != requestingCharacterId)
        {
            return false;
        }

        await _layoutRepository.DeleteLayoutAsync(layoutId, cancellationToken);
        return true;
    }

    private static bool CanManageLayouts(Guid characterId, RentablePropertySnapshot snapshot)
    {
        // Check if character is owner
        if (snapshot.CurrentOwner is not null
            && snapshot.CurrentOwner.Value.Type == PersonaType.Character
            && Guid.TryParse(snapshot.CurrentOwner.Value.Value, out Guid ownerId)
            && ownerId == characterId)
        {
            return true;
        }

        // Check if character is tenant
        if (snapshot.CurrentTenant is not null
            && snapshot.CurrentTenant.Value.Type == PersonaType.Character
            && Guid.TryParse(snapshot.CurrentTenant.Value.Value, out Guid tenantId)
            && tenantId == characterId)
        {
            return true;
        }

        // Check if character is a resident
        foreach (PersonaId resident in snapshot.Residents)
        {
            if (resident.Type == PersonaType.Character
                && Guid.TryParse(resident.Value, out Guid residentId)
                && residentId == characterId)
            {
                return true;
            }
        }

        return false;
    }

    private static List<FurnitureItem> CollectFurnitureItems(NwCreature creature)
    {
        List<FurnitureItem> items = [];

        foreach (NwItem item in creature.Inventory.Items)
        {
            LocalVariableInt marker = item.GetObjectVariable<LocalVariableInt>("is_plc");
            if (!marker.HasValue || marker.Value <= 0)
            {
                continue;
            }

            LocalVariableString resrefVar = item.GetObjectVariable<LocalVariableString>("plc_resref");
            if (!resrefVar.HasValue || string.IsNullOrWhiteSpace(resrefVar.Value))
            {
                continue;
            }

            // Get the display name (from plc_name or item name)
            LocalVariableString nameVar = item.GetObjectVariable<LocalVariableString>("plc_name");
            string displayName = nameVar.HasValue && !string.IsNullOrWhiteSpace(nameVar.Value)
                ? nameVar.Value
                : item.Name;

            byte[]? serializedData = item.Serialize();

            items.Add(new FurnitureItem(item, resrefVar.Value, displayName, serializedData));
        }

        return items;
    }

    private static FurnitureItem? FindMatchingItem(
        List<FurnitureItem> availableItems,
        string plcResRef,
        string plcName)
    {
        // First try exact match on both resref and name
        FurnitureItem? exactMatch = availableItems.FirstOrDefault(
            i => string.Equals(i.ResRef, plcResRef, StringComparison.OrdinalIgnoreCase)
                 && string.Equals(i.DisplayName, plcName, StringComparison.OrdinalIgnoreCase));

        if (exactMatch is not null)
        {
            return exactMatch;
        }

        // Fall back to resref-only match if no exact match
        return availableItems.FirstOrDefault(
            i => string.Equals(i.ResRef, plcResRef, StringComparison.OrdinalIgnoreCase));
    }

    private static string BuildRestoreResultMessage(int placedCount, List<string> missingItems)
    {
        if (missingItems.Count == 0)
        {
            return $"Successfully placed {placedCount} item(s).";
        }

        if (placedCount == 0)
        {
            return $"Could not place any items. Missing: {string.Join(", ", missingItems.Take(5))}" +
                   (missingItems.Count > 5 ? $" and {missingItems.Count - 5} more." : ".");
        }

        return $"Placed {placedCount} item(s). Missing: {string.Join(", ", missingItems.Take(5))}" +
               (missingItems.Count > 5 ? $" and {missingItems.Count - 5} more." : ".");
    }

    private sealed record FurnitureItem(NwItem Item, string ResRef, string DisplayName, byte[]? SerializedData);
}

/// <summary>
/// Result of a layout save operation.
/// </summary>
public sealed record LayoutSaveResult(bool IsSuccess, string Message, long LayoutId = 0, int ItemCount = 0)
{
    public static LayoutSaveResult Success(long layoutId, int itemCount)
        => new(true, $"Layout saved with {itemCount} item(s).", layoutId, itemCount);

    public static LayoutSaveResult Failed(string message)
        => new(false, message);
}

/// <summary>
/// Result of a layout restore operation.
/// </summary>
public sealed record LayoutRestoreResult(
    bool IsSuccess,
    string Message,
    int PlacedCount = 0,
    IReadOnlyList<string>? MissingItems = null)
{
    public static LayoutRestoreResult Failed(string message)
        => new(false, message);
}
