using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Storage.Queries;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Features.WorldEngine.Economy.Storage;

[TestFixture]
public class GetStoredItemsQueryTests
{
    private PwEngineContext _context = null!;
    private GetStoredItemsQueryHandler _handler = null!;
    private CoinhouseTag _testBank;
    private Guid _testCharacterId;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<PwEngineContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new PwEngineContext(options);
        _handler = new GetStoredItemsQueryHandler(_context);
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
    public async Task GetStoredItems_ReturnsEmptyListWhenNoItems()
    {
        // Arrange
        var query = new GetStoredItemsQuery(_testBank, _testCharacterId);

        // Act
        List<StoredItemDto> result = await _handler.HandleAsync(query, CancellationToken.None);

        // Assert
        Assert.That(result, Is.Empty, "Should return empty list when no items");
    }

    [Test]
    public async Task GetStoredItems_ReturnsAllItemsForCharacterAtBank()
    {
        // Arrange
        var storage = new Database.Entities.Storage
        {
            EngineId = Guid.NewGuid(),
            OwnerId = _testCharacterId,
            Capacity = 10,
            StorageType = "PersonalStorage",
            LocationKey = $"coinhouse:{_testBank.Value}"
        };
        _context.Warehouses.Add(storage);
        await _context.SaveChangesAsync();

        var items = new[]
        {
            new Database.Entities.StoredItem
            {
                ItemData = new byte[] { 1 },
                Owner = _testCharacterId,
                Name = "Sword",
                Description = "Sharp blade",
                WarehouseId = storage.Id
            },
            new Database.Entities.StoredItem
            {
                ItemData = new byte[] { 2 },
                Owner = _testCharacterId,
                Name = "Shield",
                Description = "Sturdy defense",
                WarehouseId = storage.Id
            }
        };
        _context.WarehouseItems.AddRange(items);
        await _context.SaveChangesAsync();

        var query = new GetStoredItemsQuery(_testBank, _testCharacterId);

        // Act
        List<StoredItemDto> result = await _handler.HandleAsync(query, CancellationToken.None);

        // Assert
        Assert.That(result.Count, Is.EqualTo(2), "Should return both items");
        Assert.That(result.Select(i => i.Name), Does.Contain("Sword"));
        Assert.That(result.Select(i => i.Name), Does.Contain("Shield"));
    }

    [Test]
    public async Task GetStoredItems_FiltersByBank()
    {
        // Arrange
        var bank1 = new CoinhouseTag("cordor_bank");
        var bank2 = new CoinhouseTag("barak_bank");

        var storage1 = new Database.Entities.Storage
        {
            EngineId = Guid.NewGuid(),
            OwnerId = _testCharacterId,
            Capacity = 10,
            StorageType = "PersonalStorage",
            LocationKey = $"coinhouse:{bank1.Value}"
        };
        var storage2 = new Database.Entities.Storage
        {
            EngineId = Guid.NewGuid(),
            OwnerId = _testCharacterId,
            Capacity = 10,
            StorageType = "PersonalStorage",
            LocationKey = $"coinhouse:{bank2.Value}"
        };
        _context.Warehouses.AddRange(storage1, storage2);
        await _context.SaveChangesAsync();

        _context.WarehouseItems.Add(new Database.Entities.StoredItem
        {
            ItemData = new byte[] { 1 },
            Owner = _testCharacterId,
            Name = "Item at Bank 1",
            Description = "Test",
            WarehouseId = storage1.Id
        });
        _context.WarehouseItems.Add(new Database.Entities.StoredItem
        {
            ItemData = new byte[] { 2 },
            Owner = _testCharacterId,
            Name = "Item at Bank 2",
            Description = "Test",
            WarehouseId = storage2.Id
        });
        await _context.SaveChangesAsync();

        var query = new GetStoredItemsQuery(bank1, _testCharacterId);

        // Act
        List<StoredItemDto> result = await _handler.HandleAsync(query, CancellationToken.None);

        // Assert
        Assert.That(result.Count, Is.EqualTo(1), "Should only return items from bank 1");
        Assert.That(result[0].Name, Is.EqualTo("Item at Bank 1"));
    }

    [Test]
    public async Task GetStoredItems_FiltersByCharacter()
    {
        // Arrange
        var character1 = Guid.NewGuid();
        var character2 = Guid.NewGuid();

        var storage1 = new Database.Entities.Storage
        {
            EngineId = Guid.NewGuid(),
            OwnerId = character1,
            Capacity = 10,
            StorageType = "PersonalStorage",
            LocationKey = $"coinhouse:{_testBank.Value}"
        };
        var storage2 = new Database.Entities.Storage
        {
            EngineId = Guid.NewGuid(),
            OwnerId = character2,
            Capacity = 10,
            StorageType = "PersonalStorage",
            LocationKey = $"coinhouse:{_testBank.Value}"
        };
        _context.Warehouses.AddRange(storage1, storage2);
        await _context.SaveChangesAsync();

        _context.WarehouseItems.Add(new Database.Entities.StoredItem
        {
            ItemData = new byte[] { 1 },
            Owner = character1,
            Name = "Character 1 Item",
            Description = "Test",
            WarehouseId = storage1.Id
        });
        _context.WarehouseItems.Add(new Database.Entities.StoredItem
        {
            ItemData = new byte[] { 2 },
            Owner = character2,
            Name = "Character 2 Item",
            Description = "Test",
            WarehouseId = storage2.Id
        });
        await _context.SaveChangesAsync();

        var query = new GetStoredItemsQuery(_testBank, character1);

        // Act
        List<StoredItemDto> result = await _handler.HandleAsync(query, CancellationToken.None);

        // Assert
        Assert.That(result.Count, Is.EqualTo(1), "Should only return items for character 1");
        Assert.That(result[0].Name, Is.EqualTo("Character 1 Item"));
    }

    [Test]
    public async Task GetStoredItems_IncludesItemDescription()
    {
        // Arrange
        var storage = new Database.Entities.Storage
        {
            EngineId = Guid.NewGuid(),
            OwnerId = _testCharacterId,
            Capacity = 10,
            StorageType = "PersonalStorage",
            LocationKey = $"coinhouse:{_testBank.Value}"
        };
        _context.Warehouses.Add(storage);
        await _context.SaveChangesAsync();

        string expectedDescription = "A legendary sword with ancient runes";
        _context.WarehouseItems.Add(new Database.Entities.StoredItem
        {
            ItemData = new byte[] { 1, 2, 3 },
            Owner = _testCharacterId,
            Name = "Runed Blade",
            Description = expectedDescription,
            WarehouseId = storage.Id
        });
        await _context.SaveChangesAsync();

        var query = new GetStoredItemsQuery(_testBank, _testCharacterId);

        // Act
        List<StoredItemDto> result = await _handler.HandleAsync(query, CancellationToken.None);

        // Assert
        Assert.That(result[0].Description, Is.EqualTo(expectedDescription));
    }
}
