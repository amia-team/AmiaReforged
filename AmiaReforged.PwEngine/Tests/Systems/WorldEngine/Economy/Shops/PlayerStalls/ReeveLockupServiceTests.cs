using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Database.Entities.Economy.Shops;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops.PlayerStalls;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.Economy.Shops.PlayerStalls;

[TestFixture]
public sealed class ReeveLockupServiceTests
{
    private sealed class InMemoryPwContextFactory : IDbContextFactory<PwEngineContext>
    {
        private readonly DbContextOptions<PwEngineContext> _options;

        public InMemoryPwContextFactory(string databaseName)
        {
            _options = new DbContextOptionsBuilder<PwEngineContext>()
                .UseInMemoryDatabase(databaseName)
                .Options;
        }

        public PwEngineContext CreateDbContext()
        {
            return new PwEngineContext(_options);
        }
    }

    [Test]
    public async Task StoreSuspendedInventoryAsync_PersistsOneEntryPerItem()
    {
        string databaseName = nameof(StoreSuspendedInventoryAsync_PersistsOneEntryPerItem);
        InMemoryPwContextFactory factory = new(databaseName);
        ReeveLockupService service = new(factory);

        Guid consignorGuid = Guid.NewGuid();
        string personaId = $"Character:{consignorGuid}";
        PlayerStall stall = CreateStall(42, "ar_market", consignorGuid, personaId);
        StallProduct product = CreateProduct(1, stall.Id, personaId, quantity: 3);

        int storedCount = await service.StoreSuspendedInventoryAsync(stall, new[] { product });

        Assert.That(storedCount, Is.EqualTo(3));

        await using PwEngineContext verificationContext = factory.CreateDbContext();
        List<Storage> storages = await verificationContext.Warehouses.ToListAsync();
        List<StoredItem> items = await verificationContext.WarehouseItems.ToListAsync();

        Assert.That(storages, Has.Count.EqualTo(1));
        Assert.That(items, Has.Count.EqualTo(3));
        Assert.That(items.Select(i => i.WarehouseId), Has.All.EqualTo(storages[0].Id));

        Guid expectedOwner = PersonaId.ToGuid(PersonaId.Parse(personaId));
        Assert.That(items.Select(i => i.Owner), Has.All.EqualTo(expectedOwner));
        Assert.That(items.All(i => i.ItemData.SequenceEqual(product.ItemData)), Is.True);

        Guid expectedEngineId = ComputeStorageEngineId(stall.AreaResRef);
        Assert.That(storages[0].EngineId, Is.EqualTo(expectedEngineId));
    }

    [Test]
    public async Task StoreSuspendedInventoryAsync_WhenPersonaMissing_DoesNotPersist()
    {
        string databaseName = nameof(StoreSuspendedInventoryAsync_WhenPersonaMissing_DoesNotPersist);
        InMemoryPwContextFactory factory = new(databaseName);
        ReeveLockupService service = new(factory);

        PlayerStall stall = new()
        {
            Id = 77,
            Tag = "stall_missing_persona",
            AreaResRef = "ar_market",
            OwnerPersonaId = null
        };

        StallProduct product = new()
        {
            Id = 5,
            StallId = stall.Id,
            ResRef = "resref_missing",
            Name = "Missing Persona Item",
            Price = 10,
            Quantity = 2,
            ItemData = Encoding.UTF8.GetBytes("{}"),
            ConsignedByPersonaId = null
        };

        int storedCount = await service.StoreSuspendedInventoryAsync(stall, new[] { product });

        Assert.That(storedCount, Is.EqualTo(0));

        await using PwEngineContext verificationContext = factory.CreateDbContext();
        Assert.That(await verificationContext.Warehouses.CountAsync(), Is.EqualTo(0));
        Assert.That(await verificationContext.WarehouseItems.CountAsync(), Is.EqualTo(0));
    }

    [Test]
    public async Task StoreSuspendedInventoryAsync_ReusesStoragePerArea()
    {
        string databaseName = nameof(StoreSuspendedInventoryAsync_ReusesStoragePerArea);
        InMemoryPwContextFactory factory = new(databaseName);
        ReeveLockupService service = new(factory);

        Guid consignorGuid = Guid.NewGuid();
        string personaId = $"Character:{consignorGuid}";
        PlayerStall stall = CreateStall(99, "ar_market", consignorGuid, personaId);

        StallProduct firstBatch = CreateProduct(10, stall.Id, personaId, quantity: 1);
        StallProduct secondBatch = CreateProduct(11, stall.Id, personaId, quantity: 2);

        await service.StoreSuspendedInventoryAsync(stall, new[] { firstBatch });
        await service.StoreSuspendedInventoryAsync(stall, new[] { secondBatch });

        await using PwEngineContext verificationContext = factory.CreateDbContext();
        Assert.That(await verificationContext.Warehouses.CountAsync(), Is.EqualTo(1));
        Assert.That(await verificationContext.WarehouseItems.CountAsync(), Is.EqualTo(3));
    }

    private static PlayerStall CreateStall(long id, string areaResRef, Guid ownerGuid, string personaId)
    {
        return new PlayerStall
        {
            Id = id,
            Tag = $"stall_{id}",
            AreaResRef = areaResRef,
            OwnerCharacterId = ownerGuid,
            OwnerPersonaId = personaId,
            OwnerDisplayName = "Test Owner",
            DailyRent = 100,
            LeaseStartUtc = DateTime.UtcNow,
            NextRentDueUtc = DateTime.UtcNow
        };
    }

    private static StallProduct CreateProduct(long id, long stallId, string personaId, int quantity)
    {
        return new StallProduct
        {
            Id = id,
            StallId = stallId,
            ResRef = $"resref_{id}",
            Name = $"Item {id}",
            Price = 25,
            Quantity = quantity,
            ItemData = Encoding.UTF8.GetBytes($"{{\"id\":{id}}}"),
            ConsignedByPersonaId = personaId
        };
    }

    private static Guid ComputeStorageEngineId(string? areaResRef)
    {
        string normalized = string.IsNullOrWhiteSpace(areaResRef)
            ? "market-reeve:unknown"
            : $"market-reeve:{areaResRef.Trim().ToLowerInvariant()}";

        using SHA256 sha = SHA256.Create();
        byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(normalized));
        byte[] guidBytes = hash.Take(16).ToArray();
        return new Guid(guidBytes);
    }
}
