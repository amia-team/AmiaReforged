using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities.Economy.Shops;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops.PlayerStalls.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops.PlayerStalls.Events;
using Moq;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Tests.Shops.PlayerStalls;

/// <summary>
/// BDD-style tests for DepositStallRentCommandHandler.
/// Tests the handler's ability to process rent deposits into stall escrow.
/// </summary>
[TestFixture]
public class DepositStallRentCommandHandlerTests
{
    private Mock<IPlayerShopRepository> _shops = null!;
    private Mock<IEventBus> _eventBus = null!;
    private DepositStallRentCommandHandler _handler = null!;
    private PlayerStall _stall = null!;
    private string _depositorPersonaId = null!;
    private string _depositorDisplayName = null!;

    [SetUp]
    public void SetUp()
    {
        _shops = new Mock<IPlayerShopRepository>();
        _eventBus = new Mock<IEventBus>();
        _handler = new DepositStallRentCommandHandler(_shops.Object, _eventBus.Object);

        _depositorPersonaId = $"Character:{Guid.NewGuid()}";
        _depositorDisplayName = "Elara Swiftblade";

        _stall = CreateStall();
        _stall.OwnerPersonaId = _depositorPersonaId;
        _stall.EscrowBalance = 5000;
        _stall.IsActive = true;

        _shops
            .Setup(r => r.GetShopById(It.IsAny<long>()))
            .Returns<long>(id => id == _stall.Id ? _stall : null);

        _shops
            .Setup(r => r.UpdateShop(It.IsAny<long>(), It.IsAny<Action<PlayerStall>>()))
            .Returns<long, Action<PlayerStall>>((id, action) =>
            {
                if (id != _stall.Id)
                {
                    return false;
                }

                action(_stall);
                return true;
            });
    }

    [Test]
    public async Task Given_ValidDeposit_When_HandlingCommand_Then_ReturnsSuccess()
    {
        DepositStallRentCommand command = DepositStallRentCommand.Create(
            _stall.Id,
            2500,
            _depositorPersonaId,
            _depositorDisplayName,
            DateTime.UtcNow);

        CommandResult result = await _handler.HandleAsync(command);

        Assert.That(result.Success, Is.True);
        Assert.That(result.ErrorMessage, Is.Null);
    }

    [Test]
    public async Task Given_ValidDeposit_When_HandlingCommand_Then_UpdatesEscrowBalance()
    {
        int initialBalance = _stall.EscrowBalance;
        int depositAmount = 3000;

        DepositStallRentCommand command = DepositStallRentCommand.Create(
            _stall.Id,
            depositAmount,
            _depositorPersonaId,
            _depositorDisplayName,
            DateTime.UtcNow);

        await _handler.HandleAsync(command);

        Assert.That(_stall.EscrowBalance, Is.EqualTo(initialBalance + depositAmount));
    }

    [Test]
    public async Task Given_ValidDeposit_When_HandlingCommand_Then_AddsLedgerEntry()
    {
        int initialLedgerCount = _stall.LedgerEntries.Count;
        int depositAmount = 1500;

        DepositStallRentCommand command = DepositStallRentCommand.Create(
            _stall.Id,
            depositAmount,
            _depositorPersonaId,
            _depositorDisplayName,
            DateTime.UtcNow);

        await _handler.HandleAsync(command);

        Assert.That(_stall.LedgerEntries.Count, Is.EqualTo(initialLedgerCount + 1));

        PlayerStallLedgerEntry? entry = _stall.LedgerEntries.LastOrDefault();
        Assert.That(entry, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(entry!.EntryType, Is.EqualTo(PlayerStallLedgerEntryType.Deposit));
            Assert.That(entry.Amount, Is.EqualTo(depositAmount));
            Assert.That(entry.StallId, Is.EqualTo(_stall.Id));
            Assert.That(entry.Description, Does.Contain(_depositorDisplayName));
            Assert.That(entry.Description, Does.Contain("1,500")); // Updated to match formatted number
        });
    }

    [Test]
    public async Task Given_ValidDeposit_When_HandlingCommand_Then_PublishesStallEscrowDepositedEvent()
    {
        int depositAmount = 2000;
        DepositStallRentCommand command = DepositStallRentCommand.Create(
            _stall.Id,
            depositAmount,
            _depositorPersonaId,
            _depositorDisplayName,
            DateTime.UtcNow);

        await _handler.HandleAsync(command);

        _eventBus.Verify(bus =>
            bus.PublishAsync(
                It.Is<StallEscrowDepositedEvent>(evt =>
                    evt.StallId == _stall.Id &&
                    evt.DepositAmount == depositAmount &&
                    evt.DepositorPersonaId == _depositorPersonaId &&
                    evt.DepositorDisplayName == _depositorDisplayName &&
                    evt.NewEscrowBalance == 7000),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task Given_StallNotFound_When_HandlingCommand_Then_ReturnsFailure()
    {
        DepositStallRentCommand command = DepositStallRentCommand.Create(
            999L, // Non-existent stall
            1000,
            _depositorPersonaId,
            _depositorDisplayName,
            DateTime.UtcNow);

        CommandResult result = await _handler.HandleAsync(command);

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("not found"));
    }

    [Test]
    public async Task Given_UnauthorizedDepositor_When_HandlingCommand_Then_ReturnsFailure()
    {
        string unauthorizedPersona = $"Character:{Guid.NewGuid()}";
        DepositStallRentCommand command = DepositStallRentCommand.Create(
            _stall.Id,
            1000,
            unauthorizedPersona,
            "Unauthorized User",
            DateTime.UtcNow);

        CommandResult result = await _handler.HandleAsync(command);

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("permission").Or.Contains("Unauthorized"));
    }

    [Test]
    public async Task Given_InactiveStall_When_HandlingCommand_Then_ReturnsFailure()
    {
        _stall.IsActive = false;

        DepositStallRentCommand command = DepositStallRentCommand.Create(
            _stall.Id,
            1000,
            _depositorPersonaId,
            _depositorDisplayName,
            DateTime.UtcNow);

        CommandResult result = await _handler.HandleAsync(command);

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("inactive").Or.Contains("active"));
    }

    [Test]
    public async Task Given_DepositExceeding100Days_When_HandlingCommand_Then_ReturnsFailure()
    {
        _stall.DailyRent = 1000;
        int tooLargeDeposit = 1000 * 100 + 1; // Exceeds 100 days

        DepositStallRentCommand command = DepositStallRentCommand.Create(
            _stall.Id,
            tooLargeDeposit,
            _depositorPersonaId,
            _depositorDisplayName,
            DateTime.UtcNow);

        CommandResult result = await _handler.HandleAsync(command);

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("exceed").Or.Contains("large").Or.Contains("100 days"));
    }

    [Test]
    public async Task Given_RepositoryUpdateFails_When_HandlingCommand_Then_ReturnsFailure()
    {
        _shops
            .Setup(r => r.UpdateShop(It.IsAny<long>(), It.IsAny<Action<PlayerStall>>()))
            .Returns(false);

        DepositStallRentCommand command = DepositStallRentCommand.Create(
            _stall.Id,
            1000,
            _depositorPersonaId,
            _depositorDisplayName,
            DateTime.UtcNow);

        CommandResult result = await _handler.HandleAsync(command);

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("Failed to update"));
    }

    [Test]
    public async Task Given_ValidDeposit_When_HandlingCommand_Then_UpdatesStallTimestamp()
    {
        DateTime depositTime = new DateTime(2025, 11, 15, 14, 30, 0, DateTimeKind.Utc);
        DepositStallRentCommand command = DepositStallRentCommand.Create(
            _stall.Id,
            2000,
            _depositorPersonaId,
            _depositorDisplayName,
            depositTime);

        await _handler.HandleAsync(command);

        Assert.That(_stall.UpdatedUtc, Is.EqualTo(depositTime));
    }

    [Test]
    public async Task Given_MultipleDeposits_When_HandlingCommands_Then_AccumulatesBalance()
    {
        int initialBalance = _stall.EscrowBalance;

        DepositStallRentCommand command1 = DepositStallRentCommand.Create(
            _stall.Id, 1000, _depositorPersonaId, _depositorDisplayName, DateTime.UtcNow);
        DepositStallRentCommand command2 = DepositStallRentCommand.Create(
            _stall.Id, 1500, _depositorPersonaId, _depositorDisplayName, DateTime.UtcNow);
        DepositStallRentCommand command3 = DepositStallRentCommand.Create(
            _stall.Id, 2500, _depositorPersonaId, _depositorDisplayName, DateTime.UtcNow);

        await _handler.HandleAsync(command1);
        await _handler.HandleAsync(command2);
        await _handler.HandleAsync(command3);

        Assert.That(_stall.EscrowBalance, Is.EqualTo(initialBalance + 1000 + 1500 + 2500));
    }

    [Test]
    public async Task Given_ValidDeposit_When_HandlingCommand_Then_LedgerEntryContainsMetadata()
    {
        DepositStallRentCommand command = DepositStallRentCommand.Create(
            _stall.Id,
            1000,
            _depositorPersonaId,
            _depositorDisplayName,
            DateTime.UtcNow);

        await _handler.HandleAsync(command);

        PlayerStallLedgerEntry? entry = _stall.LedgerEntries.LastOrDefault();
        Assert.That(entry, Is.Not.Null);
        Assert.That(entry!.MetadataJson, Is.Not.Null);
        Assert.That(entry.MetadataJson, Does.Contain(_depositorPersonaId));
        Assert.That(entry.MetadataJson, Does.Contain(_depositorDisplayName));
        Assert.That(entry.MetadataJson, Does.Contain("rent_deposit"));
    }

    private static PlayerStall CreateStall()
    {
        return new PlayerStall
        {
            Id = 42,
            Tag = "stall_test",
            AreaResRef = "ar_test",
            DailyRent = 1000,
            EscrowBalance = 0,
            CreatedUtc = DateTime.UtcNow,
            UpdatedUtc = DateTime.UtcNow,
            LeaseStartUtc = DateTime.UtcNow,
            NextRentDueUtc = DateTime.UtcNow.AddDays(1),
            IsActive = true,
            LedgerEntries = new List<PlayerStallLedgerEntry>(),
            Inventory = new List<StallProduct>()
        };
    }
}
