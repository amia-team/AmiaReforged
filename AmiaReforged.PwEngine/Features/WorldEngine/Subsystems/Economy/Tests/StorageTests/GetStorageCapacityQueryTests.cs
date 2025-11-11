using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Storage.Queries;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Tests.Storage;

[TestFixture]
public class GetStorageCapacityQueryTests
{
    private PwEngineContext _context = null!;
    private GetStorageCapacityQueryHandler _handler = null!;
    private CoinhouseTag _testBank;
    private Guid _testCharacterId;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<PwEngineContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new PwEngineContext(options);
        _handler = new GetStorageCapacityQueryHandler(_context);
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
    public async Task GetStorageCapacity_ReturnsDefaultWhenNoStorage()
    {
        // Arrange
        var query = new GetStorageCapacityQuery(_testBank, _testCharacterId);

        // Act
        GetStorageCapacityResult result = await _handler.HandleAsync(query, CancellationToken.None);

        // Assert
        Assert.That(result.TotalCapacity, Is.EqualTo(10), "Should return default capacity of 10");
        Assert.That(result.UsedSlots, Is.EqualTo(0), "Should have 0 used slots");
        Assert.That(result.AvailableSlots, Is.EqualTo(10), "Should have 10 available slots");
        Assert.That(result.CanUpgrade, Is.True, "Should be able to upgrade");
        Assert.That(result.NextUpgradeCost, Is.EqualTo(50_000), "First upgrade should cost 50k");
    }

    [Test]
    public async Task GetStorageCapacity_ReturnsCorrectUsageWithItems()
    {
        // Arrange
        var storage = new Database.Entities.Storage
        {
            EngineId = Guid.NewGuid(),
            OwnerId = _testCharacterId,
            Capacity = 20,
            StorageType = "PersonalStorage",
            LocationKey = $"coinhouse:{_testBank.Value}"
        };
        _context.Warehouses.Add(storage);
        await _context.SaveChangesAsync();

        // Add 5 items
        for (int i = 0; i < 5; i++)
        {
            _context.WarehouseItems.Add(new Database.Entities.StoredItem
            {
                ItemData = new byte[] { 1, 2, 3 },
                Owner = _testCharacterId,
                Name = $"Item {i}",
                Description = "Test",
                WarehouseId = storage.Id
            });
        }
        await _context.SaveChangesAsync();

        var query = new GetStorageCapacityQuery(_testBank, _testCharacterId);

        // Act
        GetStorageCapacityResult result = await _handler.HandleAsync(query, CancellationToken.None);

        // Assert
        Assert.That(result.TotalCapacity, Is.EqualTo(20));
        Assert.That(result.UsedSlots, Is.EqualTo(5), "Should have 5 items stored");
        Assert.That(result.AvailableSlots, Is.EqualTo(15), "Should have 15 available slots");
    }

    [Test]
    public async Task GetStorageCapacity_IndicatesCannotUpgradeAtMax()
    {
        // Arrange
        var storage = new Database.Entities.Storage
        {
            EngineId = Guid.NewGuid(),
            OwnerId = _testCharacterId,
            Capacity = 100, // Max capacity
            StorageType = "PersonalStorage",
            LocationKey = $"coinhouse:{_testBank.Value}"
        };
        _context.Warehouses.Add(storage);
        await _context.SaveChangesAsync();

        var query = new GetStorageCapacityQuery(_testBank, _testCharacterId);

        // Act
        GetStorageCapacityResult result = await _handler.HandleAsync(query, CancellationToken.None);

        // Assert
        Assert.That(result.TotalCapacity, Is.EqualTo(100));
        Assert.That(result.CanUpgrade, Is.False, "Should not be able to upgrade at max");
        Assert.That(result.NextUpgradeCost, Is.EqualTo(0), "Cost should be 0 at max");
    }

    [Test]
    public async Task GetStorageCapacity_ShowsCorrectUpgradeCosts()
    {
        // Test various capacity levels and their upgrade costs
        var testCases = new[]
        {
            (Capacity: 10, ExpectedCost: 50_000),   // First upgrade
            (Capacity: 20, ExpectedCost: 150_000),  // Second upgrade
            (Capacity: 30, ExpectedCost: 250_000),  // Third upgrade
            (Capacity: 90, ExpectedCost: 850_000),  // Ninth upgrade
        };

        foreach (var (capacity, expectedCost) in testCases)
        {
            // Arrange
            var storage = new Database.Entities.Storage
            {
                EngineId = Guid.NewGuid(),
                OwnerId = Guid.NewGuid(),
                Capacity = capacity,
                StorageType = "PersonalStorage",
                LocationKey = $"coinhouse:{_testBank.Value}"
            };
            _context.Warehouses.Add(storage);
            await _context.SaveChangesAsync();

            var query = new GetStorageCapacityQuery(_testBank, storage.OwnerId!.Value);

            // Act
            GetStorageCapacityResult result = await _handler.HandleAsync(query, CancellationToken.None);

            // Assert
            Assert.That(result.NextUpgradeCost, Is.EqualTo(expectedCost),
                $"Capacity {capacity} should have upgrade cost {expectedCost}");
            Assert.That(result.CanUpgrade, Is.True, $"Capacity {capacity} should be upgradeable");
        }
    }

    [Test]
    public async Task GetStorageCapacity_IsIsolatedByBankAndCharacter()
    {
        // Arrange
        var bank1 = new CoinhouseTag("cordor_bank");
        var bank2 = new CoinhouseTag("barak_bank");
        var character1 = Guid.NewGuid();
        var character2 = Guid.NewGuid();

        // Create different storage for different combinations
        var storages = new[]
        {
            new Database.Entities.Storage
            {
                EngineId = Guid.NewGuid(),
                OwnerId = character1,
                Capacity = 30,
                StorageType = "PersonalStorage",
                LocationKey = $"coinhouse:{bank1.Value}"
            },
            new Database.Entities.Storage
            {
                EngineId = Guid.NewGuid(),
                OwnerId = character1,
                Capacity = 40,
                StorageType = "PersonalStorage",
                LocationKey = $"coinhouse:{bank2.Value}"
            },
            new Database.Entities.Storage
            {
                EngineId = Guid.NewGuid(),
                OwnerId = character2,
                Capacity = 50,
                StorageType = "PersonalStorage",
                LocationKey = $"coinhouse:{bank1.Value}"
            }
        };
        _context.Warehouses.AddRange(storages);
        await _context.SaveChangesAsync();

        // Act & Assert - Character 1 at Bank 1
        var query1 = new GetStorageCapacityQuery(bank1, character1);
        GetStorageCapacityResult result1 = await _handler.HandleAsync(query1, CancellationToken.None);
        Assert.That(result1.TotalCapacity, Is.EqualTo(30), "Character 1 at Bank 1 should have 30");

        // Act & Assert - Character 1 at Bank 2
        var query2 = new GetStorageCapacityQuery(bank2, character1);
        GetStorageCapacityResult result2 = await _handler.HandleAsync(query2, CancellationToken.None);
        Assert.That(result2.TotalCapacity, Is.EqualTo(40), "Character 1 at Bank 2 should have 40");

        // Act & Assert - Character 2 at Bank 1
        var query3 = new GetStorageCapacityQuery(bank1, character2);
        GetStorageCapacityResult result3 = await _handler.HandleAsync(query3, CancellationToken.None);
        Assert.That(result3.TotalCapacity, Is.EqualTo(50), "Character 2 at Bank 1 should have 50");
    }
}
