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

    [SetUp]
    public void SetUp()
    {
        _shops = new Mock<IPlayerShopRepository>();
        _service = new PlayerStallService(_shops.Object);
        _persisted = CreateStall();

        _shops
            .Setup(r => r.GetShopById(It.IsAny<long>()))
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
    }

    [Test]
    public async Task ClaimAsync_WhenStallMissing_ReturnsNotFound()
    {
        ClaimPlayerStallRequest request = new(
            999,
            PersonaId.FromCharacter(CharacterId.New()),
            "Aria",
            null,
            HoldEarningsInStall: false,
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(1));

        PlayerStallServiceResult result = await _service.ClaimAsync(request, CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo(PlayerStallError.StallNotFound));
    }

    [Test]
    public async Task ClaimAsync_WhenPersonaNotGuidBacked_ReturnsFailure()
    {
        ClaimPlayerStallRequest request = new(
            _persisted.Id,
            PersonaId.FromSystem("stall-daemon"),
            "Daemon",
            null,
            HoldEarningsInStall: false,
            DateTime.UtcNow,
            DateTime.UtcNow.AddDays(1));

        PlayerStallServiceResult result = await _service.ClaimAsync(request, CancellationToken.None);

        Assert.That(result.Success, Is.False);
        Assert.That(result.Error, Is.EqualTo(PlayerStallError.PersonaNotGuidBacked));
        _shops.Verify(r => r.UpdateShop(It.IsAny<long>(), It.IsAny<Action<PlayerStall>>()), Times.Never);
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
            IsActive = true
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
            Inventory = new List<StallProduct>(source.Inventory)
        };
    }
}
