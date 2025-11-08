using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Storage.Queries;

[ServiceBinding(typeof(IQueryHandler<GetStoredItemsQuery, List<StoredItemDto>>))]
public class GetStoredItemsQueryHandler : IQueryHandler<GetStoredItemsQuery, List<StoredItemDto>>
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly PwEngineContext _context;

    public GetStoredItemsQueryHandler(PwEngineContext context)
    {
        _context = context;
    }

    public async Task<List<StoredItemDto>> HandleAsync(GetStoredItemsQuery request, CancellationToken cancellationToken)
    {
        string locationKey = BuildLocationKey(request.CoinhouseTag);

        // First, get the warehouse for this location and character
        Database.Entities.Storage? warehouse = await _context.Warehouses
            .FirstOrDefaultAsync(w => w.LocationKey == locationKey
                                     && w.StorageType == nameof(StorageLocationType.PersonalStorage)
                                     && w.OwnerId == request.CharacterId,
                cancellationToken);

        // If no warehouse exists, return empty list
        if (warehouse == null)
        {
            Log.Debug($"No storage found for character {request.CharacterId} at bank '{request.CoinhouseTag}'.");
            return new List<StoredItemDto>();
        }

        // Now get items directly by warehouse ID - much cleaner!
        List<StoredItem> items = await _context.WarehouseItems
            .Where(item => item.WarehouseId == warehouse.Id)
            .ToListAsync(cancellationToken);

        List<StoredItemDto> dtos = items.Select(item => new StoredItemDto(
            item.Id,
            item.Name,
            item.Description ?? "",
            item.ItemData
        )).ToList();

        Log.Debug($"Retrieved {items.Count} stored items for character {request.CharacterId} " +
                  $"at bank '{request.CoinhouseTag}' (Warehouse ID: {warehouse.Id}).");

        return dtos;
    }

    private static string BuildLocationKey(SharedKernel.ValueObjects.CoinhouseTag coinhouseTag)
    {
        return $"coinhouse:{coinhouseTag.Value}";
    }
}
