using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using Microsoft.EntityFrameworkCore;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Storage.Commands;

public class StoreItemCommandHandler : ICommandHandler<StoreItemCommand>
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly PwEngineContext _context;

    public StoreItemCommandHandler(PwEngineContext context)
    {
        _context = context;
    }

    public async Task<CommandResult> HandleAsync(StoreItemCommand request, CancellationToken cancellationToken)
    {
        string locationKey = BuildLocationKey(request.CoinhouseTag);

        // Get or create personal storage
        Database.Entities.Storage? storage = await _context.Warehouses
            .Include(w => w.Items)
            .FirstOrDefaultAsync(
                w => w.StorageType == nameof(StorageLocationType.PersonalStorage)
                     && w.LocationKey == locationKey
                     && w.OwnerId == request.CharacterId,
                cancellationToken);

        if (storage == null)
        {
            storage = new Database.Entities.Storage
            {
                EngineId = Guid.NewGuid(),
                OwnerId = request.CharacterId,
                Capacity = 10, // Initial capacity
                StorageType = nameof(StorageLocationType.PersonalStorage),
                LocationKey = locationKey
            };

            _context.Warehouses.Add(storage);
            await _context.SaveChangesAsync(cancellationToken);
            
            Log.Info($"Created new personal storage for character {request.CharacterId} " +
                     $"at bank '{request.CoinhouseTag}' with 10 slots.");
        }

        // Check capacity
        int currentItemCount = await _context.WarehouseItems
            .CountAsync(i => i.WarehouseId == storage.Id, cancellationToken);

        if (currentItemCount >= storage.Capacity)
        {
            return CommandResult.Fail(
                $"Storage is full ({currentItemCount}/{storage.Capacity} slots). Purchase additional capacity to store more items.");
        }

        // Store the item
        StoredItem item = new()
        {
            ItemData = request.ItemData,
            Owner = request.CharacterId,
            Name = request.ItemName,
            Description = request.ItemDescription,
            WarehouseId = storage.Id
        };

        _context.WarehouseItems.Add(item);
        await _context.SaveChangesAsync(cancellationToken);

        int newCount = currentItemCount + 1;
        Log.Info($"Stored item '{request.ItemName}' for character {request.CharacterId} " +
                 $"at bank '{request.CoinhouseTag}' (Item ID: {item.Id}, Usage: {newCount}/{storage.Capacity}).");

        return CommandResult.Ok(new Dictionary<string, object>
        {
            ["UsedSlots"] = newCount,
            ["TotalCapacity"] = storage.Capacity,
            ["Message"] = "Item stored successfully."
        });
    }

    private static string BuildLocationKey(SharedKernel.ValueObjects.CoinhouseTag coinhouseTag)
    {
        return $"coinhouse:{coinhouseTag.Value}";
    }
}
