using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Storage.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Features.WorldEngine.Economy.Storage;

[TestFixture]
public class WithdrawItemCommandTests
{
    private PwEngineContext _context = null!;
    private WithdrawItemCommandHandler _handler = null!;
    private CoinhouseTag _testBank;
    private Guid _testCharacterId;

    [SetUp]
    public void SetUp()
    {
        var options = new DbContextOptionsBuilder<PwEngineContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new PwEngineContext(options);
        _handler = new WithdrawItemCommandHandler(_context);
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
    public async Task WithdrawItem_ReturnsItemDataAndRemovesFromStorage()
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

        byte[] expectedData = new byte[] { 1, 2, 3, 4, 5 };
        var storedItem = new Database.Entities.StoredItem
        {
            ItemData = expectedData,
            Owner = _testCharacterId,
            Name = "Test Item",
            Description = "Test Description",
            WarehouseId = storage.Id
        };
        _context.WarehouseItems.Add(storedItem);
        await _context.SaveChangesAsync();

        long itemId = storedItem.Id;
        var command = new WithdrawItemCommand(itemId, _testCharacterId);

        // Act
        CommandResult result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        Assert.That(result.Success, Is.True, "Withdrawal should succeed");
        Assert.That((byte[])result.Data["ItemData"], Is.EqualTo(expectedData), "Should return original item data");
        Assert.That((string)result.Data["ItemName"], Is.EqualTo("Test Item"), "Should return item name");

        // Verify item was removed from storage
        var remainingItem = await _context.WarehouseItems.FindAsync(itemId);
        Assert.That(remainingItem, Is.Null, "Item should be removed from storage");
    }

    [Test]
    public async Task WithdrawItem_FailsWhenItemNotFound()
    {
        // Arrange
        long nonExistentItemId = 99999;
        var command = new WithdrawItemCommand(nonExistentItemId, _testCharacterId);

        // Act
        CommandResult result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        Assert.That(result.Success, Is.False, "Should fail when item not found");
        Assert.That(result.ErrorMessage, Does.Contain("not found"), "Message should indicate item not found");
    }

    [Test]
    public async Task WithdrawItem_FailsWhenCharacterDoesNotOwnItem()
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

        var storedItem = new Database.Entities.StoredItem
        {
            ItemData = new byte[] { 1, 2, 3 },
            Owner = _testCharacterId,
            Name = "Test Item",
            Description = "Test",
            WarehouseId = storage.Id
        };
        _context.WarehouseItems.Add(storedItem);
        await _context.SaveChangesAsync();

        long itemId = storedItem.Id;
        Guid otherCharacterId = Guid.NewGuid();
        var command = new WithdrawItemCommand(itemId, otherCharacterId);

        // Act
        CommandResult result = await _handler.HandleAsync(command, CancellationToken.None);

        // Assert
        Assert.That(result.Success, Is.False, "Should fail when character doesn't own item");
        Assert.That(result.ErrorMessage, Does.Contain("permission"), "Message should mention permission");

        // Verify item was not removed
        var remainingItem = await _context.WarehouseItems.FindAsync(storedItem.Id);
        Assert.That(remainingItem, Is.Not.Null, "Item should still exist in storage");
    }
}
