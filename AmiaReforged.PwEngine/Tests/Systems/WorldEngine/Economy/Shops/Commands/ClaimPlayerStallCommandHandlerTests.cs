using System;
using System.Threading.Tasks;
using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities.Economy.Shops;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Shops.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using Moq;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.Economy.Shops.Commands;

[TestFixture]
public class ClaimPlayerStallCommandHandlerTests
{
    private Mock<IPlayerShopRepository> _shops = null!;
    private ClaimPlayerStallCommandHandler _handler = null!;
    private PlayerStall _persisted = null!;
    private Guid _ownerGuid;
    private PersonaId _ownerPersona;

    [SetUp]
    public void SetUp()
    {
        _shops = new Mock<IPlayerShopRepository>();
        _handler = new ClaimPlayerStallCommandHandler(_shops.Object);

        _ownerGuid = Guid.NewGuid();
        _ownerPersona = PersonaId.FromCharacter(CharacterId.From(_ownerGuid));

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
    public async Task Given_UnownedStall_When_Claiming_Then_AssignsOwner()
    {
        Guid coinHouseAccount = Guid.NewGuid();
        DateTime leaseStart = new DateTime(2025, 11, 03, 10, 15, 00, DateTimeKind.Utc);

        ClaimPlayerStallCommand command = ClaimPlayerStallCommand.Create(
            _persisted.Id,
            _ownerPersona,
            "Aria Moonwhisper",
            coinHouseAccountId: coinHouseAccount,
            holdEarningsInStall: true,
            rentInterval: TimeSpan.FromHours(12),
            leaseStartUtc: leaseStart);

        CommandResult result = await _handler.HandleAsync(command);

        Assert.That(result.Success, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(_persisted.OwnerCharacterId, Is.EqualTo(_ownerGuid));
            Assert.That(_persisted.OwnerPersonaId, Is.EqualTo(_ownerPersona.ToString()));
            Assert.That(_persisted.OwnerDisplayName, Is.EqualTo("Aria Moonwhisper"));
            Assert.That(_persisted.CoinHouseAccountId, Is.EqualTo(coinHouseAccount));
            Assert.That(_persisted.HoldEarningsInStall, Is.True);
            Assert.That(_persisted.LeaseStartUtc, Is.EqualTo(leaseStart));
            Assert.That(_persisted.NextRentDueUtc, Is.EqualTo(command.NextRentDueUtc));
            Assert.That(_persisted.LastRentPaidUtc, Is.EqualTo(leaseStart));
            Assert.That(_persisted.IsActive, Is.True);
            Assert.That(_persisted.SuspendedUtc, Is.Null);
        });
    }

    [Test]
    public async Task Given_StallClaimedBySomeoneElse_When_Claiming_Then_Fails()
    {
        Guid existingOwner = Guid.NewGuid();
        PersonaId existingPersona = PersonaId.FromCharacter(CharacterId.From(existingOwner));
        _persisted.OwnerCharacterId = existingOwner;
        _persisted.OwnerPersonaId = existingPersona.ToString();

        ClaimPlayerStallCommand command = ClaimPlayerStallCommand.Create(
            _persisted.Id,
            _ownerPersona,
            "Aria");

        CommandResult result = await _handler.HandleAsync(command);

        Assert.That(result.Success, Is.False);
        _shops.Verify(r => r.UpdateShop(It.IsAny<long>(), It.IsAny<Action<PlayerStall>>()), Times.Never);
    }

    [Test]
    public async Task Given_StallDoesNotExist_When_Claiming_Then_Fails()
    {
        ClaimPlayerStallCommand command = ClaimPlayerStallCommand.Create(
            999,
            _ownerPersona,
            "Aria");

        CommandResult result = await _handler.HandleAsync(command);

        Assert.That(result.Success, Is.False);
    }

    [Test]
    public async Task Given_PersonaWithoutGuid_When_Claiming_Then_Fails()
    {
        PersonaId systemPersona = PersonaId.FromSystem("stall-daemon");
        ClaimPlayerStallCommand command = ClaimPlayerStallCommand.Create(
            _persisted.Id,
            systemPersona,
            "Daemon");

        CommandResult result = await _handler.HandleAsync(command);

        Assert.That(result.Success, Is.False);
        _shops.Verify(r => r.UpdateShop(It.IsAny<long>(), It.IsAny<Action<PlayerStall>>()), Times.Never);
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
            UpdatedUtc = DateTime.UtcNow
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
            DeactivatedUtc = source.DeactivatedUtc
        };
    }
}
