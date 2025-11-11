using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Storage;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Tests.Storage;

[TestFixture]
public class ForeclosureStorageServiceTests
{
    private PwEngineContext _context = null!;
    private ForeclosureStorageService _service = null!;

    [SetUp]
    public void SetUp()
    {
        // Create in-memory database for testing
        DbContextOptions<PwEngineContext> options = new DbContextOptionsBuilder<PwEngineContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new PwEngineContext(options);
        _service = new ForeclosureStorageService(_context);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Test]
    public async Task GetOrCreateForeclosureStorageAsync_CreatesNewStorage_WhenNoneExists()
    {
        // Arrange
        CoinhouseTag coinhouseTag = new("test_coinhouse");

        // Act
        Database.Entities.Storage result = await _service.GetOrCreateForeclosureStorageAsync(coinhouseTag);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.StorageType, Is.EqualTo(nameof(StorageLocationType.ForeclosedItems)));
        Assert.That(result.LocationKey, Is.EqualTo("coinhouse:test_coinhouse"));
        Assert.That(result.Capacity, Is.EqualTo(-1));
        Assert.That(result.OwnerId, Is.Null);
    }

    [Test]
    public async Task GetOrCreateForeclosureStorageAsync_ReturnsExistingStorage_WhenAlreadyExists()
    {
        // Arrange
        CoinhouseTag coinhouseTag = new("existing_coinhouse");
        Database.Entities.Storage existingStorage = await _service.GetOrCreateForeclosureStorageAsync(coinhouseTag);

        // Act
        Database.Entities.Storage result = await _service.GetOrCreateForeclosureStorageAsync(coinhouseTag);

        // Assert
        Assert.That(result.Id, Is.EqualTo(existingStorage.Id));
    }

    [Test]
    public async Task GetOrCreateForeclosureStorageAsync_CreatesUniqueStorage_ForDifferentCoinhouses()
    {
        // Arrange
        CoinhouseTag coinhouse1 = new("coinhouse_1");
        CoinhouseTag coinhouse2 = new("coinhouse_2");

        // Act
        Database.Entities.Storage storage1 = await _service.GetOrCreateForeclosureStorageAsync(coinhouse1);
        Database.Entities.Storage storage2 = await _service.GetOrCreateForeclosureStorageAsync(coinhouse2);

        // Assert
        Assert.That(storage1.Id, Is.Not.EqualTo(storage2.Id));
        Assert.That(storage1.LocationKey, Is.EqualTo("coinhouse:coinhouse_1"));
        Assert.That(storage2.LocationKey, Is.EqualTo("coinhouse:coinhouse_2"));
    }

    [Test]
    public async Task AddForeclosedItemAsync_AddsItemToStorage()
    {
        // Arrange
        CoinhouseTag coinhouseTag = new("test_coinhouse");
        Guid characterId = Guid.NewGuid();
        byte[] itemData = [1, 2, 3, 4, 5];

        // Act
        await _service.AddForeclosedItemAsync(coinhouseTag, characterId, itemData);

        // Assert
        List<StoredItem> items = await _service.GetForeclosedItemsAsync(coinhouseTag, characterId);
        Assert.That(items, Has.Count.EqualTo(1));
        Assert.That(items[0].Owner, Is.EqualTo(characterId));
        Assert.That(items[0].ItemData, Is.EqualTo(itemData));
    }

    [Test]
    public async Task AddForeclosedItemAsync_AddsMultipleItems_ForSameCharacter()
    {
        // Arrange
        CoinhouseTag coinhouseTag = new("test_coinhouse");
        Guid characterId = Guid.NewGuid();
        byte[] itemData1 = [1, 2, 3];
        byte[] itemData2 = [4, 5, 6];
        byte[] itemData3 = [7, 8, 9];

        // Act
        await _service.AddForeclosedItemAsync(coinhouseTag, characterId, itemData1);
        await _service.AddForeclosedItemAsync(coinhouseTag, characterId, itemData2);
        await _service.AddForeclosedItemAsync(coinhouseTag, characterId, itemData3);

        // Assert
        List<StoredItem> items = await _service.GetForeclosedItemsAsync(coinhouseTag, characterId);
        Assert.That(items, Has.Count.EqualTo(3));
    }

    [Test]
    public async Task GetForeclosedItemsAsync_ReturnsEmpty_WhenNoItemsExist()
    {
        // Arrange
        CoinhouseTag coinhouseTag = new("test_coinhouse");
        Guid characterId = Guid.NewGuid();

        // Act
        List<StoredItem> items = await _service.GetForeclosedItemsAsync(coinhouseTag, characterId);

        // Assert
        Assert.That(items, Is.Empty);
    }

    [Test]
    public async Task GetForeclosedItemsAsync_OnlyReturnsItemsForSpecificCharacter()
    {
        // Arrange
        CoinhouseTag coinhouseTag = new("test_coinhouse");
        Guid characterId1 = Guid.NewGuid();
        Guid characterId2 = Guid.NewGuid();
        byte[] itemData1 = [1, 2, 3];
        byte[] itemData2 = [4, 5, 6];

        await _service.AddForeclosedItemAsync(coinhouseTag, characterId1, itemData1);
        await _service.AddForeclosedItemAsync(coinhouseTag, characterId2, itemData2);

        // Act
        List<StoredItem> items = await _service.GetForeclosedItemsAsync(coinhouseTag, characterId1);

        // Assert
        Assert.That(items, Has.Count.EqualTo(1));
        Assert.That(items[0].Owner, Is.EqualTo(characterId1));
    }

    [Test]
    public async Task GetForeclosedItemsAsync_OnlyReturnsItemsForSpecificCoinhouse()
    {
        // Arrange
        CoinhouseTag coinhouse1 = new("coinhouse_1");
        CoinhouseTag coinhouse2 = new("coinhouse_2");
        Guid characterId = Guid.NewGuid();
        byte[] itemData1 = [1, 2, 3];
        byte[] itemData2 = [4, 5, 6];

        await _service.AddForeclosedItemAsync(coinhouse1, characterId, itemData1);
        await _service.AddForeclosedItemAsync(coinhouse2, characterId, itemData2);

        // Act
        List<StoredItem> items = await _service.GetForeclosedItemsAsync(coinhouse1, characterId);

        // Assert
        Assert.That(items, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task HasForeclosedItemsAsync_ReturnsTrue_WhenItemsExist()
    {
        // Arrange
        CoinhouseTag coinhouseTag = new("test_coinhouse");
        Guid characterId = Guid.NewGuid();
        byte[] itemData = [1, 2, 3];

        await _service.AddForeclosedItemAsync(coinhouseTag, characterId, itemData);

        // Act
        bool hasItems = await _service.HasForeclosedItemsAsync(coinhouseTag, characterId);

        // Assert
        Assert.That(hasItems, Is.True);
    }

    [Test]
    public async Task HasForeclosedItemsAsync_ReturnsFalse_WhenNoItemsExist()
    {
        // Arrange
        CoinhouseTag coinhouseTag = new("test_coinhouse");
        Guid characterId = Guid.NewGuid();

        // Act
        bool hasItems = await _service.HasForeclosedItemsAsync(coinhouseTag, characterId);

        // Assert
        Assert.That(hasItems, Is.False);
    }

    [Test]
    public async Task RemoveForeclosedItemAsync_RemovesItem_WhenItExists()
    {
        // Arrange
        CoinhouseTag coinhouseTag = new("test_coinhouse");
        Guid characterId = Guid.NewGuid();
        byte[] itemData = [1, 2, 3];

        await _service.AddForeclosedItemAsync(coinhouseTag, characterId, itemData);
        List<StoredItem> items = await _service.GetForeclosedItemsAsync(coinhouseTag, characterId);
        long itemId = items[0].Id;

        // Act
        await _service.RemoveForeclosedItemAsync(itemId);

        // Assert
        List<StoredItem> remainingItems = await _service.GetForeclosedItemsAsync(coinhouseTag, characterId);
        Assert.That(remainingItems, Is.Empty);
    }

    [Test]
    public async Task RemoveForeclosedItemAsync_DoesNotThrow_WhenItemDoesNotExist()
    {
        // Arrange
        long nonExistentItemId = 99999;

        // Act & Assert
        Assert.DoesNotThrowAsync(async () =>
            await _service.RemoveForeclosedItemAsync(nonExistentItemId));
    }

    [Test]
    public async Task RemoveForeclosedItemAsync_OnlyRemovesSpecificItem()
    {
        // Arrange
        CoinhouseTag coinhouseTag = new("test_coinhouse");
        Guid characterId = Guid.NewGuid();
        byte[] itemData1 = [1, 2, 3];
        byte[] itemData2 = [4, 5, 6];

        await _service.AddForeclosedItemAsync(coinhouseTag, characterId, itemData1);
        await _service.AddForeclosedItemAsync(coinhouseTag, characterId, itemData2);

        List<StoredItem> items = await _service.GetForeclosedItemsAsync(coinhouseTag, characterId);
        long itemToRemoveId = items[0].Id;

        // Act
        await _service.RemoveForeclosedItemAsync(itemToRemoveId);

        // Assert
        List<StoredItem> remainingItems = await _service.GetForeclosedItemsAsync(coinhouseTag, characterId);
        Assert.That(remainingItems, Has.Count.EqualTo(1));
        Assert.That(remainingItems[0].Id, Is.Not.EqualTo(itemToRemoveId));
    }
}
