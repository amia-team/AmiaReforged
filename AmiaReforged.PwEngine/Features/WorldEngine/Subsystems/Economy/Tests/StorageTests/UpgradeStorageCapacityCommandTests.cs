using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Storage.Commands;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Tests.StorageTests;

[TestFixture]
public class UpgradeStorageCapacityCommandTests
{
    private PwEngineContext _context = null!;
    private UpgradeStorageCapacityCommandHandler _handler = null!;
    private CoinhouseTag _testBank;
    private Guid _testCharacterId;

    [SetUp]
    public void SetUp()
    {
        DbContextOptions<PwEngineContext> options = new DbContextOptionsBuilder<PwEngineContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new PwEngineContext(options);
        _handler = new UpgradeStorageCapacityCommandHandler(_context);
        _testBank = new CoinhouseTag("test_bank");
        _testCharacterId = Guid.NewGuid();
    }

    [TearDown]
    public void TearDown()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Test]
    public async Task UpgradeStorage_FirstUpgrade_Costs50k_AndIncreasesby10()
    {
        // Arrange
        Storage storage = new Database.Entities.Storage
        {
            EngineId = Guid.NewGuid(),
            OwnerId = _testCharacterId,
            Capacity = 10,
            StorageType = "PersonalStorage",
            LocationKey = $"coinhouse:{_testBank.Value}"
        };
        _context.Warehouses.Add(storage);
        await _context.SaveChangesAsync();

        UpgradeStorageCapacityCommand command = new UpgradeStorageCapacityCommand(_testBank, _testCharacterId);

        // Act
        CommandResult result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        Assert.That(result.Success, Is.True, "First upgrade should succeed");
        Assert.That((int)result.Data["UpgradeCost"], Is.EqualTo(50_000), "First upgrade should cost 50k");
        Assert.That((int)result.Data["NewCapacity"], Is.EqualTo(20), "Capacity should increase to 20");

        // Verify in database
        Storage? updatedStorage = await _context.Warehouses.FindAsync(storage.Id);
        Assert.That(updatedStorage!.Capacity, Is.EqualTo(20), "Database should reflect new capacity");
    }

    [Test]
    public async Task UpgradeStorage_SecondUpgrade_Costs150k()
    {
        // Arrange
        Storage storage = new Database.Entities.Storage
        {
            EngineId = Guid.NewGuid(),
            OwnerId = _testCharacterId,
            Capacity = 20, // Already upgraded once
            StorageType = "PersonalStorage",
            LocationKey = $"coinhouse:{_testBank.Value}"
        };
        _context.Warehouses.Add(storage);
        await _context.SaveChangesAsync();

        UpgradeStorageCapacityCommand command = new UpgradeStorageCapacityCommand(_testBank, _testCharacterId);

        // Act
        CommandResult result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That((int)result.Data["UpgradeCost"], Is.EqualTo(150_000), "Second upgrade should cost 150k");
        Assert.That((int)result.Data["NewCapacity"], Is.EqualTo(30), "Capacity should increase to 30");
    }

    [Test]
    public async Task UpgradeStorage_ThirdUpgrade_Costs250k()
    {
        // Arrange
        Storage storage = new Database.Entities.Storage
        {
            EngineId = Guid.NewGuid(),
            OwnerId = _testCharacterId,
            Capacity = 30, // Already upgraded twice
            StorageType = "PersonalStorage",
            LocationKey = $"coinhouse:{_testBank.Value}"
        };
        _context.Warehouses.Add(storage);
        await _context.SaveChangesAsync();

        UpgradeStorageCapacityCommand command = new UpgradeStorageCapacityCommand(_testBank, _testCharacterId);

        // Act
        CommandResult result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        Assert.That((int)result.Data["UpgradeCost"], Is.EqualTo(250_000), "Third upgrade should cost 250k");
        Assert.That((int)result.Data["NewCapacity"], Is.EqualTo(40));
    }

    [Test]
    public async Task UpgradeStorage_FailsAtMaximumCapacity()
    {
        // Arrange
        Storage storage = new Database.Entities.Storage
        {
            EngineId = Guid.NewGuid(),
            OwnerId = _testCharacterId,
            Capacity = 100, // Already at max
            StorageType = "PersonalStorage",
            LocationKey = $"coinhouse:{_testBank.Value}"
        };
        _context.Warehouses.Add(storage);
        await _context.SaveChangesAsync();

        UpgradeStorageCapacityCommand command = new UpgradeStorageCapacityCommand(_testBank, _testCharacterId);

        // Act
        CommandResult result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        Assert.That(result.Success, Is.False, "Should fail at maximum capacity");
        Assert.That(result.ErrorMessage, Does.Contain("maximum"), "Message should mention maximum");
    }

    [Test]
    public async Task UpgradeStorage_CreatesStorageIfNotExists()
    {
        // Arrange - No existing storage
        UpgradeStorageCapacityCommand command = new UpgradeStorageCapacityCommand(_testBank, _testCharacterId);

        // Act
        CommandResult result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        Assert.That(result.Success, Is.True, "Should create storage and upgrade");
        Assert.That((int)result.Data["NewCapacity"], Is.EqualTo(20), "Should start at 10 and upgrade to 20");

        // Verify storage was created
        Storage? storage = await _context.Warehouses
            .FirstOrDefaultAsync(w => w.LocationKey == $"coinhouse:{_testBank.Value}"
                                     && w.OwnerId == _testCharacterId);
        Assert.That(storage, Is.Not.Null, "Storage should be created");
    }

    [Test]
    public async Task UpgradeStorage_CalculatesCostProgressionCorrectly()
    {
        // This test verifies the cost progression: 50k, 150k, 250k, 350k, etc.
        // Formula: 50k + (upgrade_number - 1) * 100k

        (int CurrentCapacity, int ExpectedCost)[] testCases = new[]
        {
            (CurrentCapacity: 10, ExpectedCost: 50_000),   // 1st upgrade
            (CurrentCapacity: 20, ExpectedCost: 150_000),  // 2nd upgrade
            (CurrentCapacity: 30, ExpectedCost: 250_000),  // 3rd upgrade
            (CurrentCapacity: 40, ExpectedCost: 350_000),  // 4th upgrade
            (CurrentCapacity: 50, ExpectedCost: 450_000),  // 5th upgrade
            (CurrentCapacity: 90, ExpectedCost: 850_000),  // 9th upgrade
        };

        foreach ((int currentCapacity, int expectedCost) in testCases)
        {
            // Arrange
            Storage storage = new Database.Entities.Storage
            {
                EngineId = Guid.NewGuid(),
                OwnerId = Guid.NewGuid(),
                Capacity = currentCapacity,
                StorageType = "PersonalStorage",
                LocationKey = $"coinhouse:{_testBank.Value}"
            };
            _context.Warehouses.Add(storage);
            await _context.SaveChangesAsync();

            UpgradeStorageCapacityCommand command = new UpgradeStorageCapacityCommand(_testBank, storage.OwnerId!.Value);

            // Act
            CommandResult result = await _handler.HandleAsync(command, CancellationToken.None);

            // Assert
            Assert.That((int)result.Data["UpgradeCost"], Is.EqualTo(expectedCost),
                $"Upgrade from {currentCapacity} should cost {expectedCost}");
        }
    }
}
