using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Storage.Queries;

[ServiceBinding(typeof(IQueryHandler<GetStorageCapacityQuery, GetStorageCapacityResult>))]
public class GetStorageCapacityQueryHandler : IQueryHandler<GetStorageCapacityQuery, GetStorageCapacityResult>
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly PwEngineContext _context;

    private const int InitialCapacity = 10;
    private const int MaxCapacity = 100;
    private const int SlotsPerUpgrade = 10;
    private const int FirstUpgradeCost = 50_000;
    private const int SubsequentUpgradeCostIncrease = 100_000;

    public GetStorageCapacityQueryHandler(PwEngineContext context)
    {
        _context = context;
    }

    public async Task<GetStorageCapacityResult> HandleAsync(
        GetStorageCapacityQuery request,
        CancellationToken cancellationToken)
    {
        string locationKey = BuildLocationKey(request.CoinhouseTag);

        // Get or create personal storage
        Database.Entities.Storage? storage = await _context.Warehouses
            .FirstOrDefaultAsync(
                w => w.StorageType == nameof(StorageLocationType.PersonalStorage)
                     && w.LocationKey == locationKey
                     && w.OwnerId == request.CharacterId,
                cancellationToken);

        if (storage == null)
        {
            // Return default initial capacity
            return new GetStorageCapacityResult(
                InitialCapacity,
                0,
                InitialCapacity,
                true,
                FirstUpgradeCost);
        }

        int usedSlots = await _context.WarehouseItems
            .CountAsync(i => i.WarehouseId == storage.Id, cancellationToken);

        int availableSlots = storage.Capacity - usedSlots;
        bool canUpgrade = storage.Capacity < MaxCapacity;
        int nextUpgradeCost = canUpgrade ? CalculateUpgradeCost(storage.Capacity) : 0;

        Log.Debug($"Storage capacity for character {request.CharacterId} at bank '{request.CoinhouseTag}': " +
                  $"{usedSlots}/{storage.Capacity} slots used, can upgrade: {canUpgrade}");

        return new GetStorageCapacityResult(
            storage.Capacity,
            usedSlots,
            availableSlots,
            canUpgrade,
            nextUpgradeCost);
    }

    private static int CalculateUpgradeCost(int currentCapacity)
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
        int upgradeNumber = (currentCapacity - InitialCapacity) / SlotsPerUpgrade;
        return FirstUpgradeCost + (upgradeNumber * SubsequentUpgradeCostIncrease);
    }

    private static string BuildLocationKey(SharedKernel.ValueObjects.CoinhouseTag coinhouseTag)
    {
        return $"coinhouse:{coinhouseTag.Value}";
    }
}
