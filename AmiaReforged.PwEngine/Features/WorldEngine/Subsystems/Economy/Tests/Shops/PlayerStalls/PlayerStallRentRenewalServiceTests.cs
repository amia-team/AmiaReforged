using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities.Economy.Shops;
using AmiaReforged.PwEngine.Database.Entities.Economy.Treasuries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops.PlayerStalls;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops.PlayerStalls.Commands;
using Anvil.API;
using Moq;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Tests.Shops.PlayerStalls;

[TestFixture]
public class PlayerStallRentRenewalServiceTests
{
    private Mock<IPlayerShopRepository> _shops = null!;
    private Mock<IWorldEngineFacade> _worldEngine = null!;
    private Mock<IPlayerStallOwnerNotifier> _notifier = null!;
    private Mock<IPlayerStallEventBroadcaster> _events = null!;
    private Mock<IPlayerStallInventoryCustodian> _custodian = null!;
    private Mock<ICoinhouseRepository> _coinhouses = null!;
    private PlayerStallRentRenewalService _service = null!;
    private PlayerStall _stall = null!;
    private List<PlayerStall> _allShops = null!;
    private List<(Guid? Owner, string Message, Color Color)> _capturedNotifications = null!;

    [SetUp]
    public void SetUp()
    {
        _stall = CreateStall();
        _allShops = new List<PlayerStall> { _stall };
        _capturedNotifications = new List<(Guid?, string, Color)>();

        _shops = new Mock<IPlayerShopRepository>(MockBehavior.Strict);
        _shops.Setup(r => r.AllShops()).Returns(() => new List<PlayerStall>(_allShops));
        _shops.Setup(r => r.GetShopById(It.IsAny<long>()))
            .Returns<long>(id => id == _stall.Id ? _stall : null);
        _shops.Setup(r => r.ProductsForShop(It.IsAny<long>()))
            .Returns<long>(id => id == _stall.Id ? _stall.Inventory : null);
        _shops.Setup(r => r.UpdateShop(It.IsAny<long>(), It.IsAny<Action<PlayerStall>>()))
            .Returns<long, Action<PlayerStall>>((id, action) =>
            {
                if (id != _stall.Id)
                {
                    return false;
                }

                action(_stall);
                return true;
            });

        _notifier = new Mock<IPlayerStallOwnerNotifier>(MockBehavior.Strict);
        _notifier
            .Setup(n => n.NotifyAsync(It.IsAny<Guid?>(), It.IsAny<string>(), It.IsAny<Color>()))
            .Returns<Guid?, string, Color>((owner, message, color) =>
            {
                _capturedNotifications.Add((owner, message, color));
                return Task.CompletedTask;
            });

        _events = new Mock<IPlayerStallEventBroadcaster>(MockBehavior.Strict);
        _events.Setup(e => e.BroadcastSellerRefreshAsync(It.IsAny<long>()))
            .Returns(Task.CompletedTask);

        _custodian = new Mock<IPlayerStallInventoryCustodian>(MockBehavior.Strict);
        _custodian.Setup(c =>
                c.TransferInventoryToMarketReeveAsync(It.IsAny<PlayerStall>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _coinhouses = new Mock<ICoinhouseRepository>(MockBehavior.Strict);
        _coinhouses.Setup(c => c.GetSettlementCoinhouse(It.IsAny<SettlementId>()))
            .Returns((CoinHouse?)null);

        _worldEngine = new Mock<IWorldEngineFacade>(MockBehavior.Strict);
        _worldEngine.Setup(w => w.ExecuteAsync(It.IsAny<WithdrawGoldCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult.Ok());
        _worldEngine.Setup(w => w.ExecuteAsync(It.IsAny<DepositGoldCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult.Ok());

        // Set up PayStallRentCommand to actually modify the stall
        _worldEngine.Setup(w => w.ExecuteAsync(It.IsAny<PayStallRentCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PayStallRentCommand cmd, CancellationToken ct) =>
            {
                // Simulate what the handler does
                _shops.Object.UpdateShop(cmd.StallId, entity =>
                {
                    if (cmd.Source == RentChargeSource.StallEscrow)
                    {
                        entity.EscrowBalance = Math.Max(0, entity.EscrowBalance - cmd.RentAmount);
                    }
                    if (cmd.RentAmount > 0)
                    {
                        entity.LifetimeNetEarnings -= cmd.RentAmount;
                        entity.LedgerEntries.Add(new PlayerStallLedgerEntry
                        {
                            StallId = entity.Id,
                            EntryType = PlayerStallLedgerEntryType.RentPayment,
                            Amount = -cmd.RentAmount,
                            Description = $"Rent payment: {cmd.RentAmount} gp",
                            OccurredUtc = cmd.PaymentTimestamp
                        });
                    }
                    entity.LastRentPaidUtc = cmd.PaymentTimestamp;
                    entity.NextRentDueUtc = entity.NextRentDueUtc.AddDays(1);
                    entity.SuspendedUtc = null;
                    entity.DeactivatedUtc = null;
                    entity.IsActive = true;
                });
                return CommandResult.Ok();
            });

        // Set up SuspendStallForNonPaymentCommand to actually modify the stall
        _worldEngine.Setup(w => w.ExecuteAsync(It.IsAny<SuspendStallForNonPaymentCommand>(), It.IsAny<CancellationToken>()))
            .Returns<SuspendStallForNonPaymentCommand, CancellationToken>(async (cmd, ct) =>
            {
                bool shouldReleaseOwnership = false;

                // Simulate what the handler does
                _shops.Object.UpdateShop(cmd.StallId, entity =>
                {
                    if (entity.SuspendedUtc == null)
                    {
                        // First suspension - start grace period
                        entity.SuspendedUtc = cmd.SuspensionTimestamp;
                        entity.IsActive = true;
                        entity.NextRentDueUtc = cmd.SuspensionTimestamp.Add(cmd.GracePeriod);
                    }
                    else if (cmd.SuspensionTimestamp >= entity.SuspendedUtc.Value.Add(cmd.GracePeriod))
                    {
                        // Grace period expired - release ownership
                        shouldReleaseOwnership = true;
                        entity.OwnerCharacterId = null;
                        entity.OwnerPersonaId = null;
                        entity.OwnerPlayerPersonaId = null;
                        entity.OwnerDisplayName = null;
                        entity.CoinHouseAccountId = null;
                        entity.HoldEarningsInStall = false;
                        entity.IsActive = false;
                        entity.DeactivatedUtc ??= cmd.SuspensionTimestamp;
                        entity.NextRentDueUtc = cmd.SuspensionTimestamp + TimeSpan.FromHours(1);
                    }
                    else
                    {
                        // Still in grace period
                        entity.IsActive = true;
                        entity.NextRentDueUtc = entity.SuspendedUtc.Value.Add(cmd.GracePeriod);
                    }
                });

                // Transfer inventory when releasing ownership
                if (shouldReleaseOwnership)
                {
                    await _custodian.Object.TransferInventoryToMarketReeveAsync(_stall, ct);
                }

                return CommandResult.Ok();
            });

        _service = new PlayerStallRentRenewalService(
            _shops.Object,
            _worldEngine.Object,
            _notifier.Object,
            _events.Object,
            _custodian.Object,
            _coinhouses.Object
        );
    }

    [TearDown]
    public void TearDown()
    {
        _service.Dispose();
    }

    [Test]
    public async Task RunSingleCycleAsync_WhenRentFails_StartsGracePeriod()
    {
        _stall.NextRentDueUtc = DateTime.UtcNow.AddMinutes(-10);
        _stall.EscrowBalance = 0;
        _stall.CoinHouseAccountId = null;

        await _service.RunSingleCycleAsync(CancellationToken.None);

        Assert.That(_stall.SuspendedUtc, Is.Not.Null);
        Assert.That(_stall.IsActive, Is.True);
        Assert.That(_stall.NextRentDueUtc, Is.GreaterThan(DateTime.UtcNow));
        Assert.That(_capturedNotifications, Has.Count.EqualTo(1));
        (Guid? owner, string message, Color color) = _capturedNotifications[0];
        Assert.That(owner, Is.EqualTo(_stall.OwnerCharacterId));
        Assert.That(color, Is.EqualTo(ColorConstants.Yellow));
        StringAssert.Contains("will suspend", message);
        _events.Verify(e => e.BroadcastSellerRefreshAsync(_stall.Id), Times.Once);
        _custodian.Verify(
            c => c.TransferInventoryToMarketReeveAsync(It.IsAny<PlayerStall>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task RunSingleCycleAsync_WhenGraceExpired_SuspendsStall()
    {
        DateTime pastDue = DateTime.UtcNow.AddHours(-2);
        _stall.SuspendedUtc = pastDue;
        _stall.NextRentDueUtc = DateTime.UtcNow.AddMinutes(-5);
        _stall.DeactivatedUtc = null;
        _stall.IsActive = true;
        _stall.EscrowBalance = 0;
        _stall.CoinHouseAccountId = null;
        string originalOwnerPersonaId = _stall.OwnerPersonaId!;

        await _service.RunSingleCycleAsync(CancellationToken.None);

        Assert.That(_stall.IsActive, Is.False);
        Assert.That(_stall.DeactivatedUtc, Is.Not.Null);
        Assert.That(_stall.NextRentDueUtc, Is.GreaterThan(DateTime.UtcNow));
        // Verify ownership is cleared so stall can be claimed by others
        Assert.That(_stall.OwnerCharacterId, Is.Null);
        Assert.That(_stall.OwnerPersonaId, Is.Null);
        Assert.That(_stall.OwnerPlayerPersonaId, Is.Null);
        Assert.That(_stall.OwnerDisplayName, Is.Null);
        Assert.That(_stall.CoinHouseAccountId, Is.Null);
        Assert.That(_stall.HoldEarningsInStall, Is.False);
        Assert.That(_capturedNotifications, Has.Count.EqualTo(1));
        Assert.That(_capturedNotifications[0].Color, Is.EqualTo(ColorConstants.Red));
        StringAssert.Contains("now suspended", _capturedNotifications[0].Message);
        StringAssert.Contains("market reeve", _capturedNotifications[0].Message);
        StringAssert.Contains(originalOwnerPersonaId, _capturedNotifications[0].Message);
        _custodian.Verify(c => c.TransferInventoryToMarketReeveAsync(_stall, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task RunSingleCycleAsync_WhenRentSucceeds_ClearsSuspension()
    {
        Guid ownerGuid = _stall.OwnerCharacterId!.Value;
        _stall.SuspendedUtc = DateTime.UtcNow.AddHours(-2);
        _stall.DeactivatedUtc = DateTime.UtcNow.AddHours(-1);
        _stall.IsActive = false;
        _stall.CoinHouseAccountId = Guid.NewGuid();
        _stall.SettlementTag = "market_square";
        _stall.OwnerPersonaId = $"Character:{ownerGuid}";
        _stall.NextRentDueUtc = DateTime.UtcNow.AddMinutes(-5);
        _stall.EscrowBalance = 500;
        _stall.LifetimeNetEarnings = 1000;

        await _service.RunSingleCycleAsync(CancellationToken.None);

        Assert.That(_stall.SuspendedUtc, Is.Null);
        Assert.That(_stall.DeactivatedUtc, Is.Null);
        Assert.That(_stall.IsActive, Is.True);
        Assert.That(_stall.LastRentPaidUtc, Is.Not.Null);
        Assert.That(_stall.NextRentDueUtc, Is.GreaterThan(DateTime.UtcNow));
        Assert.That(_stall.LifetimeNetEarnings, Is.EqualTo(1000 - _stall.DailyRent));
        Assert.That(_stall.LedgerEntries, Has.Count.GreaterThanOrEqualTo(1));
        Assert.That(_capturedNotifications, Has.Count.EqualTo(1));
        Assert.That(_capturedNotifications[0].Color, Is.EqualTo(ColorConstants.Orange));
        StringAssert.Contains("Rent of", _capturedNotifications[0].Message);
        _worldEngine.Verify(w => w.ExecuteAsync(It.IsAny<PayStallRentCommand>(), It.IsAny<CancellationToken>()),
            Times.Once);
        _custodian.Verify(
            c => c.TransferInventoryToMarketReeveAsync(It.IsAny<PlayerStall>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task RunSingleCycleAsync_WhenStallEmptyFor2Hours_ReleasesStallWithRefund()
    {
        // Set up a stall that has been empty for 2+ hours
        _stall.UpdatedUtc = DateTime.UtcNow.AddHours(-3);
        _stall.Inventory.Clear(); // Empty inventory
        _stall.NextRentDueUtc = DateTime.UtcNow.AddHours(20); // 20 hours until next rent
        _stall.CoinHouseAccountId = Guid.NewGuid();
        _stall.SettlementTag = "market_square";
        Guid originalOwner = _stall.OwnerCharacterId!.Value;
        string originalPersona = _stall.OwnerPersonaId!;

        await _service.RunSingleCycleAsync(CancellationToken.None);

        // Verify stall was released
        Assert.That(_stall.OwnerCharacterId, Is.Null);
        Assert.That(_stall.OwnerPersonaId, Is.Null);
        Assert.That(_stall.OwnerPlayerPersonaId, Is.Null);
        Assert.That(_stall.OwnerDisplayName, Is.Null);
        Assert.That(_stall.CoinHouseAccountId, Is.Null);
        Assert.That(_stall.HoldEarningsInStall, Is.False);
        Assert.That(_stall.IsActive, Is.False);
        Assert.That(_stall.DeactivatedUtc, Is.Not.Null);
        Assert.That(_stall.SuspendedUtc, Is.Not.Null);

        // Verify notification was sent
        Assert.That(_capturedNotifications, Has.Count.EqualTo(1));
        Assert.That(_capturedNotifications[0].Color, Is.EqualTo(ColorConstants.Orange));
        _worldEngine.Verify(w => w.ExecuteAsync(It.IsAny<DepositGoldCommand>(), It.IsAny<CancellationToken>()),
            Times.Once);
        StringAssert.Contains("prorated refund", _capturedNotifications[0].Message);

        // Verify refund was attempted to coinhouse
        _worldEngine.Verify(w => w.ExecuteAsync(It.IsAny<DepositGoldCommand>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task RunSingleCycleAsync_WhenStallHasInventory_DoesNotRelease()
    {
        // Set up a stall that has been empty for 2+ hours but has inventory
        _stall.UpdatedUtc = DateTime.UtcNow.AddHours(-3);
        _stall.Inventory.Add(new StallProduct
        {
            Id = 1,
            StallId = _stall.Id,
            ResRef = "test_item",
            Name = "Test Item",
            Price = 100,
            Quantity = 1,
            ItemData = new byte[] { 1, 2, 3 }
        });
        _stall.NextRentDueUtc = DateTime.UtcNow.AddHours(20);

        await _service.RunSingleCycleAsync(CancellationToken.None);

        // Verify stall was NOT released
        Assert.That(_stall.OwnerCharacterId, Is.Not.Null);
        Assert.That(_stall.IsActive, Is.True);
    }

    [Test]
    public async Task RunSingleCycleAsync_WhenStallEmptyButNotLongEnough_DoesNotRelease()
    {
        // Set up a stall that has been empty for less than 2 hours
        _stall.UpdatedUtc = DateTime.UtcNow.AddMinutes(-30);
        _stall.Inventory.Clear();
        _stall.NextRentDueUtc = DateTime.UtcNow.AddHours(20);

        await _service.RunSingleCycleAsync(CancellationToken.None);

        // Verify stall was NOT released
        Assert.That(_stall.OwnerCharacterId, Is.Not.Null);
        Assert.That(_stall.IsActive, Is.True);
    }

    private static PlayerStall CreateStall()
    {
        Guid ownerGuid = Guid.NewGuid();
        return new PlayerStall
        {
            Id = 77,
            Tag = "stall_tag",
            AreaResRef = "ar_market",
            SettlementTag = "market_square",
            OwnerCharacterId = ownerGuid,
            OwnerPersonaId = $"Character:{ownerGuid}",
            OwnerDisplayName = "Test Owner",
            DailyRent = 250,
            LeaseStartUtc = DateTime.UtcNow.AddDays(-3),
            NextRentDueUtc = DateTime.UtcNow.AddMinutes(-5),
            LastRentPaidUtc = DateTime.UtcNow.AddDays(-1),
            EscrowBalance = 0,
            LifetimeNetEarnings = 0,
            UpdatedUtc = DateTime.UtcNow,
            LedgerEntries = new List<PlayerStallLedgerEntry>(),
            Inventory = new List<StallProduct>(),
            Members = new List<PlayerStallMember>(),
            Transactions = new List<StallTransaction>(),
            IsActive = true
        };
    }

    #region Rent Deposit Integration Tests

    [Test]
    public async Task RunSingleCycleAsync_WhenEscrowHasDeposit_PaysRentFromEscrow()
    {
        // Stall has deposit in escrow but no Coinhouse account
        _stall.EscrowBalance = 5000; // Includes deposit
        _stall.DailyRent = 1000;
        _stall.CoinHouseAccountId = null;
        _stall.NextRentDueUtc = DateTime.UtcNow.AddMinutes(-5);
        int initialBalance = _stall.EscrowBalance;

        await _service.RunSingleCycleAsync(CancellationToken.None);

        // Verify rent was paid from escrow
        Assert.That(_stall.EscrowBalance, Is.EqualTo(initialBalance - _stall.DailyRent));
        Assert.That(_stall.IsActive, Is.True);
        Assert.That(_stall.SuspendedUtc, Is.Null);
        Assert.That(_stall.LastRentPaidUtc, Is.Not.Null);

        // Verify PayStallRentCommand was called with escrow source
        _worldEngine.Verify(w => w.ExecuteAsync(
            It.Is<PayStallRentCommand>(cmd =>
                cmd.StallId == _stall.Id &&
                cmd.Source == RentChargeSource.StallEscrow &&
                cmd.RentAmount == _stall.DailyRent),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task RunSingleCycleAsync_WhenBothCoinhouseAndEscrow_PrefersCoinhouse()
    {
        // Stall has both Coinhouse account and escrow balance
        _stall.CoinHouseAccountId = Guid.NewGuid();
        _stall.EscrowBalance = 5000;
        _stall.DailyRent = 1000;
        _stall.SettlementTag = "market_square";
        _stall.NextRentDueUtc = DateTime.UtcNow.AddMinutes(-5);
        int initialEscrowBalance = _stall.EscrowBalance;

        await _service.RunSingleCycleAsync(CancellationToken.None);

        // Verify rent was paid from Coinhouse, not escrow
        Assert.That(_stall.EscrowBalance, Is.EqualTo(initialEscrowBalance),
            "Escrow should not be touched when Coinhouse is available");
        Assert.That(_stall.IsActive, Is.True);
        Assert.That(_stall.SuspendedUtc, Is.Null);

        // Verify withdraw from Coinhouse was attempted (not escrow)
        _worldEngine.Verify(w => w.ExecuteAsync(
            It.Is<WithdrawGoldCommand>(cmd => cmd.Amount.Value == _stall.DailyRent),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task RunSingleCycleAsync_WhenOnlyPartialEscrowDeposit_FailsAndStartsGracePeriod()
    {
        // Escrow has a deposit but not enough to cover rent
        _stall.EscrowBalance = 500; // Not enough for 1000 gp rent
        _stall.DailyRent = 1000;
        _stall.CoinHouseAccountId = null;
        _stall.NextRentDueUtc = DateTime.UtcNow.AddMinutes(-5);
        int initialBalance = _stall.EscrowBalance;

        await _service.RunSingleCycleAsync(CancellationToken.None);

        // Verify rent payment failed and grace period started
        Assert.That(_stall.EscrowBalance, Is.EqualTo(initialBalance),
            "Escrow should not be touched for insufficient partial payment");
        Assert.That(_stall.SuspendedUtc, Is.Not.Null);
        Assert.That(_stall.IsActive, Is.True,
            "Stall should remain active during grace period");

        // Verify warning notification was sent
        Assert.That(_capturedNotifications, Has.Count.EqualTo(1));
        Assert.That(_capturedNotifications[0].Color, Is.EqualTo(ColorConstants.Yellow));
    }

    [Test]
    public async Task RunSingleCycleAsync_WhenMultipleDeposits_AccumulatesForRentPayment()
    {
        // Multiple deposits have accumulated in escrow
        _stall.EscrowBalance = 15000; // From multiple deposits
        _stall.DailyRent = 2500;
        _stall.CoinHouseAccountId = null;
        _stall.NextRentDueUtc = DateTime.UtcNow.AddMinutes(-5);

        // Pay rent multiple times to simulate multiple cycles
        await _service.RunSingleCycleAsync(CancellationToken.None);
        Assert.That(_stall.EscrowBalance, Is.EqualTo(12500));

        _stall.NextRentDueUtc = DateTime.UtcNow.AddMinutes(-5);
        await _service.RunSingleCycleAsync(CancellationToken.None);
        Assert.That(_stall.EscrowBalance, Is.EqualTo(10000));

        _stall.NextRentDueUtc = DateTime.UtcNow.AddMinutes(-5);
        await _service.RunSingleCycleAsync(CancellationToken.None);
        Assert.That(_stall.EscrowBalance, Is.EqualTo(7500));

        // All payments should have succeeded
        Assert.That(_stall.IsActive, Is.True);
        Assert.That(_stall.SuspendedUtc, Is.Null);
        _worldEngine.Verify(w => w.ExecuteAsync(
            It.IsAny<PayStallRentCommand>(),
            It.IsAny<CancellationToken>()),
            Times.Exactly(3));
    }

    [Test]
    public async Task RunSingleCycleAsync_WhenDepositExactlyCoversRent_PaysAndEscrowBecomesZero()
    {
        // Escrow has exactly enough to cover one rent payment
        _stall.EscrowBalance = 1000;
        _stall.DailyRent = 1000;
        _stall.CoinHouseAccountId = null;
        _stall.NextRentDueUtc = DateTime.UtcNow.AddMinutes(-5);

        await _service.RunSingleCycleAsync(CancellationToken.None);

        // Verify exact payment
        Assert.That(_stall.EscrowBalance, Is.EqualTo(0));
        Assert.That(_stall.IsActive, Is.True);
        Assert.That(_stall.SuspendedUtc, Is.Null);

        _worldEngine.Verify(w => w.ExecuteAsync(
            It.Is<PayStallRentCommand>(cmd =>
                cmd.Source == RentChargeSource.StallEscrow &&
                cmd.RentAmount == 1000),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task RunSingleCycleAsync_WhenDepositAndSales_CombinedEscrowPaysRent()
    {
        // Escrow contains both deposits and sales earnings
        _stall.EscrowBalance = 8000; // Mixed from sales and deposits
        _stall.DailyRent = 2000;
        _stall.CoinHouseAccountId = null;
        _stall.HoldEarningsInStall = true; // Sales go to escrow
        _stall.NextRentDueUtc = DateTime.UtcNow.AddMinutes(-5);

        await _service.RunSingleCycleAsync(CancellationToken.None);

        // Verify rent paid from combined escrow (doesn't matter the source)
        Assert.That(_stall.EscrowBalance, Is.EqualTo(6000));
        Assert.That(_stall.IsActive, Is.True);
        Assert.That(_stall.SuspendedUtc, Is.Null);
    }

    [Test]
    public async Task RunSingleCycleAsync_WhenCoinhouseFailsButEscrowAvailable_FallsBackToEscrow()
    {
        // Coinhouse withdrawal fails, but escrow has funds
        _stall.CoinHouseAccountId = Guid.NewGuid();
        _stall.EscrowBalance = 5000;
        _stall.DailyRent = 1000;
        _stall.SettlementTag = "market_square";
        _stall.NextRentDueUtc = DateTime.UtcNow.AddMinutes(-5);

        // Simulate Coinhouse withdrawal failure
        _worldEngine
            .Setup(w => w.ExecuteAsync(It.IsAny<WithdrawGoldCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult.Fail("Insufficient funds in Coinhouse"));

        await _service.RunSingleCycleAsync(CancellationToken.None);

        // Verify fallback to escrow
        Assert.That(_stall.EscrowBalance, Is.EqualTo(4000),
            "Should have fallen back to escrow after Coinhouse failure");
        Assert.That(_stall.IsActive, Is.True);
        Assert.That(_stall.SuspendedUtc, Is.Null);

        // Verify both attempts were made
        _worldEngine.Verify(w => w.ExecuteAsync(
            It.IsAny<WithdrawGoldCommand>(),
            It.IsAny<CancellationToken>()),
            Times.Once);
        _worldEngine.Verify(w => w.ExecuteAsync(
            It.Is<PayStallRentCommand>(cmd => cmd.Source == RentChargeSource.StallEscrow),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion
}
