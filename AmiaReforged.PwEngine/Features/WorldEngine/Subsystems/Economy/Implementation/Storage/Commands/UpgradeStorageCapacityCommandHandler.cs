using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Storage.Commands;

[ServiceBinding(typeof(ICommandHandler<UpgradeStorageCapacityCommand>))]
[ServiceBinding(typeof(ICommandHandlerMarker))]
public class UpgradeStorageCapacityCommandHandler : ICommandHandler<UpgradeStorageCapacityCommand>
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly PwEngineContext _context;

    private const int InitialCapacity = 10;
    private const int MaxCapacity = 100;
    private const int SlotsPerUpgrade = 10;
    private const int FirstUpgradeCost = 50_000;
    private const int SubsequentUpgradeCostIncrease = 100_000;

    public UpgradeStorageCapacityCommandHandler(PwEngineContext context)
    {
        _context = context;
    }

    public async Task<CommandResult> HandleAsync(
        UpgradeStorageCapacityCommand request,
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
            storage = new Database.Entities.Storage
            {
                EngineId = Guid.NewGuid(),
                OwnerId = request.CharacterId,
                Capacity = InitialCapacity,
                StorageType = nameof(StorageLocationType.PersonalStorage),
                LocationKey = locationKey
            };

            _context.Warehouses.Add(storage);
            await _context.SaveChangesAsync(cancellationToken);

            Log.Info($"Created new personal storage for character {request.CharacterId} " +
                     $"at bank '{request.CoinhouseTag}' with {InitialCapacity} slots.");
        }

        int currentCapacity = storage.Capacity;

        // Check if already at max capacity
        if (currentCapacity >= MaxCapacity)
        {
            return CommandResult.Fail($"Storage is already at maximum capacity ({MaxCapacity} slots).");
        }

        // Calculate upgrade cost
        int upgradeCost = CalculateUpgradeCost(currentCapacity);
        int newCapacity = currentCapacity + SlotsPerUpgrade;

        // Update capacity (caller handles gold deduction)
        storage.Capacity = newCapacity;
        await _context.SaveChangesAsync(cancellationToken);

        Log.Info($"Upgraded storage capacity for character {request.CharacterId} " +
                 $"at bank '{request.CoinhouseTag}' from {currentCapacity} to {newCapacity} slots " +
                 $"(Cost: {upgradeCost} gold).");

        return CommandResult.Ok(new Dictionary<string, object>
        {
            ["NewCapacity"] = newCapacity,
            ["UpgradeCost"] = upgradeCost,
            ["Message"] = $"Storage upgraded successfully! New capacity: {newCapacity} slots."
        });
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
        // 20→30: 50k + 100k = 150k
        // 30→40: 50k + 200k = 250k
        // 40→50: 50k + 300k = 350k
        int upgradeNumber = (currentCapacity - InitialCapacity) / SlotsPerUpgrade;
        return FirstUpgradeCost + (upgradeNumber * SubsequentUpgradeCostIncrease);
    }

    private static string BuildLocationKey(SharedKernel.ValueObjects.CoinhouseTag coinhouseTag)
    {
        return $"coinhouse:{coinhouseTag.Value}";
    }
}
