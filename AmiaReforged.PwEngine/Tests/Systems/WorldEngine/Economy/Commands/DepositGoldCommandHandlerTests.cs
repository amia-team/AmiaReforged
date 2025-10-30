using System.Threading;
using System.Threading.Tasks;
using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities.Economy.Treasuries;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Transactions;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Tests.Helpers.WorldEngine;
using Moq;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.Economy.Commands;

/// <summary>
/// BDD-style tests for DepositGoldCommandHandler.
/// Tests the handler's ability to deposit gold into a coinhouse account.
/// </summary>
[TestFixture]
public class DepositGoldCommandHandlerTests
{
    private Mock<ICoinhouseRepository> _mockCoinhouseRepo = null!;
    private Mock<ITransactionRepository> _mockTransactionRepo = null!;
    private Mock<IEventBus> _mockEventBus = null!;
    private DepositGoldCommandHandler _handler = null!;

    private PersonaId _depositor;
    private CoinhouseTag _coinhouse;
    private CoinHouse _testCoinhouse = null!;
    private CoinHouseAccount _testAccount = null!;

    [SetUp]
    public void Setup()
    {
        _mockCoinhouseRepo = new Mock<ICoinhouseRepository>();
        _mockTransactionRepo = new Mock<ITransactionRepository>();
        _mockEventBus = new Mock<IEventBus>();
        _handler = new DepositGoldCommandHandler(
            _mockCoinhouseRepo.Object,
            _mockTransactionRepo.Object,
            _mockEventBus.Object);

        _depositor = PersonaTestHelpers.CreateCharacterPersona().Id;
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
            Id = _depositor.Value == "Character" ? Guid.Parse(_depositor.Value.Split(':')[1]) : Guid.NewGuid(),
            Debit = 0,
            Credit = 0,
            CoinHouseId = _testCoinhouse.Id,
            OpenedAt = DateTime.UtcNow
        };

        _testCoinhouse.Accounts!.Add(_testAccount);

        _mockCoinhouseRepo
            .Setup(r => r.SaveAccountAsync(It.IsAny<CoinHouseAccount>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    #region Happy Path Tests

    [Test]
    public async Task Given_ValidDeposit_When_HandlingCommand_Then_ReturnsSuccess()
    {
        // Given
        DepositGoldCommand command = DepositGoldCommand.Create(_depositor, _coinhouse, 500, "Test deposit");

        _mockCoinhouseRepo
            .Setup(r => r.GetByTag(_coinhouse))
            .Returns(_testCoinhouse);

        _mockCoinhouseRepo
            .Setup(r => r.GetAccountFor(It.IsAny<Guid>()))
            .Returns(_testAccount);

        _mockTransactionRepo
            .Setup(r => r.RecordTransactionAsync(It.IsAny<Database.Entities.Economy.Transaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Database.Entities.Economy.Transaction t, CancellationToken ct) =>
            {
                t.Id = 123;
                return t;
            });

        // When
        CommandResult result = await _handler.HandleAsync(command);

        // Then
        Assert.That(result.Success, Is.True);
        Assert.That(result.ErrorMessage, Is.Null);
    }

    [Test]
    public async Task Given_ValidDeposit_When_HandlingCommand_Then_UpdatesAccountBalance()
    {
        // Given
        int initialBalance = _testAccount.Debit;
        int depositAmount = 500;
        DepositGoldCommand command = DepositGoldCommand.Create(_depositor, _coinhouse, depositAmount, "Test deposit");

        _mockCoinhouseRepo.Setup(r => r.GetByTag(_coinhouse)).Returns(_testCoinhouse);
        _mockCoinhouseRepo.Setup(r => r.GetAccountFor(It.IsAny<Guid>())).Returns(_testAccount);
        _mockTransactionRepo
            .Setup(r => r.RecordTransactionAsync(It.IsAny<Database.Entities.Economy.Transaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Database.Entities.Economy.Transaction t, CancellationToken ct) =>
            {
                t.Id = 123;
                return t;
            });

        // When
        await _handler.HandleAsync(command);

        // Then
        Assert.That(_testAccount.Debit, Is.EqualTo(initialBalance + depositAmount));
    }

    [Test]
    public async Task Given_ValidDeposit_When_HandlingCommand_Then_PublishesGoldDepositedEvent()
    {
        // Given
        DepositGoldCommand command = DepositGoldCommand.Create(_depositor, _coinhouse, 500, "Test deposit");

        _mockCoinhouseRepo.Setup(r => r.GetByTag(_coinhouse)).Returns(_testCoinhouse);
        _mockCoinhouseRepo.Setup(r => r.GetAccountFor(It.IsAny<Guid>())).Returns(_testAccount);
        _mockTransactionRepo
            .Setup(r => r.RecordTransactionAsync(It.IsAny<Database.Entities.Economy.Transaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Database.Entities.Economy.Transaction t, CancellationToken ct) =>
            {
                t.Id = 123;
                return t;
            });

        // When
        await _handler.HandleAsync(command);

        // Then
        _mockEventBus.Verify(bus =>
            bus.PublishAsync(
                It.Is<GoldDepositedEvent>(e =>
                    e.Depositor == _depositor &&
                    e.Coinhouse == _coinhouse &&
                    e.Amount.Value == 500),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task Given_ValidDeposit_When_HandlingCommand_Then_RecordsTransaction()
    {
        // Given
        DepositGoldCommand command = DepositGoldCommand.Create(_depositor, _coinhouse, 500, "Test deposit");

        _mockCoinhouseRepo.Setup(r => r.GetByTag(_coinhouse)).Returns(_testCoinhouse);
        _mockCoinhouseRepo.Setup(r => r.GetAccountFor(It.IsAny<Guid>())).Returns(_testAccount);
        _mockTransactionRepo
            .Setup(r => r.RecordTransactionAsync(It.IsAny<Database.Entities.Economy.Transaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Database.Entities.Economy.Transaction t, CancellationToken ct) =>
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
                    t.FromPersonaId == _depositor.ToString() &&
                    t.ToPersonaId == _testCoinhouse.PersonaId.ToString() &&
                    t.Amount == 500 &&
                    t.Memo!.Contains("Test deposit")),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Validation Tests

    [Test]
    public async Task Given_NonexistentCoinhouse_When_HandlingCommand_Then_ReturnsFailure()
    {
        // Given
        DepositGoldCommand command = DepositGoldCommand.Create(_depositor, _coinhouse, 500, "Test deposit");

        _mockCoinhouseRepo
            .Setup(r => r.GetByTag(_coinhouse))
            .Returns((CoinHouse?)null);

        // When
        CommandResult result = await _handler.HandleAsync(command);

        // Then
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("Coinhouse"));
        Assert.That(result.ErrorMessage, Does.Contain("not found"));
    }

    [Test]
    public async Task Given_NoAccountExists_When_HandlingCommand_Then_CreatesNewAccount()
    {
        // Given
        DepositGoldCommand command = DepositGoldCommand.Create(_depositor, _coinhouse, 500, "Test deposit");

        _mockCoinhouseRepo.Setup(r => r.GetByTag(_coinhouse)).Returns(_testCoinhouse);
        _mockCoinhouseRepo
            .Setup(r => r.GetAccountFor(It.IsAny<Guid>()))
            .Returns((CoinHouseAccount?)null);

        _mockTransactionRepo
            .Setup(r => r.RecordTransactionAsync(It.IsAny<Database.Entities.Economy.Transaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Database.Entities.Economy.Transaction t, CancellationToken ct) =>
            {
                t.Id = 123;
                return t;
            });

        // When
        CommandResult result = await _handler.HandleAsync(command);

        // Then
        Assert.That(result.Success, Is.True);
        // Verify a new account was created in the coinhouse
        Assert.That(_testCoinhouse.Accounts, Has.Count.GreaterThan(1));
    }

    [Test]
    public async Task Given_RepositoryThrowsException_When_HandlingCommand_Then_ReturnsFailure()
    {
        // Given
        DepositGoldCommand command = DepositGoldCommand.Create(_depositor, _coinhouse, 500, "Test deposit");

        _mockCoinhouseRepo
            .Setup(r => r.GetByTag(_coinhouse))
            .Throws(new InvalidOperationException("Database error"));

        // When
        CommandResult result = await _handler.HandleAsync(command);

        // Then
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("Failed"));
    }

    #endregion

    #region Business Logic Tests

    [Test]
    public async Task Given_MultipleDeposits_When_HandlingCommands_Then_AccumulatesBalance()
    {
        // Given
        DepositGoldCommand command1 = DepositGoldCommand.Create(_depositor, _coinhouse, 500, "First deposit");
        DepositGoldCommand command2 = DepositGoldCommand.Create(_depositor, _coinhouse, 300, "Second deposit");

        _mockCoinhouseRepo.Setup(r => r.GetByTag(_coinhouse)).Returns(_testCoinhouse);
        _mockCoinhouseRepo.Setup(r => r.GetAccountFor(It.IsAny<Guid>())).Returns(_testAccount);
        _mockTransactionRepo
            .Setup(r => r.RecordTransactionAsync(It.IsAny<Database.Entities.Economy.Transaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Database.Entities.Economy.Transaction t, CancellationToken ct) =>
            {
                t.Id = 123;
                return t;
            });

        // When
        await _handler.HandleAsync(command1);
        await _handler.HandleAsync(command2);

        // Then
        Assert.That(_testAccount.Debit, Is.EqualTo(800));
    }

    [Test]
    public async Task Given_ZeroAmountDeposit_When_HandlingCommand_Then_ReturnsSuccess()
    {
        // Given - DepositGoldCommand factory allows zero (valid for some business cases)
        DepositGoldCommand command = DepositGoldCommand.Create(_depositor, _coinhouse, 0, "Zero deposit test");

        _mockCoinhouseRepo.Setup(r => r.GetByTag(_coinhouse)).Returns(_testCoinhouse);
        _mockCoinhouseRepo.Setup(r => r.GetAccountFor(It.IsAny<Guid>())).Returns(_testAccount);
        _mockTransactionRepo
            .Setup(r => r.RecordTransactionAsync(It.IsAny<Database.Entities.Economy.Transaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Database.Entities.Economy.Transaction t, CancellationToken ct) =>
            {
                t.Id = 123;
                return t;
            });

        // When
        CommandResult result = await _handler.HandleAsync(command);

        // Then
        Assert.That(result.Success, Is.True);
        Assert.That(_testAccount.Debit, Is.EqualTo(0));
    }

    #endregion

    #region Concurrency Tests

    [Test]
    public void Given_CancellationRequested_When_HandlingCommand_Then_PropagatesCancellation()
    {
        // Given
        DepositGoldCommand command = DepositGoldCommand.Create(_depositor, _coinhouse, 500, "Test deposit");
        CancellationTokenSource cts = new CancellationTokenSource();
        cts.Cancel();

        _mockCoinhouseRepo.Setup(r => r.GetByTag(_coinhouse)).Returns(_testCoinhouse);

        // When/Then
        Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await _handler.HandleAsync(command, cts.Token));
    }

    #endregion
}

