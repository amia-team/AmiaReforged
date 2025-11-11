using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Storage.Commands;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Tests.StorageTests;

[TestFixture]
public class StoreItemCommandTests
{
    private PwEngineContext _context = null!;
    private StoreItemCommandHandler _handler = null!;
    private CoinhouseTag _testBank;
    private Guid _testCharacterId;

    [SetUp]
    public void SetUp()
    {
        DbContextOptions<PwEngineContext> options = new DbContextOptionsBuilder<PwEngineContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new PwEngineContext(options);
        _handler = new StoreItemCommandHandler(_context);
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
    public async Task StoreItem_CreatesStorageAndStoresItem_WhenFirstItem()
    {
        // Arrange
        StoreItemCommand command = new StoreItemCommand(
            _testBank,
            _testCharacterId,
            "Iron Sword",
            "A sharp blade",
            new byte[] { 1, 2, 3, 4, 5 });

        // Act
        CommandResult result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        Assert.That(result.Success, Is.True, "Should successfully store first item");
        Assert.That((int)result.Data["UsedSlots"], Is.EqualTo(1), "Should have 1 used slot");
        Assert.That((int)result.Data["TotalCapacity"], Is.EqualTo(10), "Should have initial capacity of 10");

        // Verify storage was created
        Storage? storage = await _context.Warehouses
            .FirstOrDefaultAsync(w => w.LocationKey == $"coinhouse:{_testBank.Value}"
                                     && w.OwnerId == _testCharacterId);
        Assert.That(storage, Is.Not.Null, "Storage should be created");
        Assert.That(storage!.Capacity, Is.EqualTo(10), "Initial capacity should be 10");
    }

    [Test]
    public async Task StoreItem_FailsWhenStorageIsFull()
    {
        // Arrange - Create storage with 10 capacity and fill it
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

        // Add 10 items to fill storage
        for (int i = 0; i < 10; i++)
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

        StoreItemCommand command = new StoreItemCommand(
            _testBank,
            _testCharacterId,
            "Overflow Item",
            "Should not fit",
            new byte[] { 1, 2, 3 });

        // Act
        CommandResult result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        Assert.That(result.Success, Is.False, "Should fail when storage is full");
        Assert.That(result.ErrorMessage, Does.Contain("full"), "Message should mention storage is full");
    }

    [Test]
    public async Task StoreItem_StoresItemDescription()
    {
        // Arrange
        string expectedDescription = "A legendary weapon forged in dragon fire";
        StoreItemCommand command = new StoreItemCommand(
            _testBank,
            _testCharacterId,
            "Dragon Blade",
            expectedDescription,
            new byte[] { 1, 2, 3 });

        // Act
        CommandResult result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        Assert.That(result.Success, Is.True);

        StoredItem? storedItem = await _context.WarehouseItems
            .FirstOrDefaultAsync(i => i.Owner == _testCharacterId);
        Assert.That(storedItem, Is.Not.Null);
        Assert.That(storedItem!.Description, Is.EqualTo(expectedDescription));
    }

    [Test]
    public async Task StoreItem_IsBankSpecific()
    {
        // Arrange
        CoinhouseTag bank1 = new CoinhouseTag("cordor_bank");
        CoinhouseTag bank2 = new CoinhouseTag("barak_bank");

        StoreItemCommand command1 = new StoreItemCommand(bank1, _testCharacterId, "Item 1", "Desc", new byte[] { 1 });
        StoreItemCommand command2 = new StoreItemCommand(bank2, _testCharacterId, "Item 2", "Desc", new byte[] { 2 });

        // Act
        await _handler.HandleAsync(command1, CancellationToken.None);
        await _handler.HandleAsync(command2, CancellationToken.None);

        // Assert
        Storage? storage1 = await _context.Warehouses
            .FirstOrDefaultAsync(w => w.LocationKey == $"coinhouse:{bank1.Value}");
        Storage? storage2 = await _context.Warehouses
            .FirstOrDefaultAsync(w => w.LocationKey == $"coinhouse:{bank2.Value}");

        Assert.That(storage1, Is.Not.Null, "Should create separate storage for bank 1");
        Assert.That(storage2, Is.Not.Null, "Should create separate storage for bank 2");
        Assert.That(storage1!.Id, Is.Not.EqualTo(storage2!.Id), "Should be different storage instances");
    }

    [Test]
    public async Task StoreItem_IsCharacterSpecific()
    {
        // Arrange
        Guid character1 = Guid.NewGuid();
        Guid character2 = Guid.NewGuid();

        StoreItemCommand command1 = new StoreItemCommand(_testBank, character1, "Item 1", "Desc", new byte[] { 1 });
        StoreItemCommand command2 = new StoreItemCommand(_testBank, character2, "Item 2", "Desc", new byte[] { 2 });

        // Act
        await _handler.HandleAsync(command1, CancellationToken.None);
        await _handler.HandleAsync(command2, CancellationToken.None);

        // Assert
        Storage? storage1 = await _context.Warehouses
            .FirstOrDefaultAsync(w => w.OwnerId == character1);
        Storage? storage2 = await _context.Warehouses
            .FirstOrDefaultAsync(w => w.OwnerId == character2);

        Assert.That(storage1, Is.Not.Null, "Should create storage for character 1");
        Assert.That(storage2, Is.Not.Null, "Should create storage for character 2");
        Assert.That(storage1!.Id, Is.Not.EqualTo(storage2!.Id), "Each character has separate storage");
    }
}
