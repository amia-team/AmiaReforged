using AmiaReforged.PwEngine.Database.Entities.Economy.Shops;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops.PlayerStalls;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Tests.Shops;

[TestFixture]
public class PlayerStallAggregateTests
{
    [Test]
    public void TryClaim_WhenUnowned_ReturnsMutationApplyingState()
    {
        PlayerStall stall = CreateStall();
        PlayerStallAggregate aggregate = PlayerStallAggregate.FromEntity(stall);
        Guid ownerGuid = Guid.NewGuid();
        PlayerStallClaimOptions options = new(
            OwnerPersonaId: $"Character:{ownerGuid}",
            OwnerPlayerPersonaId: "Player:TEST",
            OwnerDisplayName: "Aria Moonwhisper",
            CoinHouseAccountId: Guid.NewGuid(),
            HoldEarningsInStall: true,
            LeaseStartUtc: new DateTime(2025, 11, 03, 10, 15, 00, DateTimeKind.Utc),
            NextRentDueUtc: new DateTime(2025, 11, 04, 10, 15, 00, DateTimeKind.Utc));

        PlayerStallDomainResult<Action<PlayerStall>> result = aggregate.TryClaim(ownerGuid, options);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Payload, Is.Not.Null);

        PlayerStall persisted = CreateStall();
        result.Payload!(persisted);

        Assert.Multiple(() =>
        {
            Assert.That(persisted.OwnerCharacterId, Is.EqualTo(ownerGuid));
            Assert.That(persisted.OwnerPersonaId, Is.EqualTo(options.OwnerPersonaId));
            Assert.That(persisted.OwnerPlayerPersonaId, Is.EqualTo(options.OwnerPlayerPersonaId));
            Assert.That(persisted.OwnerDisplayName, Is.EqualTo(options.OwnerDisplayName));
            Assert.That(persisted.CoinHouseAccountId, Is.EqualTo(options.CoinHouseAccountId));
            Assert.That(persisted.HoldEarningsInStall, Is.True);
            Assert.That(persisted.LeaseStartUtc, Is.EqualTo(options.LeaseStartUtc));
            Assert.That(persisted.NextRentDueUtc, Is.EqualTo(options.NextRentDueUtc));
            Assert.That(persisted.LastRentPaidUtc, Is.EqualTo(options.LeaseStartUtc));
            Assert.That(persisted.IsActive, Is.True);
            Assert.That(persisted.SuspendedUtc, Is.Null);
            Assert.That(persisted.DeactivatedUtc, Is.Null);
        });
    }

    [Test]
    public void TryClaim_WhenOwnedByDifferentOwner_ReturnsFailure()
    {
        PlayerStall stall = CreateStall();
        stall.OwnerCharacterId = Guid.NewGuid();
        PlayerStallAggregate aggregate = PlayerStallAggregate.FromEntity(stall);

        PlayerStallDomainResult<Action<PlayerStall>> result = aggregate.TryClaim(Guid.NewGuid(), new PlayerStallClaimOptions(
            OwnerPersonaId: "Character:another",
            OwnerPlayerPersonaId: "Player:ANOTHER",
            OwnerDisplayName: "Different",
            CoinHouseAccountId: null,
            HoldEarningsInStall: false,
            LeaseStartUtc: DateTime.UtcNow,
            NextRentDueUtc: DateTime.UtcNow.AddDays(1)));

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo(PlayerStallError.AlreadyOwned));
    }

    [Test]
    public void TryRelease_WhenOwnedAndRequestorMatches_ReturnsMutation()
    {
        Guid ownerGuid = Guid.NewGuid();
        string personaId = $"Character:{ownerGuid}";
        PlayerStall stall = CreateStall();
        stall.OwnerCharacterId = ownerGuid;
        stall.OwnerPersonaId = personaId;
        PlayerStallAggregate aggregate = PlayerStallAggregate.FromEntity(stall);
        DateTime releaseUtc = new DateTime(2025, 11, 03, 12, 00, 00, DateTimeKind.Utc);

        PlayerStallDomainResult<Action<PlayerStall>> result = aggregate.TryRelease(personaId, force: false, releasedUtc: releaseUtc);

        Assert.That(result.Success, Is.True);
        PlayerStall persisted = CreateStall();
        result.Payload!(persisted);

        Assert.Multiple(() =>
        {
            Assert.That(persisted.OwnerCharacterId, Is.Null);
            Assert.That(persisted.OwnerPersonaId, Is.Null);
            Assert.That(persisted.OwnerDisplayName, Is.Null);
            Assert.That(persisted.CoinHouseAccountId, Is.Null);
            Assert.That(persisted.HoldEarningsInStall, Is.False);
            Assert.That(persisted.IsActive, Is.False);
            Assert.That(persisted.SuspendedUtc, Is.EqualTo(releaseUtc));
            Assert.That(persisted.DeactivatedUtc, Is.EqualTo(releaseUtc));
        });
    }

    [Test]
    public void CreateProduct_WhenStallInactive_ReturnsFailure()
    {
        PlayerStall stall = CreateStall();
        stall.IsActive = false;
        PlayerStallAggregate aggregate = PlayerStallAggregate.FromEntity(stall);

        PlayerStallProductDescriptor descriptor = new(
            StallId: stall.Id,
            ResRef: "item_resref",
            Name: "Fine Sword",
            Description: null,
            Price: 5000,
            Quantity: 1,
            BaseItemType: 0,
            ItemData: new byte[] { 0x01, 0x02 },
            ConsignorPersonaId: null,
            ConsignorDisplayName: null,
            Notes: null,
            SortOrder: 0,
            IsActive: true,
            ListedUtc: DateTime.UtcNow,
            UpdatedUtc: DateTime.UtcNow);

        PlayerStallDomainResult<StallProduct> result = aggregate.CreateProduct(descriptor);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo(PlayerStallError.StallInactive));
    }

    [Test]
    public void TryUpdateProductPrice_WhenAuthorized_ReturnsMutation()
    {
        PlayerStall stall = CreateStall();
        string ownerPersona = $"Character:{Guid.NewGuid()}";
        stall.OwnerPersonaId = ownerPersona;
        stall.Members = new List<PlayerStallMember>
        {
            CreateMember(stall.Id, ownerPersona, canManageInventory: true)
        };

        StallProduct product = CreateProduct(stall.Id, price: 1_500, productId: 77);
        PlayerStallAggregate aggregate = PlayerStallAggregate.FromEntity(stall);

        PlayerStallDomainResult<Func<PlayerStall, StallProduct, bool>> result = aggregate.TryUpdateProductPrice(
            ownerPersona,
            product,
            newPrice: 2_750);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Payload, Is.Not.Null);

        PlayerStall persistedStall = CreateStall();
        persistedStall.Id = stall.Id;
        StallProduct persistedProduct = CreateProduct(stall.Id, price: 1_500, productId: 77);

        bool applied = result.Payload!(persistedStall, persistedProduct);
        Assert.That(applied, Is.True);
        Assert.That(persistedProduct.Price, Is.EqualTo(2_750));
    }

    [Test]
    public void TryUpdateProductPrice_WhenUnauthorized_ReturnsFailure()
    {
        PlayerStall stall = CreateStall();
        string ownerPersona = $"Character:{Guid.NewGuid()}";
        stall.OwnerPersonaId = ownerPersona;
        stall.Members = new List<PlayerStallMember>
        {
            CreateMember(stall.Id, ownerPersona, canManageInventory: true)
        };

        StallProduct product = CreateProduct(stall.Id, price: 900, productId: 5);
        PlayerStallAggregate aggregate = PlayerStallAggregate.FromEntity(stall);

        PlayerStallDomainResult<Func<PlayerStall, StallProduct, bool>> result = aggregate.TryUpdateProductPrice(
            requestorPersonaId: "Character:unauthorized",
            product,
            newPrice: 1_100);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo(PlayerStallError.Unauthorized));
    }

    [Test]
    public void TryUpdateProductPrice_WhenNegative_ReturnsFailure()
    {
        PlayerStall stall = CreateStall();
        string managerPersona = $"Character:{Guid.NewGuid()}";
        stall.OwnerPersonaId = managerPersona;
        stall.Members = new List<PlayerStallMember>
        {
            CreateMember(stall.Id, managerPersona, canManageInventory: true)
        };

        StallProduct product = CreateProduct(stall.Id, price: 900, productId: 12);
        PlayerStallAggregate aggregate = PlayerStallAggregate.FromEntity(stall);

        PlayerStallDomainResult<Func<PlayerStall, StallProduct, bool>> result = aggregate.TryUpdateProductPrice(
            managerPersona,
            product,
            newPrice: -1);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo(PlayerStallError.PriceOutOfRange));
    }

    [Test]
    public void TryUpdateProductPrice_WhenProductNotFromStall_ReturnsFailure()
    {
        PlayerStall stall = CreateStall();
        string managerPersona = $"Character:{Guid.NewGuid()}";
        stall.OwnerPersonaId = managerPersona;
        stall.Members = new List<PlayerStallMember>
        {
            CreateMember(stall.Id, managerPersona, canManageInventory: true)
        };

        StallProduct product = CreateProduct(stall.Id + 1, price: 900, productId: 12);
        PlayerStallAggregate aggregate = PlayerStallAggregate.FromEntity(stall);

        PlayerStallDomainResult<Func<PlayerStall, StallProduct, bool>> result = aggregate.TryUpdateProductPrice(
            managerPersona,
            product,
            newPrice: 1_200);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo(PlayerStallError.ProductNotFound));
    }

    private static PlayerStall CreateStall()
    {
        return new PlayerStall
        {
            Id = 42,
            Tag = "stall_test",
            AreaResRef = "ar_test",
            DailyRent = 1000,
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow,
            LeaseStartUtc = DateTime.UtcNow,
            NextRentDueUtc = DateTime.UtcNow.AddDays(1),
            IsActive = true
        };
    }

    private static StallProduct CreateProduct(long stallId, int price, long productId)
    {
        return new StallProduct
        {
            Id = productId,
            StallId = stallId,
            ResRef = "resref_test",
            Name = "Test Product",
            Description = null,
            Price = price,
            Quantity = 1,
            BaseItemType = null,
            ItemData = new byte[] { 0x01 }
        };
    }

    private static PlayerStallMember CreateMember(long stallId, string personaId, bool canManageInventory)
    {
        return new PlayerStallMember
        {
            StallId = stallId,
            PersonaId = personaId,
            DisplayName = personaId,
            CanManageInventory = canManageInventory,
            CanConfigureSettings = false,
            CanCollectEarnings = false,
            AddedByPersonaId = personaId,
            AddedUtc = DateTime.UtcNow
        };
    }
}
