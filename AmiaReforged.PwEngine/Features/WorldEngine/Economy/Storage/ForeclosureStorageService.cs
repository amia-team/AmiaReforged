using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using Anvil.API;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Storage;

/// <summary>
/// Service for managing foreclosed storage at coinhouses.
/// When properties are evicted, tenant belongings are moved to foreclosed storage
/// at the settlement's coinhouse where they can be reclaimed.
/// </summary>
[ServiceBinding(typeof(IForeclosureStorageService))]
public class ForeclosureStorageService : IForeclosureStorageService
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly PwEngineContext _context;

    public ForeclosureStorageService(PwEngineContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<Database.Entities.Storage> GetOrCreateForeclosureStorageAsync(
        CoinhouseTag coinhouseTag,
        CancellationToken cancellationToken = default)
    {
        string locationKey = BuildLocationKey(coinhouseTag);

        // Try to find existing storage
        Database.Entities.Storage? existing = await _context.Warehouses
            .FirstOrDefaultAsync(
                w => w.StorageType == nameof(StorageLocationType.ForeclosedItems)
                     && w.LocationKey == locationKey,
                cancellationToken);

        if (existing is not null)
        {
            Log.Info($"Found existing foreclosure storage for coinhouse '{coinhouseTag}' " +
                     $"with {existing.Items?.Count ?? 0} items (Storage ID: {existing.Id})");
            return existing;
        }

        // Create new foreclosure storage with unlimited capacity
        // Generate deterministic EngineId based on coinhouse tag
        Guid engineId = Guid.NewGuid(); // In reality, you'd want a deterministic approach

        Database.Entities.Storage storage = new()
        {
            EngineId = engineId,
            Capacity = -1, // Unlimited capacity
            StorageType = StorageLocationType.ForeclosedItems.ToString(),
            LocationKey = locationKey
        };

        _context.Warehouses.Add(storage);
        await _context.SaveChangesAsync(cancellationToken);

        Log.Info($"Created new foreclosure storage for coinhouse '{coinhouseTag}' " +
                 $"(Storage ID: {storage.Id})");

        return storage;
    }

    /// <inheritdoc />
    public async Task AddForeclosedItemAsync(
        CoinhouseTag coinhouseTag,
        Guid characterId,
        byte[] itemData,
        CancellationToken cancellationToken = default)
    {
        Database.Entities.Storage storage = await GetOrCreateForeclosureStorageAsync(
            coinhouseTag,
            cancellationToken);

        StoredItem item = new()
        {
            ItemData = itemData,
            Owner = characterId,
            WarehouseId = storage.Id
        };

        _context.WarehouseItems.Add(item);
        await _context.SaveChangesAsync(cancellationToken);

        Log.Info($"Added foreclosed item to storage at coinhouse '{coinhouseTag}' " +
                         $"for character {characterId} (Item ID: {item.Id}, Storage ID: {storage.Id}).");
    }

    /// <inheritdoc />
    public async Task<List<StoredItem>> GetForeclosedItemsAsync(
        CoinhouseTag coinhouseTag,
        Guid characterId,
        CancellationToken cancellationToken = default)
    {
        string locationKey = BuildLocationKey(coinhouseTag);

        // First, get the warehouse for this location
        Database.Entities.Storage? warehouse = await _context.Warehouses
            .FirstOrDefaultAsync(w => w.LocationKey == locationKey
                                     && w.StorageType == nameof(StorageLocationType.ForeclosedItems),
                cancellationToken);

        // If no warehouse exists, return empty list
        if (warehouse == null)
        {
            Log.Info($"No foreclosure storage found at coinhouse '{coinhouseTag}' - returning empty list.");
            return new List<StoredItem>();
        }

        // Now get items directly by warehouse ID and owner
        List<StoredItem> items = await _context.WarehouseItems
            .Where(item => item.WarehouseId == warehouse.Id && item.Owner == characterId)
            .ToListAsync(cancellationToken);

        Log.Info($"Retrieved {items.Count} foreclosed items for character {characterId} " +
                         $"at coinhouse '{coinhouseTag}' (Warehouse ID: {warehouse.Id}).");

        return items;
    }

    /// <inheritdoc />
    public async Task<bool> HasForeclosedItemsAsync(
        CoinhouseTag coinhouseTag,
        Guid characterId,
        CancellationToken cancellationToken = default)
    {
        string locationKey = BuildLocationKey(coinhouseTag);

        // First, get the warehouse for this location
        Database.Entities.Storage? warehouse = await _context.Warehouses
            .FirstOrDefaultAsync(w => w.LocationKey == locationKey
                                     && w.StorageType == nameof(StorageLocationType.ForeclosedItems),
                cancellationToken);

        // If no warehouse exists, no items can exist
        if (warehouse == null)
        {
            return false;
        }

        // Check if items exist for this warehouse and owner
        bool hasItems = await _context.WarehouseItems
            .AnyAsync(item => item.WarehouseId == warehouse.Id && item.Owner == characterId,
                cancellationToken);

        Log.Info($"Character {characterId} " +
                         $"{(hasItems ? "has" : "does not have")} foreclosed items " +
                         $"at coinhouse '{coinhouseTag}'.");

        return hasItems;
    }

    /// <inheritdoc />
    public async Task RemoveForeclosedItemAsync(
        long storedItemId,
        CancellationToken cancellationToken = default)
    {
        StoredItem? item = await _context.WarehouseItems
            .FirstOrDefaultAsync(i => i.Id == storedItemId, cancellationToken);

        if (item == null)
        {
            Log.Warn($"Attempted to remove foreclosed item {storedItemId} but it was not found.");
            return;
        }

        _context.WarehouseItems.Remove(item);
        await _context.SaveChangesAsync(cancellationToken);

        Log.Info($"Removed foreclosed item {storedItemId} from storage " +
                         $"(Owner: {item.Owner}, Warehouse ID: {item.WarehouseId}).");
    }

    /// <summary>
    /// Builds the location key for foreclosed storage at a specific coinhouse.
    /// Format: "coinhouse:{coinhouse_tag}"
    /// </summary>
    private static string BuildLocationKey(CoinhouseTag coinhouseTag)
    {
        return $"coinhouse:{coinhouseTag.Value}";
    }
}
