using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities.Economy.Shops;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops.PlayerStalls;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops.PlayerStalls.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops.PlayerStalls.Events;
using Moq;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Tests.Shops.PlayerStalls;

/// <summary>
/// Tests for SuspendStallForNonPaymentCommand and its handler.
/// Verifies that stalls are correctly suspended, grace periods are managed,
/// and ownership is released appropriately.
/// </summary>
[TestFixture]
public class SuspendStallForNonPaymentCommandTests
{
    private Mock<IPlayerShopRepository> _shopRepo = null!;
    private Mock<IEventBus> _eventBus = null!;
    private Mock<IPlayerStallInventoryCustodian> _inventoryCustodian = null!;
    private SuspendStallForNonPaymentCommandHandler _handler = null!;
    private PlayerStall _testStall = null!;

    [SetUp]
    public void SetUp()
    {
        _shopRepo = new Mock<IPlayerShopRepository>(MockBehavior.Strict);
        _eventBus = new Mock<IEventBus>(MockBehavior.Strict);
        _inventoryCustodian = new Mock<IPlayerStallInventoryCustodian>(MockBehavior.Strict);

        _testStall = new PlayerStall
        {
            Id = 123L,
            Tag = "test_stall",
            AreaResRef = "test_area",
            SettlementTag = "1",
            OwnerCharacterId = Guid.NewGuid(),
            OwnerPersonaId = Guid.NewGuid().ToString(),
            OwnerPlayerPersonaId = Guid.NewGuid().ToString(),
            OwnerDisplayName = "Test Owner",
            DailyRent = 100,
            EscrowBalance = 50, // Not enough for rent
            NextRentDueUtc = DateTime.UtcNow.AddHours(-1),
            IsActive = true,
            SuspendedUtc = null,
            DeactivatedUtc = null,
            LedgerEntries = new List<PlayerStallLedgerEntry>()
        };

        _handler = new SuspendStallForNonPaymentCommandHandler(
            _shopRepo.Object,
            _eventBus.Object,
            _inventoryCustodian.Object);
    }

    #region Command Creation Tests

    [Test]
    public void Create_WithValidParameters_ReturnsCommand()
    {
        // Arrange & Act
        SuspendStallForNonPaymentCommand command = SuspendStallForNonPaymentCommand.Create(
            stallId: 123L,
            reason: "Insufficient funds",
            timestamp: DateTime.UtcNow,
            gracePeriod: TimeSpan.FromHours(1));

        // Assert
        Assert.That(command, Is.Not.Null);
        Assert.That(command.StallId, Is.EqualTo(123L));
        Assert.That(command.Reason, Is.EqualTo("Insufficient funds"));
        Assert.That(command.GracePeriod, Is.EqualTo(TimeSpan.FromHours(1)));
    }

    [Test]
    public void Create_WithNegativeStallId_ThrowsArgumentException()
    {
        // Arrange, Act & Assert
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            SuspendStallForNonPaymentCommand.Create(
                -1L, "reason", DateTime.UtcNow, TimeSpan.FromHours(1)));

        Assert.That(ex.ParamName, Is.EqualTo("stallId"));
    }

    [Test]
    public void Create_WithEmptyReason_ThrowsArgumentException()
    {
        // Arrange, Act & Assert
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            SuspendStallForNonPaymentCommand.Create(
                123L, "", DateTime.UtcNow, TimeSpan.FromHours(1)));

        Assert.That(ex.ParamName, Is.EqualTo("reason"));
    }

    [Test]
    public void Create_WithNegativeGracePeriod_ThrowsArgumentException()
    {
        // Arrange, Act & Assert
        ArgumentException ex = Assert.Throws<ArgumentException>(() =>
            SuspendStallForNonPaymentCommand.Create(
                123L, "reason", DateTime.UtcNow, TimeSpan.FromHours(-1)));

        Assert.That(ex.ParamName, Is.EqualTo("gracePeriod"));
    }

    #endregion

    #region Handler Tests - First Suspension

    [Test]
    public async Task HandleAsync_WithFirstSuspension_SetsSuspendedUtc()
    {
        // Arrange
        DateTime now = DateTime.UtcNow;
        SuspendStallForNonPaymentCommand command = SuspendStallForNonPaymentCommand.Create(
            _testStall.Id, "Insufficient funds", now, TimeSpan.FromHours(1));

        _shopRepo.Setup(r => r.GetShopById(_testStall.Id)).Returns(_testStall);

        _shopRepo.Setup(r => r.UpdateShop(_testStall.Id, It.IsAny<Action<PlayerStall>>()))
            .Callback<long, Action<PlayerStall>>((id, action) => action(_testStall))
            .Returns(true);

        _eventBus.Setup(e => e.PublishAsync(It.IsAny<StallSuspendedEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        CommandResult result = await _handler.HandleAsync(command);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(_testStall.SuspendedUtc, Is.Not.Null);
        Assert.That(_testStall.SuspendedUtc!.Value, Is.EqualTo(now).Within(TimeSpan.FromSeconds(1)));
    }

    [Test]
    public async Task HandleAsync_WithFirstSuspension_KeepsStallActive()
    {
        // Arrange
        SuspendStallForNonPaymentCommand command = SuspendStallForNonPaymentCommand.Create(
            _testStall.Id, "Insufficient funds", DateTime.UtcNow, TimeSpan.FromHours(1));

        _shopRepo.Setup(r => r.GetShopById(_testStall.Id)).Returns(_testStall);

        _shopRepo.Setup(r => r.UpdateShop(_testStall.Id, It.IsAny<Action<PlayerStall>>()))
            .Callback<long, Action<PlayerStall>>((id, action) => action(_testStall))
            .Returns(true);

        _eventBus.Setup(e => e.PublishAsync(It.IsAny<StallSuspendedEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        CommandResult result = await _handler.HandleAsync(command);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(_testStall.IsActive, Is.True);
    }

    [Test]
    public async Task HandleAsync_WithFirstSuspension_PublishesStallSuspendedEvent()
    {
        // Arrange
        SuspendStallForNonPaymentCommand command = SuspendStallForNonPaymentCommand.Create(
            _testStall.Id, "Insufficient funds", DateTime.UtcNow, TimeSpan.FromHours(1));

        _shopRepo.Setup(r => r.GetShopById(_testStall.Id)).Returns(_testStall);

        _shopRepo.Setup(r => r.UpdateShop(_testStall.Id, It.IsAny<Action<PlayerStall>>()))
            .Callback<long, Action<PlayerStall>>((id, action) => action(_testStall))
            .Returns(true);

        StallSuspendedEvent? capturedEvent = null;
        _eventBus.Setup(e => e.PublishAsync(It.IsAny<StallSuspendedEvent>(), It.IsAny<CancellationToken>()))
            .Callback<StallSuspendedEvent, CancellationToken>((evt, ct) => capturedEvent = evt)
            .Returns(Task.CompletedTask);

        // Act
        CommandResult result = await _handler.HandleAsync(command);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(capturedEvent, Is.Not.Null);
        Assert.That(capturedEvent!.StallId, Is.EqualTo(_testStall.Id));
        Assert.That(capturedEvent.Reason, Is.EqualTo("Insufficient funds"));
        Assert.That(capturedEvent.IsFirstSuspension, Is.True);
    }

    #endregion

    #region Handler Tests - After Grace Period (Ownership Release)

    [Test]
    public async Task HandleAsync_AfterGracePeriod_ReleasesOwnership()
    {
        // Arrange
        _testStall.SuspendedUtc = DateTime.UtcNow.AddHours(-2); // 2 hours ago

        SuspendStallForNonPaymentCommand command = SuspendStallForNonPaymentCommand.Create(
            _testStall.Id, "Grace period expired", DateTime.UtcNow, TimeSpan.FromHours(1));

        _shopRepo.Setup(r => r.GetShopById(_testStall.Id)).Returns(_testStall);

        _shopRepo.Setup(r => r.UpdateShop(_testStall.Id, It.IsAny<Action<PlayerStall>>()))
            .Callback<long, Action<PlayerStall>>((id, action) => action(_testStall))
            .Returns(true);

        _inventoryCustodian.Setup(c => c.TransferInventoryToMarketReeveAsync(_testStall, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _eventBus.Setup(e => e.PublishAsync(It.IsAny<StallOwnershipReleasedEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        CommandResult result = await _handler.HandleAsync(command);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(_testStall.OwnerCharacterId, Is.Null);
        Assert.That(_testStall.OwnerPersonaId, Is.Null);
        Assert.That(_testStall.OwnerPlayerPersonaId, Is.Null);
        Assert.That(_testStall.OwnerDisplayName, Is.Null);
    }

    [Test]
    public async Task HandleAsync_AfterGracePeriod_DeactivatesStall()
    {
        // Arrange
        _testStall.SuspendedUtc = DateTime.UtcNow.AddHours(-2);

        SuspendStallForNonPaymentCommand command = SuspendStallForNonPaymentCommand.Create(
            _testStall.Id, "Grace period expired", DateTime.UtcNow, TimeSpan.FromHours(1));

        _shopRepo.Setup(r => r.GetShopById(_testStall.Id)).Returns(_testStall);

        _shopRepo.Setup(r => r.UpdateShop(_testStall.Id, It.IsAny<Action<PlayerStall>>()))
            .Callback<long, Action<PlayerStall>>((id, action) => action(_testStall))
            .Returns(true);

        _inventoryCustodian.Setup(c => c.TransferInventoryToMarketReeveAsync(_testStall, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _eventBus.Setup(e => e.PublishAsync(It.IsAny<StallOwnershipReleasedEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        CommandResult result = await _handler.HandleAsync(command);

        // Assert
        Assert.That(result.Success, Is.True);
        Assert.That(_testStall.IsActive, Is.False);
        Assert.That(_testStall.DeactivatedUtc, Is.Not.Null);
    }

    #endregion

    #region Handler Tests - Error Cases

    [Test]
    public async Task HandleAsync_WhenStallNotFound_ReturnsFailure()
    {
        // Arrange
        SuspendStallForNonPaymentCommand command = SuspendStallForNonPaymentCommand.Create(
            999L, "Not found", DateTime.UtcNow, TimeSpan.FromHours(1));

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
        SuspendStallForNonPaymentCommand command = SuspendStallForNonPaymentCommand.Create(
            _testStall.Id, "Insufficient funds", DateTime.UtcNow, TimeSpan.FromHours(1));

        _shopRepo.Setup(r => r.GetShopById(_testStall.Id)).Returns(_testStall);

        _shopRepo.Setup(r => r.UpdateShop(_testStall.Id, It.IsAny<Action<PlayerStall>>()))
            .Returns(false); // Simulate update failure

        // Act
        CommandResult result = await _handler.HandleAsync(command);

        // Assert
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("Failed to update"));
    }

    #endregion
}

