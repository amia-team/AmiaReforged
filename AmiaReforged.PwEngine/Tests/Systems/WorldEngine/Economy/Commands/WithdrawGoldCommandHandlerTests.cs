using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities.Economy.Treasuries;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Transactions;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Tests.Helpers.WorldEngine;
using Moq;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.Economy.Commands;

/// <summary>
/// BDD-style tests for WithdrawGoldCommandHandler.
/// Tests the handler's ability to withdraw gold from a coinhouse account.
/// </summary>
[TestFixture]
public class WithdrawGoldCommandHandlerTests
{
    private Mock<ICoinhouseRepository> _mockCoinhouseRepo = null!;
    private Mock<ITransactionRepository> _mockTransactionRepo = null!;
    private Mock<IEventBus> _mockEventBus = null!;
    private WithdrawGoldCommandHandler _handler = null!;

    private PersonaId _withdrawer;
    private CoinhouseTag _coinhouse;
    private CoinHouse _testCoinhouse = null!;
    private CoinHouseAccount _testAccount = null!;

    [SetUp]
    public void Setup()
    {
        _mockCoinhouseRepo = new Mock<ICoinhouseRepository>();
        _mockTransactionRepo = new Mock<ITransactionRepository>();
        _mockEventBus = new Mock<IEventBus>();
        _handler = new WithdrawGoldCommandHandler(
            _mockCoinhouseRepo.Object,
            _mockTransactionRepo.Object,
            _mockEventBus.Object);

        _withdrawer = PersonaTestHelpers.CreateCharacterPersona().Id;
        _coinhouse = EconomyTestHelpers.CreateCoinhouseTag("cordor_bank");

        // Setup test coinhouse with account
        _testCoinhouse = new CoinHouse
        {
            Id = 1,
            Tag = _coinhouse.Value,
            Settlement = 1,
            EngineId = Guid.NewGuid(),
            StoredGold = 0,
            Accounts = new List<CoinHouseAccount>()
        };

        _testAccount = new CoinHouseAccount
        {
            Id = Guid.NewGuid(),
            Debit = 1000, // Start with 1000 gold in account
            Credit = 0,
            CoinHouseId = _testCoinhouse.Id,
            OpenedAt = DateTime.UtcNow
        };

        _testCoinhouse.Accounts!.Add(_testAccount);
    }

    #region Happy Path Tests

    [Test]
    public async Task Given_ValidWithdrawal_When_HandlingCommand_Then_ReturnsSuccess()
    {
        // Given
        var command = WithdrawGoldCommand.Create(_withdrawer, _coinhouse, 500, "Test withdrawal");

        _mockCoinhouseRepo.Setup(r => r.GetByTag(_coinhouse)).Returns(_testCoinhouse);
        _mockCoinhouseRepo.Setup(r => r.GetAccountFor(It.IsAny<Guid>())).Returns(_testAccount);
        _mockTransactionRepo
            .Setup(r => r.RecordTransactionAsync(It.IsAny<Database.Entities.Economy.Transaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Database.Entities.Economy.Transaction t, CancellationToken _) =>
            {
                t.Id = 123;
                return t;
            });

        // When
        var result = await _handler.HandleAsync(command);

        // Then
        Assert.That(result.Success, Is.True);
        Assert.That(result.ErrorMessage, Is.Null);
    }

    [Test]
    public async Task Given_ValidWithdrawal_When_HandlingCommand_Then_DecreasesAccountBalance()
    {
        // Given
        var initialBalance = _testAccount.Debit;
        var withdrawAmount = 500;
        var command = WithdrawGoldCommand.Create(_withdrawer, _coinhouse, withdrawAmount, "Test withdrawal");

        _mockCoinhouseRepo.Setup(r => r.GetByTag(_coinhouse)).Returns(_testCoinhouse);
        _mockCoinhouseRepo.Setup(r => r.GetAccountFor(It.IsAny<Guid>())).Returns(_testAccount);
        _mockTransactionRepo
            .Setup(r => r.RecordTransactionAsync(It.IsAny<Database.Entities.Economy.Transaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Database.Entities.Economy.Transaction t, CancellationToken _) =>
            {
                t.Id = 123;
                return t;
            });

        // When
        await _handler.HandleAsync(command);

        // Then
        Assert.That(_testAccount.Debit, Is.EqualTo(initialBalance - withdrawAmount));
    }

    [Test]
    public async Task Given_ValidWithdrawal_When_HandlingCommand_Then_PublishesGoldWithdrawnEvent()
    {
        // Given
        var command = WithdrawGoldCommand.Create(_withdrawer, _coinhouse, 500, "Test withdrawal");

        _mockCoinhouseRepo.Setup(r => r.GetByTag(_coinhouse)).Returns(_testCoinhouse);
        _mockCoinhouseRepo.Setup(r => r.GetAccountFor(It.IsAny<Guid>())).Returns(_testAccount);
        _mockTransactionRepo
            .Setup(r => r.RecordTransactionAsync(It.IsAny<Database.Entities.Economy.Transaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Database.Entities.Economy.Transaction t, CancellationToken _) =>
            {
                t.Id = 123;
                return t;
            });

        // When
        await _handler.HandleAsync(command);

        // Then
        _mockEventBus.Verify(bus =>
            bus.PublishAsync(
                It.Is<GoldWithdrawnEvent>(e =>
                    e.Withdrawer == _withdrawer &&
                    e.Coinhouse == _coinhouse &&
                    e.Amount.Value == 500),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task Given_ValidWithdrawal_When_HandlingCommand_Then_RecordsTransaction()
    {
        // Given
        var command = WithdrawGoldCommand.Create(_withdrawer, _coinhouse, 500, "Test withdrawal");

        _mockCoinhouseRepo.Setup(r => r.GetByTag(_coinhouse)).Returns(_testCoinhouse);
        _mockCoinhouseRepo.Setup(r => r.GetAccountFor(It.IsAny<Guid>())).Returns(_testAccount);
        _mockTransactionRepo
            .Setup(r => r.RecordTransactionAsync(It.IsAny<Database.Entities.Economy.Transaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Database.Entities.Economy.Transaction t, CancellationToken _) =>
            {
                t.Id = 123;
                return t;
            });

        // When
        await _handler.HandleAsync(command);

        // Then
        _mockTransactionRepo.Verify(repo =>
            repo.RecordTransactionAsync(
                It.Is<Database.Entities.Economy.Transaction>(t =>
                    t.FromPersonaId == _testCoinhouse.PersonaId.ToString() &&
                    t.ToPersonaId == _withdrawer.ToString() &&
                    t.Amount == 500 &&
                    t.Memo!.Contains("Test withdrawal")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task Given_ExactBalanceWithdrawal_When_HandlingCommand_Then_BalanceBecomesZero()
    {
        // Given
        var exactBalance = _testAccount.Debit;
        var command = WithdrawGoldCommand.Create(_withdrawer, _coinhouse, exactBalance, "Withdraw all");

        _mockCoinhouseRepo.Setup(r => r.GetByTag(_coinhouse)).Returns(_testCoinhouse);
        _mockCoinhouseRepo.Setup(r => r.GetAccountFor(It.IsAny<Guid>())).Returns(_testAccount);
        _mockTransactionRepo
            .Setup(r => r.RecordTransactionAsync(It.IsAny<Database.Entities.Economy.Transaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Database.Entities.Economy.Transaction t, CancellationToken _) =>
            {
                t.Id = 123;
                return t;
            });

        // When
        var result = await _handler.HandleAsync(command);

        // Then
        Assert.That(result.Success, Is.True);
        Assert.That(_testAccount.Debit, Is.EqualTo(0));
    }

    #endregion

    #region Validation Tests - Balance Checks

    [Test]
    public async Task Given_InsufficientBalance_When_HandlingCommand_Then_ReturnsFailure()
    {
        // Given
        _testAccount.Debit = 100; // Only 100 gold
        var command = WithdrawGoldCommand.Create(_withdrawer, _coinhouse, 500, "Overdraft attempt");

        _mockCoinhouseRepo.Setup(r => r.GetByTag(_coinhouse)).Returns(_testCoinhouse);
        _mockCoinhouseRepo.Setup(r => r.GetAccountFor(It.IsAny<Guid>())).Returns(_testAccount);

        // When
        var result = await _handler.HandleAsync(command);

        // Then
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("Insufficient balance"));
        Assert.That(result.ErrorMessage, Does.Contain("100")); // Current balance
        Assert.That(result.ErrorMessage, Does.Contain("500")); // Requested amount
    }

    [Test]
    public async Task Given_InsufficientBalance_When_HandlingCommand_Then_DoesNotModifyBalance()
    {
        // Given
        _testAccount.Debit = 100;
        var initialBalance = _testAccount.Debit;
        var command = WithdrawGoldCommand.Create(_withdrawer, _coinhouse, 500, "Overdraft attempt");

        _mockCoinhouseRepo.Setup(r => r.GetByTag(_coinhouse)).Returns(_testCoinhouse);
        _mockCoinhouseRepo.Setup(r => r.GetAccountFor(It.IsAny<Guid>())).Returns(_testAccount);

        // When
        await _handler.HandleAsync(command);

        // Then
        Assert.That(_testAccount.Debit, Is.EqualTo(initialBalance), "Balance should not change on failed withdrawal");
    }

    [Test]
    public async Task Given_InsufficientBalance_When_HandlingCommand_Then_DoesNotPublishEvent()
    {
        // Given
        _testAccount.Debit = 100;
        var command = WithdrawGoldCommand.Create(_withdrawer, _coinhouse, 500, "Overdraft attempt");

        _mockCoinhouseRepo.Setup(r => r.GetByTag(_coinhouse)).Returns(_testCoinhouse);
        _mockCoinhouseRepo.Setup(r => r.GetAccountFor(It.IsAny<Guid>())).Returns(_testAccount);

        // When
        await _handler.HandleAsync(command);

        // Then
        _mockEventBus.Verify(bus =>
            bus.PublishAsync(It.IsAny<GoldWithdrawnEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task Given_InsufficientBalance_When_HandlingCommand_Then_DoesNotRecordTransaction()
    {
        // Given
        _testAccount.Debit = 100;
        var command = WithdrawGoldCommand.Create(_withdrawer, _coinhouse, 500, "Overdraft attempt");

        _mockCoinhouseRepo.Setup(r => r.GetByTag(_coinhouse)).Returns(_testCoinhouse);
        _mockCoinhouseRepo.Setup(r => r.GetAccountFor(It.IsAny<Guid>())).Returns(_testAccount);

        // When
        await _handler.HandleAsync(command);

        // Then
        _mockTransactionRepo.Verify(repo =>
            repo.RecordTransactionAsync(It.IsAny<Database.Entities.Economy.Transaction>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Validation Tests - Other

    [Test]
    public async Task Given_NonexistentCoinhouse_When_HandlingCommand_Then_ReturnsFailure()
    {
        // Given
        var command = WithdrawGoldCommand.Create(_withdrawer, _coinhouse, 500, "Test withdrawal");

        _mockCoinhouseRepo.Setup(r => r.GetByTag(_coinhouse)).Returns((CoinHouse?)null);

        // When
        var result = await _handler.HandleAsync(command);

        // Then
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("Coinhouse"));
        Assert.That(result.ErrorMessage, Does.Contain("not found"));
    }

    [Test]
    public async Task Given_NoAccount_When_HandlingCommand_Then_ReturnsFailure()
    {
        // Given
        var command = WithdrawGoldCommand.Create(_withdrawer, _coinhouse, 500, "Test withdrawal");

        _mockCoinhouseRepo.Setup(r => r.GetByTag(_coinhouse)).Returns(_testCoinhouse);
        _mockCoinhouseRepo.Setup(r => r.GetAccountFor(It.IsAny<Guid>())).Returns((CoinHouseAccount?)null);

        // When
        var result = await _handler.HandleAsync(command);

        // Then
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("No account found"));
    }

    [Test]
    public async Task Given_RepositoryThrowsException_When_HandlingCommand_Then_ReturnsFailure()
    {
        // Given
        var command = WithdrawGoldCommand.Create(_withdrawer, _coinhouse, 500, "Test withdrawal");

        _mockCoinhouseRepo
            .Setup(r => r.GetByTag(_coinhouse))
            .Throws(new InvalidOperationException("Database error"));

        // When
        var result = await _handler.HandleAsync(command);

        // Then
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("Failed"));
    }

    #endregion

    #region Business Logic Tests

    [Test]
    public async Task Given_ZeroAmountWithdrawal_When_HandlingCommand_Then_ReturnsSuccess()
    {
        // Given
        var command = WithdrawGoldCommand.Create(_withdrawer, _coinhouse, 0, "Zero withdrawal test");

        _mockCoinhouseRepo.Setup(r => r.GetByTag(_coinhouse)).Returns(_testCoinhouse);
        _mockCoinhouseRepo.Setup(r => r.GetAccountFor(It.IsAny<Guid>())).Returns(_testAccount);
        _mockTransactionRepo
            .Setup(r => r.RecordTransactionAsync(It.IsAny<Database.Entities.Economy.Transaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Database.Entities.Economy.Transaction t, CancellationToken _) =>
            {
                t.Id = 123;
                return t;
            });

        // When
        var result = await _handler.HandleAsync(command);

        // Then
        Assert.That(result.Success, Is.True);
        Assert.That(_testAccount.Debit, Is.EqualTo(1000)); // Unchanged
    }

    [Test]
    public async Task Given_MultipleWithdrawals_When_HandlingCommands_Then_DecreasesBalanceAccumulatively()
    {
        // Given
        var command1 = WithdrawGoldCommand.Create(_withdrawer, _coinhouse, 300, "First withdrawal");
        var command2 = WithdrawGoldCommand.Create(_withdrawer, _coinhouse, 200, "Second withdrawal");

        _mockCoinhouseRepo.Setup(r => r.GetByTag(_coinhouse)).Returns(_testCoinhouse);
        _mockCoinhouseRepo.Setup(r => r.GetAccountFor(It.IsAny<Guid>())).Returns(_testAccount);
        _mockTransactionRepo
            .Setup(r => r.RecordTransactionAsync(It.IsAny<Database.Entities.Economy.Transaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Database.Entities.Economy.Transaction t, CancellationToken _) =>
            {
                t.Id = 123;
                return t;
            });

        // When
        await _handler.HandleAsync(command1);
        await _handler.HandleAsync(command2);

        // Then
        Assert.That(_testAccount.Debit, Is.EqualTo(500)); // 1000 - 300 - 200
    }

    #endregion

    #region Concurrency Tests

    [Test]
    public void Given_CancellationRequested_When_HandlingCommand_Then_PropagatesCancellation()
    {
        // Given
        var command = WithdrawGoldCommand.Create(_withdrawer, _coinhouse, 500, "Test withdrawal");
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _mockCoinhouseRepo.Setup(r => r.GetByTag(_coinhouse)).Returns(_testCoinhouse);

        // When/Then
        Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await _handler.HandleAsync(command, cts.Token));
    }

    #endregion
}

