using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities.Economy.Shops;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops.PlayerStalls;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using Moq;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.Economy.Shops.PlayerStalls;

[TestFixture]
public class PlayerStallServiceTests
{
    private Mock<IPlayerShopRepository> _shops = null!;
    private PlayerStallService _service = null!;
    private PlayerStall _persisted = null!;
    private List<PlayerStallMember> _capturedMembers = null!;
    private StallProduct _persistedProduct = null!;
    private int _updateStallAndProductCalls;

    [SetUp]
    public void SetUp()
    {
        _shops = new Mock<IPlayerShopRepository>();
        _service = new PlayerStallService(_shops.Object);
        _persisted = CreateStall();
        _capturedMembers = new List<PlayerStallMember>();
        _persistedProduct = new StallProduct
        {
            Id = 101,
            StallId = _persisted.Id,
            ResRef = "resref_item",
            Name = "Fine Blade",
            Description = "A masterwork blade.",
            Price = 1_200,
            Quantity = 1,
            BaseItemType = 5,
            ItemData = new byte[] { 0x10, 0x20 },
            SortOrder = 0,
            IsActive = true,
            ListedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow
        };
        _persisted.Inventory.Add(_persistedProduct);
        _updateStallAndProductCalls = 0;

        _shops
            .Setup(r => r.GetShopById(It.IsAny<long>()))
            .Returns<long>(id => id == _persisted.Id ? Clone(_persisted) : null);

        _shops
            .Setup(r => r.GetShopWithMembers(It.IsAny<long>()))
            .Returns<long>(id => id == _persisted.Id ? Clone(_persisted) : null);

        _shops
            .Setup(r => r.UpdateShop(It.IsAny<long>(), It.IsAny<Action<PlayerStall>>()))
            .Returns<long, Action<PlayerStall>>((id, action) =>
            {
                if (id != _persisted.Id)
                {
                    return false;
                }

                action(_persisted);
                return true;
            });

        _shops
            .Setup(r => r.UpdateShopWithMembers(
                It.IsAny<long>(),
                It.IsAny<Action<PlayerStall>>(),
                It.IsAny<IEnumerable<PlayerStallMember>>()))
            .Returns<long, Action<PlayerStall>, IEnumerable<PlayerStallMember>>((id, action, members) =>
            {
                if (id != _persisted.Id)
                {
                    return false;
                }

                action(_persisted);
                _capturedMembers = new List<PlayerStallMember>();
                foreach (PlayerStallMember member in members)
                {
                    _capturedMembers.Add(Clone(member));
                }

                return true;
            });

        _shops
            .Setup(r => r.GetProductById(It.IsAny<long>(), It.IsAny<long>()))
            .Returns<long, long>((stallId, productId) =>
                stallId == _persisted.Id && productId == _persistedProduct.Id
                    ? Clone(_persistedProduct)
                    : null);

        _shops
            .Setup(r => r.UpdateStallAndProduct(
                It.IsAny<long>(),
                It.IsAny<long>(),
                It.IsAny<Func<PlayerStall, StallProduct, bool>>()))
            .Returns<long, long, Func<PlayerStall, StallProduct, bool>>((stallId, productId, update) =>
            {
                if (stallId != _persisted.Id || productId != _persistedProduct.Id)
                {
                    return false;
                }

                _updateStallAndProductCalls++;

                PlayerStall persistedStall = Clone(_persisted);
                StallProduct persistedProduct = Clone(_persistedProduct);

                bool shouldPersist = update(persistedStall, persistedProduct);
                if (!shouldPersist)
                {
                    return false;
                }

                _persisted = persistedStall;
                _persistedProduct = persistedProduct;
                return true;
            });

        _shops
            .Setup(r => r.HasActiveOwnershipInArea(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<long>()))
            .Returns(false);

    }

    [Test]
    public async Task ClaimAsync_WhenStallExistsAndPersonaGuid_ReturnsSuccess()
    {
        Guid ownerGuid = Guid.NewGuid();
        PersonaId persona = PersonaId.FromCharacter(CharacterId.From(ownerGuid));
        Guid coinHouseAccount = Guid.NewGuid();
        DateTime leaseStart = new DateTime(2025, 11, 03, 10, 15, 00, DateTimeKind.Utc);

        ClaimPlayerStallRequest request = new(
            _persisted.Id,
            _persisted.AreaResRef,
            _persisted.Tag,
            persona,
            "Aria Moonwhisper",
            coinHouseAccount,
            HoldEarningsInStall: true,
            LeaseStartUtc: leaseStart,
            NextRentDueUtc: leaseStart.AddHours(12));

        PlayerStallServiceResult result = await _service.ClaimAsync(request, CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(result.Data, Is.Not.Null);
        Assert.That(result.Data!["stallId"], Is.EqualTo(_persisted.Id));
        Assert.Multiple(() =>
        {
            Assert.That(_persisted.OwnerCharacterId, Is.EqualTo(ownerGuid));
            Assert.That(_persisted.OwnerPersonaId, Is.EqualTo(persona.ToString()));
            Assert.That(_persisted.OwnerDisplayName, Is.EqualTo("Aria Moonwhisper"));
            Assert.That(_persisted.CoinHouseAccountId, Is.EqualTo(coinHouseAccount));
            Assert.That(_persisted.HoldEarningsInStall, Is.True);
            Assert.That(_persisted.LeaseStartUtc, Is.EqualTo(leaseStart));
            Assert.That(_persisted.NextRentDueUtc, Is.EqualTo(leaseStart.AddHours(12)));
            Assert.That(_persisted.LastRentPaidUtc, Is.EqualTo(leaseStart));
            Assert.That(_persisted.IsActive, Is.True);
        });
        Assert.That(_capturedMembers, Has.Count.EqualTo(1));
        PlayerStallMember ownerMember = _capturedMembers[0];
        Assert.Multiple(() =>
        {
            Assert.That(ownerMember.PersonaId, Is.EqualTo(persona.ToString()));
            Assert.That(ownerMember.CanManageInventory, Is.True);
            Assert.That(ownerMember.CanConfigureSettings, Is.True);
            Assert.That(ownerMember.CanCollectEarnings, Is.True);
        });
    }

    [Test]
    public async Task ClaimAsync_WhenStallMissing_ReturnsNotFound()
    {
        ClaimPlayerStallRequest request = new(
            999,
            "market_area",
            "stall_999",
            PersonaId.FromCharacter(CharacterId.New()),
            "Aria",
            null,
            HoldEarningsInStall: false,
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(1));

        PlayerStallServiceResult result = await _service.ClaimAsync(request, CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo(PlayerStallError.StallNotFound));
    Assert.That(_capturedMembers, Is.Empty);
    }

    [Test]
    public async Task ClaimAsync_WhenPersonaNotGuidBacked_ReturnsFailure()
    {
        ClaimPlayerStallRequest request = new(
            _persisted.Id,
            _persisted.AreaResRef,
            _persisted.Tag,
            PersonaId.FromSystem("stall-daemon"),
            "Daemon",
            null,
            HoldEarningsInStall: false,
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(1));

        PlayerStallServiceResult result = await _service.ClaimAsync(request, CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo(PlayerStallError.PersonaNotGuidBacked));
        _shops.Verify(r => r.UpdateShopWithMembers(
            It.IsAny<long>(),
            It.IsAny<Action<PlayerStall>>(),
            It.IsAny<IEnumerable<PlayerStallMember>>()), Times.Never);
    Assert.That(_capturedMembers, Is.Empty);
    }

    [Test]
    public async Task ClaimAsync_WhenAreaResRefMismatch_ReturnsFailure()
    {
        ClaimPlayerStallRequest request = new(
            _persisted.Id,
            AreaResRef: "wrong_area",
            PlaceableTag: _persisted.Tag,
            OwnerPersona: PersonaId.FromCharacter(CharacterId.New()),
            OwnerDisplayName: "Aria",
            CoinHouseAccountId: null,
            HoldEarningsInStall: false,
            LeaseStartUtc: DateTime.UtcNow,
            NextRentDueUtc: DateTime.UtcNow.AddHours(6));

        PlayerStallServiceResult result = await _service.ClaimAsync(request, CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo(PlayerStallError.PlaceableMismatch));
        Assert.That(_capturedMembers, Is.Empty);
    }

    [Test]
    public async Task ClaimAsync_WhenPlaceableTagMismatch_ReturnsFailure()
    {
        ClaimPlayerStallRequest request = new(
            _persisted.Id,
            AreaResRef: _persisted.AreaResRef,
            PlaceableTag: "different_tag",
            OwnerPersona: PersonaId.FromCharacter(CharacterId.New()),
            OwnerDisplayName: "Aria",
            CoinHouseAccountId: null,
            HoldEarningsInStall: false,
            LeaseStartUtc: DateTime.UtcNow,
            NextRentDueUtc: DateTime.UtcNow.AddHours(6));

        PlayerStallServiceResult result = await _service.ClaimAsync(request, CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo(PlayerStallError.PlaceableMismatch));
        Assert.That(_capturedMembers, Is.Empty);
    }

    [Test]
    public async Task ClaimAsync_WhenOwnerHasOtherStallInArea_ReturnsOwnershipViolation()
    {
        _shops
            .Setup(r => r.HasActiveOwnershipInArea(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<long>()))
            .Returns(true);

        ClaimPlayerStallRequest request = new(
            _persisted.Id,
            _persisted.AreaResRef,
            _persisted.Tag,
            PersonaId.FromCharacter(CharacterId.New()),
            "Aria",
            null,
            HoldEarningsInStall: false,
            DateTime.UtcNow,
            DateTime.UtcNow.AddHours(6));

        PlayerStallServiceResult result = await _service.ClaimAsync(request, CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo(PlayerStallError.OwnershipRuleViolation));
        _shops.Verify(r => r.UpdateShopWithMembers(
            It.IsAny<long>(),
            It.IsAny<Action<PlayerStall>>(),
            It.IsAny<IEnumerable<PlayerStallMember>>()), Times.Never);
        Assert.That(_capturedMembers, Is.Empty);
    }

    [Test]
    public async Task ClaimAsync_WhenCoOwnersProvided_PersistsMembers()
    {
        PersonaId owner = PersonaId.FromCharacter(CharacterId.New());
        PlayerStallCoOwnerRequest coOwner = new(
            Persona: PersonaId.FromCharacter(CharacterId.New()),
            DisplayName: "Corin",
            CanManageInventory: true,
            CanConfigureSettings: false,
            CanCollectEarnings: true);

        ClaimPlayerStallRequest request = new(
            _persisted.Id,
            _persisted.AreaResRef,
            _persisted.Tag,
            owner,
            "Aria",
            null,
            HoldEarningsInStall: false,
            LeaseStartUtc: DateTime.UtcNow,
            NextRentDueUtc: DateTime.UtcNow.AddHours(6),
            CoOwners: new[] { coOwner });

        PlayerStallServiceResult result = await _service.ClaimAsync(request, CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(_capturedMembers, Has.Count.EqualTo(2));
        PlayerStallMember second = _capturedMembers[1];
        Assert.Multiple(() =>
        {
            Assert.That(second.PersonaId, Is.EqualTo(coOwner.Persona.ToString()));
            Assert.That(second.DisplayName, Is.EqualTo("Corin"));
            Assert.That(second.CanManageInventory, Is.True);
            Assert.That(second.CanConfigureSettings, Is.False);
            Assert.That(second.CanCollectEarnings, Is.True);
            Assert.That(second.AddedByPersonaId, Is.EqualTo(owner.ToString()));
        });
    }

    [Test]
    public async Task ReleaseAsync_WhenOwnerMatches_ReturnsSuccess()
    {
        Guid ownerGuid = Guid.NewGuid();
        PersonaId persona = PersonaId.FromCharacter(CharacterId.From(ownerGuid));
        _persisted.OwnerCharacterId = ownerGuid;
        _persisted.OwnerPersonaId = persona.ToString();

        ReleasePlayerStallRequest request = new(
            _persisted.Id,
            persona,
            Force: false,
            ReleasedUtc: new DateTime(2025, 11, 03, 12, 00, 00, DateTimeKind.Utc));

        PlayerStallServiceResult result = await _service.ReleaseAsync(request, CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(_persisted.OwnerCharacterId, Is.Null);
        Assert.That(_persisted.IsActive, Is.False);
        Assert.That(_persisted.DeactivatedUtc, Is.EqualTo(request.ReleasedUtc));
    }

    [Test]
    public async Task ReleaseAsync_WhenNotOwnerAndNotForced_ReturnsFailure()
    {
        _persisted.OwnerCharacterId = Guid.NewGuid();
        _persisted.OwnerPersonaId = "Character:other";

        ReleasePlayerStallRequest request = new(
            _persisted.Id,
            PersonaId.FromCharacter(CharacterId.New()),
            Force: false,
            ReleasedUtc: DateTime.UtcNow);

        PlayerStallServiceResult result = await _service.ReleaseAsync(request, CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo(PlayerStallError.NotOwner));
        _shops.Verify(r => r.UpdateShop(It.IsAny<long>(), It.IsAny<Action<PlayerStall>>()), Times.Never);
    }

    [Test]
    public async Task ListProductAsync_WhenActive_ReturnsSuccessAndPersistsProduct()
    {
        byte[] originalData = { 0x10, 0x20 };
        StallProduct? capturedProduct = null;

        _shops
            .Setup(r => r.AddProductToShop(It.IsAny<long>(), It.IsAny<StallProduct>()))
            .Callback<long, StallProduct>((_, product) =>
            {
                capturedProduct = product;
                _persisted.Inventory.Add(product);
            });

        ListStallProductRequest request = new(
            _persisted.Id,
            "resref_item",
            "Fine Blade",
            "A masterwork blade.",
            Price: 5000,
            Quantity: 1,
            BaseItemType: 5,
            ItemData: (byte[])originalData.Clone(),
            ConsignorPersona: PersonaId.FromCharacter(CharacterId.New()),
            ConsignorDisplayName: "Consignor",
            Notes: "Handle with care",
            SortOrder: 10,
            IsActive: true,
            ListedUtc: DateTime.UtcNow,
            UpdatedUtc: DateTime.UtcNow);

        PlayerStallServiceResult result = await _service.ListProductAsync(request, CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(capturedProduct, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(capturedProduct!.ResRef, Is.EqualTo("resref_item"));
            Assert.That(capturedProduct!.Name, Is.EqualTo("Fine Blade"));
            Assert.That(capturedProduct!.Price, Is.EqualTo(5000));
            Assert.That(capturedProduct!.Quantity, Is.EqualTo(1));
            Assert.That(capturedProduct!.ConsignedByDisplayName, Is.EqualTo("Consignor"));
            Assert.That(capturedProduct!.ItemData, Is.Not.SameAs(originalData));
        });
    }

    [Test]
    public async Task UpdateProductPriceAsync_WhenAuthorized_UpdatesPrice()
    {
        Guid ownerGuid = Guid.NewGuid();
        PersonaId ownerPersona = PersonaId.FromCharacter(CharacterId.From(ownerGuid));
        _persisted.OwnerCharacterId = ownerGuid;
        _persisted.OwnerPersonaId = ownerPersona.ToString();
        _persisted.Members.Add(new PlayerStallMember
        {
            StallId = _persisted.Id,
            PersonaId = ownerPersona.ToString(),
            DisplayName = "Owner",
            CanManageInventory = true,
            CanConfigureSettings = true,
            CanCollectEarnings = true,
            AddedByPersonaId = ownerPersona.ToString()
        });

        UpdateStallProductPriceRequest request = new(
            _persisted.Id,
            _persistedProduct.Id,
            ownerPersona,
            NewPrice: 4_200);

        PlayerStallServiceResult result = await _service.UpdateProductPriceAsync(request, CancellationToken.None);

        Assert.That(result.Success, Is.True);
        Assert.That(_persistedProduct.Price, Is.EqualTo(4_200));
        Assert.That(_updateStallAndProductCalls, Is.EqualTo(1));
        Assert.That(result.Data, Is.Not.Null);
        Assert.That(result.Data!["price"], Is.EqualTo(4_200));
    }

    [Test]
    public async Task UpdateProductPriceAsync_WhenUnauthorized_ReturnsFailure()
    {
        PersonaId requestor = PersonaId.FromCharacter(CharacterId.New());

        UpdateStallProductPriceRequest request = new(
            _persisted.Id,
            _persistedProduct.Id,
            requestor,
            NewPrice: 3_000);

        PlayerStallServiceResult result = await _service.UpdateProductPriceAsync(request, CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo(PlayerStallError.Unauthorized));
        Assert.That(_updateStallAndProductCalls, Is.EqualTo(0));
        Assert.That(_persistedProduct.Price, Is.EqualTo(1_200));
    }

    [Test]
    public async Task UpdateProductPriceAsync_WhenProductMissing_ReturnsFailure()
    {
        Guid ownerGuid = Guid.NewGuid();
        PersonaId ownerPersona = PersonaId.FromCharacter(CharacterId.From(ownerGuid));
        _persisted.OwnerCharacterId = ownerGuid;
        _persisted.OwnerPersonaId = ownerPersona.ToString();
        _persisted.Members.Add(new PlayerStallMember
        {
            StallId = _persisted.Id,
            PersonaId = ownerPersona.ToString(),
            DisplayName = "Owner",
            CanManageInventory = true,
            CanConfigureSettings = false,
            CanCollectEarnings = false,
            AddedByPersonaId = ownerPersona.ToString()
        });

        _shops
            .Setup(r => r.GetProductById(It.IsAny<long>(), It.IsAny<long>()))
            .Returns<long, long>((_, _) => null);

        UpdateStallProductPriceRequest request = new(
            _persisted.Id,
            ProductId: 9999,
            ownerPersona,
            NewPrice: 2_000);

        PlayerStallServiceResult result = await _service.UpdateProductPriceAsync(request, CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo(PlayerStallError.ProductNotFound));
        Assert.That(_updateStallAndProductCalls, Is.EqualTo(0));
    }

    private static PlayerStall CreateStall()
    {
        return new PlayerStall
        {
            Id = 42,
            Tag = "stall_test",
            AreaResRef = "ar_test",
            SettlementTag = "settlement",
            DailyRent = 1000,
            LeaseStartUtc = DateTime.UtcNow,
            NextRentDueUtc = DateTime.UtcNow.AddDays(1),
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow,
            IsActive = true,
            Inventory = new List<StallProduct>(),
            Members = new List<PlayerStallMember>(),
            LedgerEntries = new List<PlayerStallLedgerEntry>(),
            Transactions = new List<StallTransaction>()
        };
    }

    private static PlayerStall Clone(PlayerStall source)
    {
        return new PlayerStall
        {
            Id = source.Id,
            Tag = source.Tag,
            AreaResRef = source.AreaResRef,
            SettlementTag = source.SettlementTag,
            OwnerCharacterId = source.OwnerCharacterId,
            OwnerPersonaId = source.OwnerPersonaId,
            OwnerDisplayName = source.OwnerDisplayName,
            CoinHouseAccountId = source.CoinHouseAccountId,
            HoldEarningsInStall = source.HoldEarningsInStall,
            EscrowBalance = source.EscrowBalance,
            LifetimeGrossSales = source.LifetimeGrossSales,
            LifetimeNetEarnings = source.LifetimeNetEarnings,
            DailyRent = source.DailyRent,
            LeaseStartUtc = source.LeaseStartUtc,
            NextRentDueUtc = source.NextRentDueUtc,
            LastRentPaidUtc = source.LastRentPaidUtc,
            SuspendedUtc = source.SuspendedUtc,
            IsActive = source.IsActive,
            CreatedUtc = source.CreatedUtc,
            UpdatedUtc = source.UpdatedUtc,
            DeactivatedUtc = source.DeactivatedUtc,
            Inventory = source.Inventory.ConvertAll(Clone),
            Members = source.Members.ConvertAll(Clone),
            LedgerEntries = new List<PlayerStallLedgerEntry>(source.LedgerEntries),
            Transactions = new List<StallTransaction>(source.Transactions)
        };
    }

    private static StallProduct Clone(StallProduct source)
    {
        return new StallProduct
        {
            Id = source.Id,
            StallId = source.StallId,
            ResRef = source.ResRef,
            Name = source.Name,
            Description = source.Description,
            Price = source.Price,
            Quantity = source.Quantity,
            BaseItemType = source.BaseItemType,
            ItemData = source.ItemData is null ? Array.Empty<byte>() : (byte[])source.ItemData.Clone(),
            ConsignedByPersonaId = source.ConsignedByPersonaId,
            ConsignedByDisplayName = source.ConsignedByDisplayName,
            Notes = source.Notes,
            SortOrder = source.SortOrder,
            IsActive = source.IsActive,
            ListedUtc = source.ListedUtc,
            UpdatedUtc = source.UpdatedUtc,
            SoldOutUtc = source.SoldOutUtc
        };
    }

    private static PlayerStallMember Clone(PlayerStallMember source)
    {
        return new PlayerStallMember
        {
            Id = source.Id,
            StallId = source.StallId,
            PersonaId = source.PersonaId,
            DisplayName = source.DisplayName,
            CanManageInventory = source.CanManageInventory,
            CanConfigureSettings = source.CanConfigureSettings,
            CanCollectEarnings = source.CanCollectEarnings,
            AddedByPersonaId = source.AddedByPersonaId,
            AddedUtc = source.AddedUtc,
            RevokedUtc = source.RevokedUtc
        };
    }
}
