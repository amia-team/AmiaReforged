using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Storage;

/// <summary>
/// Service for managing personal item storage at banks.
/// Storage capacity starts at 10 slots and can be upgraded to 100 slots.
/// Cost: 50k for first upgrade (10→20), then +100k per 10-slot tier.
/// </summary>
[ServiceBinding(typeof(IPersonalStorageService))]
public class PersonalStorageService : IPersonalStorageService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly PwEngineContext _context;

    private const int InitialCapacity = 10;
    private const int MaxCapacity = 100;
    private const int SlotsPerUpgrade = 10;
    private const int FirstUpgradeCost = 50_000;
    private const int SubsequentUpgradeCostIncrease = 100_000;

    public PersonalStorageService(PwEngineContext context)
    {
        _context = context;
    }

    public async Task<Database.Entities.Storage> GetOrCreatePersonalStorageAsync(
        CoinhouseTag coinhouseTag,
        Guid characterId,
        CancellationToken cancellationToken = default)
    {
        string locationKey = BuildLocationKey(coinhouseTag, characterId);

        // Try to find existing storage
        Database.Entities.Storage? existing = await _context.Warehouses
            .Include(w => w.Items)
            .FirstOrDefaultAsync(
                w => w.StorageType == nameof(StorageLocationType.PersonalStorage)
                     && w.LocationKey == locationKey,
                cancellationToken);

        if (existing is not null)
        {
            Log.Debug($"Found existing personal storage for character {characterId} " +
                      $"at bank '{coinhouseTag}' with {existing.Items?.Count ?? 0}/{existing.Capacity} items.");
            return existing;
        }

        // Create new personal storage with initial capacity
        Database.Entities.Storage storage = new()
        {
            EngineId = Guid.NewGuid(),
            Capacity = InitialCapacity,
            StorageType = StorageLocationType.PersonalStorage.ToString(),
            LocationKey = locationKey
        };

        _context.Warehouses.Add(storage);
        await _context.SaveChangesAsync(cancellationToken);

        Log.Info($"Created new personal storage for character {characterId} " +
                 $"at bank '{coinhouseTag}' with {InitialCapacity} slots (Storage ID: {storage.Id}).");

        return storage;
    }

    public async Task<StorageResult> StoreItemAsync(
        CoinhouseTag coinhouseTag,
        Guid characterId,
        string itemName,
        byte[] itemData,
        CancellationToken cancellationToken = default)
    {
        Database.Entities.Storage storage = await GetOrCreatePersonalStorageAsync(
            coinhouseTag,
            characterId,
            cancellationToken);

        // Reload with items to check capacity
        int currentItemCount = await _context.WarehouseItems
            .CountAsync(i => i.WarehouseId == storage.Id, cancellationToken);

        if (currentItemCount >= storage.Capacity)
        {
            return new StorageResult(false, 
                $"Storage is full ({currentItemCount}/{storage.Capacity} slots). Purchase additional capacity to store more items.");
        }

        StoredItem item = new()
        {
            ItemData = itemData,
            Owner = characterId,
            Name = itemName,
            WarehouseId = storage.Id
        };

        _context.WarehouseItems.Add(item);
        await _context.SaveChangesAsync(cancellationToken);

        int newCount = currentItemCount + 1;
        Log.Info($"Stored item '{itemName}' for character {characterId} at bank '{coinhouseTag}' " +
                 $"(Item ID: {item.Id}, Usage: {newCount}/{storage.Capacity}).");

        return new StorageResult(true, 
            $"Item stored successfully. Storage: {newCount}/{storage.Capacity} slots used.");
    }

    public async Task<List<StoredItem>> GetStoredItemsAsync(
        CoinhouseTag coinhouseTag,
        Guid characterId,
        CancellationToken cancellationToken = default)
    {
        string locationKey = BuildLocationKey(coinhouseTag, characterId);

        // First, get the warehouse for this location and character
        Database.Entities.Storage? warehouse = await _context.Warehouses
            .FirstOrDefaultAsync(w => w.LocationKey == locationKey
                                     && w.StorageType == nameof(StorageLocationType.PersonalStorage)
                                     && w.OwnerId == characterId,
                cancellationToken);

        // If no warehouse exists, return empty list
        if (warehouse == null)
        {
            Log.Debug($"No storage found for character {characterId} at bank '{coinhouseTag}'.");
            return new List<StoredItem>();
        }

        // Now get items directly by warehouse ID
        List<StoredItem> items = await _context.WarehouseItems
            .Where(item => item.WarehouseId == warehouse.Id)
            .ToListAsync(cancellationToken);

        Log.Debug($"Retrieved {items.Count} stored items for character {characterId} " +
                  $"at bank '{coinhouseTag}' (Warehouse ID: {warehouse.Id}).");

        return items;
    }

    public async Task<StoredItem?> WithdrawItemAsync(
        long storedItemId,
        Guid characterId,
        CancellationToken cancellationToken = default)
    {
        StoredItem? item = await _context.WarehouseItems
            .FirstOrDefaultAsync(
                i => i.Id == storedItemId && i.Owner == characterId,
                cancellationToken);

        if (item == null)
        {
            Log.Warn($"Character {characterId} attempted to withdraw item {storedItemId} but it was not found or not owned by them.");
            return null;
        }

        _context.WarehouseItems.Remove(item);
        await _context.SaveChangesAsync(cancellationToken);

        Log.Info($"Character {characterId} withdrew item {storedItemId} from storage " +
                 $"(Warehouse ID: {item.WarehouseId}).");

        return item;
    }

    public async Task<StorageCapacityInfo> GetStorageCapacityAsync(
        CoinhouseTag coinhouseTag,
        Guid characterId,
        CancellationToken cancellationToken = default)
    {
        Database.Entities.Storage storage = await GetOrCreatePersonalStorageAsync(
            coinhouseTag,
            characterId,
            cancellationToken);

        int usedSlots = await _context.WarehouseItems
            .CountAsync(i => i.WarehouseId == storage.Id, cancellationToken);

        int availableSlots = storage.Capacity - usedSlots;

        return new StorageCapacityInfo(storage.Capacity, usedSlots, availableSlots);
    }

    public async Task<StorageUpgradeResult> UpgradeStorageCapacityAsync(
        CoinhouseTag coinhouseTag,
        Guid characterId,
        CancellationToken cancellationToken = default)
    {
        Database.Entities.Storage storage = await GetOrCreatePersonalStorageAsync(
            coinhouseTag,
            characterId,
            cancellationToken);

        int currentCapacity = storage.Capacity;

        // Check if already at max capacity
        if (currentCapacity >= MaxCapacity)
        {
            return new StorageUpgradeResult(false, 
                $"Storage is already at maximum capacity ({MaxCapacity} slots).", 
                currentCapacity, 
                0);
        }

        // Calculate upgrade cost
        int upgradeCost = CalculateUpgradeCost(currentCapacity);
        int newCapacity = currentCapacity + SlotsPerUpgrade;

        // Note: Actual gold deduction should be handled by the caller (BankWindowView)
        // This service just manages the storage upgrade itself

        // Update capacity
        storage.Capacity = newCapacity;
        await _context.SaveChangesAsync(cancellationToken);

        Log.Info($"Upgraded storage capacity for character {characterId} at bank '{coinhouseTag}' " +
                 $"from {currentCapacity} to {newCapacity} slots (Cost: {upgradeCost} gold).");

        return new StorageUpgradeResult(true, 
            $"Storage upgraded successfully! New capacity: {newCapacity} slots.", 
            newCapacity, 
            upgradeCost);
    }

    public int CalculateUpgradeCost(int currentCapacity)
    {
        if (currentCapacity >= MaxCapacity)
        {
            return 0; // Already at max
        }

        // First upgrade (10→20): 50k
        if (currentCapacity == InitialCapacity)
        {
            return FirstUpgradeCost;
        }

        // Subsequent upgrades: 50k + (number of upgrades beyond first * 100k)
        // 20→30: 50k + 100k = 150k
        // 30→40: 50k + 200k = 250k
        // 40→50: 50k + 300k = 350k
        // etc.
        int upgradeNumber = (currentCapacity - InitialCapacity) / SlotsPerUpgrade;
        return FirstUpgradeCost + (upgradeNumber * SubsequentUpgradeCostIncrease);
    }

    /// <summary>
    /// Builds the location key for personal storage at a specific bank for a specific character.
    /// Format: "personal_storage:bank:{coinhouse_tag}:char:{character_id}"
    /// </summary>
    private static string BuildLocationKey(CoinhouseTag coinhouseTag, Guid characterId)
    {
        return $"personal_storage:bank:{coinhouseTag.Value}:char:{characterId}";
    }
}
