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
/// Tests for PayStallRentCommand and its handler.
/// Verifies that rent payments are correctly recorded, stall state is updated,
/// and appropriate domain events are published.
/// </summary>
[TestFixture]
public class PayStallRentCommandTests
{
    private Mock<IPlayerShopRepository> _shopRepo = null!;
    private Mock<IEventBus> _eventBus = null!;
    private PayStallRentCommandHandler _handler = null!;
    private PlayerStall _testStall = null!;

    [SetUp]
    public void SetUp()
    {
        _shopRepo = new Mock<IPlayerShopRepository>(MockBehavior.Strict);
        _eventBus = new Mock<IEventBus>(MockBehavior.Strict);

        _testStall = new PlayerStall
        {
            Id = 123L,
            Tag = "test_stall",
            AreaResRef = "test_area",
            SettlementTag = "1",
            OwnerCharacterId = Guid.NewGuid(),
            OwnerPersonaId = Guid.NewGuid().ToString(),
            DailyRent = 100,
            EscrowBalance = 500,
            LifetimeNetEarnings = 1000,
            NextRentDueUtc = DateTime.UtcNow.AddHours(-1),
            LastRentPaidUtc = DateTime.UtcNow.AddDays(-1),
            IsActive = true,
            LedgerEntries = new List<PlayerStallLedgerEntry>()
        };

        _handler = new PayStallRentCommandHandler(_shopRepo.Object, _eventBus.Object);
    }

    #region Command Creation Tests

    [Test]
    public void Create_WithValidParameters_ReturnsCommand()
    {
        // Arrange & Act
        PayStallRentCommand command = PayStallRentCommand.Create(
            stallId: 123L,
            rentAmount: 100,
            source: RentChargeSource.StallEscrow,
            timestamp: DateTime.UtcNow);

        // Assert
        Assert.That(command, Is.Not.Null);
        Assert.That(command.StallId, Is.EqualTo(123L));
        Assert.That(command.RentAmount, Is.EqualTo(100));
        Assert.That(command.Source, Is.EqualTo(RentChargeSource.StallEscrow));
    }

    [Test]
    public void Create_WithNegativeStallId_ThrowsArgumentException()
    {
        // Arrange, Act & Assert
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            PayStallRentCommand.Create(-1L, 100, RentChargeSource.StallEscrow, DateTime.UtcNow));

        Assert.That(ex.ParamName, Is.EqualTo("stallId"));
    }

    [Test]
    public void Create_WithZeroStallId_ThrowsArgumentException()
    {
        // Arrange, Act & Assert
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            PayStallRentCommand.Create(0L, 100, RentChargeSource.StallEscrow, DateTime.UtcNow));

        Assert.That(ex.ParamName, Is.EqualTo("stallId"));
    }

    [Test]
    public void Create_WithNegativeRentAmount_ThrowsArgumentException()
    {
        // Arrange, Act & Assert
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            PayStallRentCommand.Create(123L, -100, RentChargeSource.StallEscrow, DateTime.UtcNow));

        Assert.That(ex.ParamName, Is.EqualTo("rentAmount"));
    }

    [Test]
    public void Create_WithZeroRentAmount_Succeeds()
    {
        // Arrange & Act
        PayStallRentCommand command = PayStallRentCommand.Create(
            123L, 0, RentChargeSource.None, DateTime.UtcNow);

        // Assert
        Assert.That(command.RentAmount, Is.EqualTo(0));
    }

    #endregion

    #region Handler Tests - Successful Payment

    [Test]
    public async Task HandleAsync_WithValidCommand_UpdatesStallState()
    {
        // Arrange
        PayStallRentCommand command = PayStallRentCommand.Create(
            _testStall.Id, 100, RentChargeSource.StallEscrow, DateTime.UtcNow);

        _shopRepo.Setup(r => r.GetShopById(_testStall.Id)).Returns(_testStall);

        PlayerStall? capturedStall = null;
        _shopRepo.Setup(r => r.UpdateShop(_testStall.Id, It.IsAny<Action<PlayerStall>>()))
            .Callback<long, Action<PlayerStall>>((id, action) =>
            {
                action(_testStall);
                capturedStall = _testStall;
            })
            .Returns(true);

        _eventBus.Setup(e => e.PublishAsync(It.IsAny<StallRentPaidEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        CommandResult result = await _handler.HandleAsync(command);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(capturedStall, Is.Not.Null);
        Assert.That(capturedStall!.SuspendedUtc, Is.Null);
        Assert.That(capturedStall.DeactivatedUtc, Is.Null);
        Assert.That(capturedStall.IsActive, Is.True);
    }

    [Test]
    public async Task HandleAsync_WithEscrowSource_DeductsFromEscrowBalance()
    {
        // Arrange
        int initialEscrow = _testStall.EscrowBalance;
        PayStallRentCommand command = PayStallRentCommand.Create(
            _testStall.Id, 100, RentChargeSource.StallEscrow, DateTime.UtcNow);

        _shopRepo.Setup(r => r.GetShopById(_testStall.Id)).Returns(_testStall);

        _shopRepo.Setup(r => r.UpdateShop(_testStall.Id, It.IsAny<Action<PlayerStall>>()))
            .Callback<long, Action<PlayerStall>>((id, action) => action(_testStall))
            .Returns(true);

        _eventBus.Setup(e => e.PublishAsync(It.IsAny<StallRentPaidEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        CommandResult result = await _handler.HandleAsync(command);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(_testStall.EscrowBalance, Is.EqualTo(initialEscrow - 100));
    }

    [Test]
    public async Task HandleAsync_WithCoinhouseSource_DoesNotDeductFromEscrow()
    {
        // Arrange
        int initialEscrow = _testStall.EscrowBalance;
        PayStallRentCommand command = PayStallRentCommand.Create(
            _testStall.Id, 100, RentChargeSource.CoinhouseAccount, DateTime.UtcNow);

        _shopRepo.Setup(r => r.GetShopById(_testStall.Id)).Returns(_testStall);

        _shopRepo.Setup(r => r.UpdateShop(_testStall.Id, It.IsAny<Action<PlayerStall>>()))
            .Callback<long, Action<PlayerStall>>((id, action) => action(_testStall))
            .Returns(true);

        _eventBus.Setup(e => e.PublishAsync(It.IsAny<StallRentPaidEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        CommandResult result = await _handler.HandleAsync(command);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(_testStall.EscrowBalance, Is.EqualTo(initialEscrow)); // No deduction
    }

    [Test]
    public async Task HandleAsync_WithRentPayment_UpdatesLifetimeNetEarnings()
    {
        // Arrange
        int initialEarnings = _testStall.LifetimeNetEarnings;
        PayStallRentCommand command = PayStallRentCommand.Create(
            _testStall.Id, 100, RentChargeSource.StallEscrow, DateTime.UtcNow);

        _shopRepo.Setup(r => r.GetShopById(_testStall.Id)).Returns(_testStall);

        _shopRepo.Setup(r => r.UpdateShop(_testStall.Id, It.IsAny<Action<PlayerStall>>()))
            .Callback<long, Action<PlayerStall>>((id, action) => action(_testStall))
            .Returns(true);

        _eventBus.Setup(e => e.PublishAsync(It.IsAny<StallRentPaidEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        CommandResult result = await _handler.HandleAsync(command);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(_testStall.LifetimeNetEarnings, Is.EqualTo(initialEarnings - 100));
    }

    [Test]
    public async Task HandleAsync_WithRentPayment_AddsLedgerEntry()
    {
        // Arrange
        PayStallRentCommand command = PayStallRentCommand.Create(
            _testStall.Id, 100, RentChargeSource.StallEscrow, DateTime.UtcNow);

        _shopRepo.Setup(r => r.GetShopById(_testStall.Id)).Returns(_testStall);

        _shopRepo.Setup(r => r.UpdateShop(_testStall.Id, It.IsAny<Action<PlayerStall>>()))
            .Callback<long, Action<PlayerStall>>((id, action) => action(_testStall))
            .Returns(true);

        _eventBus.Setup(e => e.PublishAsync(It.IsAny<StallRentPaidEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        CommandResult result = await _handler.HandleAsync(command);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(_testStall.LedgerEntries, Has.Count.EqualTo(1));
        Assert.That(_testStall.LedgerEntries[0].EntryType, Is.EqualTo(PlayerStallLedgerEntryType.RentPayment));
        Assert.That(_testStall.LedgerEntries[0].Amount, Is.EqualTo(-100));
    }

    [Test]
    public async Task HandleAsync_WithZeroRent_DoesNotAddLedgerEntry()
    {
        // Arrange
        PayStallRentCommand command = PayStallRentCommand.Create(
            _testStall.Id, 0, RentChargeSource.None, DateTime.UtcNow);

        _shopRepo.Setup(r => r.GetShopById(_testStall.Id)).Returns(_testStall);

        _shopRepo.Setup(r => r.UpdateShop(_testStall.Id, It.IsAny<Action<PlayerStall>>()))
            .Callback<long, Action<PlayerStall>>((id, action) => action(_testStall))
            .Returns(true);

        _eventBus.Setup(e => e.PublishAsync(It.IsAny<StallRentPaidEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        CommandResult result = await _handler.HandleAsync(command);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(_testStall.LedgerEntries, Is.Empty);
    }

    [Test]
    public async Task HandleAsync_WithRentPayment_UpdatesNextRentDueUtc()
    {
        // Arrange
        DateTime originalDueDate = _testStall.NextRentDueUtc;
        PayStallRentCommand command = PayStallRentCommand.Create(
            _testStall.Id, 100, RentChargeSource.StallEscrow, DateTime.UtcNow);

        _shopRepo.Setup(r => r.GetShopById(_testStall.Id)).Returns(_testStall);

        _shopRepo.Setup(r => r.UpdateShop(_testStall.Id, It.IsAny<Action<PlayerStall>>()))
            .Callback<long, Action<PlayerStall>>((id, action) => action(_testStall))
            .Returns(true);

        _eventBus.Setup(e => e.PublishAsync(It.IsAny<StallRentPaidEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        CommandResult result = await _handler.HandleAsync(command);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(_testStall.NextRentDueUtc, Is.GreaterThan(originalDueDate));
    }

    [Test]
    public async Task HandleAsync_WithSuccessfulPayment_PublishesStallRentPaidEvent()
    {
        // Arrange
        PayStallRentCommand command = PayStallRentCommand.Create(
            _testStall.Id, 100, RentChargeSource.StallEscrow, DateTime.UtcNow);

        _shopRepo.Setup(r => r.GetShopById(_testStall.Id)).Returns(_testStall);

        _shopRepo.Setup(r => r.UpdateShop(_testStall.Id, It.IsAny<Action<PlayerStall>>()))
            .Callback<long, Action<PlayerStall>>((id, action) => action(_testStall))
            .Returns(true);

        StallRentPaidEvent? capturedEvent = null;
        _eventBus.Setup(e => e.PublishAsync(It.IsAny<StallRentPaidEvent>(), It.IsAny<CancellationToken>()))
            .Callback<StallRentPaidEvent, CancellationToken>((evt, ct) => capturedEvent = evt)
            .Returns(Task.CompletedTask);

        // Act
        CommandResult result = await _handler.HandleAsync(command);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(capturedEvent, Is.Not.Null);
        Assert.That(capturedEvent!.StallId, Is.EqualTo(_testStall.Id));
        Assert.That(capturedEvent.RentAmount, Is.EqualTo(100));
        Assert.That(capturedEvent.Source, Is.EqualTo(RentChargeSource.StallEscrow));
    }

    #endregion

    #region Handler Tests - Error Cases

    [Test]
    public async Task HandleAsync_WhenStallNotFound_ReturnsFailure()
    {
        // Arrange
        PayStallRentCommand command = PayStallRentCommand.Create(
            999L, 100, RentChargeSource.StallEscrow, DateTime.UtcNow);

        _shopRepo.Setup(r => r.GetShopById(999L)).Returns((PlayerStall?)null);

        // Act
        CommandResult result = await _handler.HandleAsync(command);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("not found"));
    }

    [Test]
    public async Task HandleAsync_WhenUpdateFails_ReturnsFailure()
    {
        // Arrange
        PayStallRentCommand command = PayStallRentCommand.Create(
            _testStall.Id, 100, RentChargeSource.StallEscrow, DateTime.UtcNow);

        _shopRepo.Setup(r => r.GetShopById(_testStall.Id)).Returns(_testStall);

        _shopRepo.Setup(r => r.UpdateShop(_testStall.Id, It.IsAny<Action<PlayerStall>>()))
            .Returns(false); // Simulate update failure

        // Act
        CommandResult result = await _handler.HandleAsync(command);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("Failed to update"));
    }

    [Test]
    public async Task HandleAsync_WhenUpdateFails_DoesNotPublishEvent()
    {
        // Arrange
        PayStallRentCommand command = PayStallRentCommand.Create(
            _testStall.Id, 100, RentChargeSource.StallEscrow, DateTime.UtcNow);

        _shopRepo.Setup(r => r.GetShopById(_testStall.Id)).Returns(_testStall);

        _shopRepo.Setup(r => r.UpdateShop(_testStall.Id, It.IsAny<Action<PlayerStall>>()))
            .Returns(false);

        // Act
        CommandResult result = await _handler.HandleAsync(command);

        // Assert
        Assert.That(result.Success, Is.False);
        _eventBus.Verify(
            e => e.PublishAsync(It.IsAny<StallRentPaidEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion
}

