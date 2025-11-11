using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities.Economy;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Tests.Helpers;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Accounts;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Transactions;
using Moq;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Tests.Commands;

/// <summary>
/// BDD-style tests for DepositGoldCommandHandler.
/// Tests the handler's ability to deposit gold into a coinhouse account.
/// </summary>
[TestFixture]
public class DepositGoldCommandHandlerTests
{
    private Mock<ICoinhouseRepository> _coinhouses = null!;
    private Mock<ITransactionRepository> _transactions = null!;
    private Mock<IEventBus> _eventBus = null!;
    private DepositGoldCommandHandler _handler = null!;

    private PersonaId _depositor;
    private CoinhouseTag _coinhouseTag;
    private CoinhouseDto _coinhouse = null!;
    private CoinhouseAccountDto _account = null!;

    [SetUp]
    public void SetUp()
    {
        _coinhouses = new Mock<ICoinhouseRepository>();
        _transactions = new Mock<ITransactionRepository>();
        _eventBus = new Mock<IEventBus>();

        _handler = new DepositGoldCommandHandler(
            _coinhouses.Object,
            _transactions.Object,
            _eventBus.Object);

        _depositor = PersonaTestHelpers.CreateCharacterPersona().Id;
        _coinhouseTag = EconomyTestHelpers.CreateCoinhouseTag("cordor_bank");

        _coinhouse = new CoinhouseDto
        {
            Id = 1,
            Tag = _coinhouseTag,
            Settlement = 1,
            EngineId = Guid.NewGuid(),
            Persona = PersonaId.FromCoinhouse(_coinhouseTag)
        };

        _account = new CoinhouseAccountDto
        {
            Id = PersonaAccountId.ForCoinhouse(_depositor, _coinhouseTag),
            Debit = 0,
            Credit = 0,
            CoinHouseId = _coinhouse.Id,
            OpenedAt = DateTime.UtcNow,
            LastAccessedAt = DateTime.UtcNow
        };

        _coinhouses
            .Setup(r => r.GetByTagAsync(It.IsAny<CoinhouseTag>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CoinhouseTag tag, CancellationToken _) =>
                tag.Value == _coinhouseTag.Value ? _coinhouse : null);

        _coinhouses
            .Setup(r => r.GetAccountForAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) =>
                id == _account.Id ? _account : null);

        _coinhouses
            .Setup(r => r.SaveAccountAsync(It.IsAny<CoinhouseAccountDto>(), It.IsAny<CancellationToken>()))
            .Callback<CoinhouseAccountDto, CancellationToken>((dto, _) => _account = dto)
            .Returns(Task.CompletedTask);
    }

    [Test]
    public async Task Given_ValidDeposit_When_HandlingCommand_Then_ReturnsSuccess()
    {
        DepositGoldCommand command = DepositGoldCommand.Create(_depositor, _coinhouseTag, 500, "Test deposit");

        _transactions
            .Setup(r => r.RecordTransactionAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction t, CancellationToken _) =>
            {
                t.Id = 123;
                return t;
            });

        CommandResult result = await _handler.HandleAsync(command);

        Assert.That(result.Success, Is.True);
        Assert.That(result.ErrorMessage, Is.Null);
    }

    [Test]
    public async Task Given_ValidDeposit_When_HandlingCommand_Then_UpdatesAccountBalance()
    {
        int initialBalance = _account.Debit;
        DepositGoldCommand command = DepositGoldCommand.Create(_depositor, _coinhouseTag, 500, "Test deposit");

        _transactions
            .Setup(r => r.RecordTransactionAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction t, CancellationToken _) =>
            {
                t.Id = 456;
                return t;
            });

        await _handler.HandleAsync(command);

        Assert.That(_account.Debit, Is.EqualTo(initialBalance + 500));
    }

    [Test]
    public async Task Given_ValidDeposit_When_HandlingCommand_Then_PublishesGoldDepositedEvent()
    {
        DepositGoldCommand command = DepositGoldCommand.Create(_depositor, _coinhouseTag, 250, "Test deposit");

        _transactions
            .Setup(r => r.RecordTransactionAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction t, CancellationToken _) =>
            {
                t.Id = 789;
                return t;
            });

        await _handler.HandleAsync(command);

        _eventBus.Verify(bus =>
            bus.PublishAsync(
                It.Is<GoldDepositedEvent>(evt =>
                    evt.Depositor == _depositor &&
                    evt.Coinhouse == _coinhouseTag &&
                    evt.Amount.Value == 250),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task Given_ValidDeposit_When_HandlingCommand_Then_RecordsTransaction()
    {
        DepositGoldCommand command = DepositGoldCommand.Create(_depositor, _coinhouseTag, 600, "Test deposit");
        Transaction? capturedTransaction = null;

        _transactions
            .Setup(r => r.RecordTransactionAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .Callback<Transaction, CancellationToken>((t, _) => capturedTransaction = t)
            .ReturnsAsync((Transaction t, CancellationToken _) =>
            {
                t.Id = 42;
                return t;
            });

        await _handler.HandleAsync(command);

        Assert.That(capturedTransaction, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(capturedTransaction!.FromPersonaId, Is.EqualTo(_depositor.ToString()));
            Assert.That(capturedTransaction.ToPersonaId, Is.EqualTo(_coinhouse.Persona.ToString()));
            Assert.That(capturedTransaction.Amount, Is.EqualTo(600));
            Assert.That(capturedTransaction.Memo, Does.Contain("Test deposit"));
        });
    }

    [Test]
    public async Task Given_NonexistentCoinhouse_When_HandlingCommand_Then_ReturnsFailure()
    {
        DepositGoldCommand command = DepositGoldCommand.Create(_depositor, _coinhouseTag, 500, "Test deposit");

        _coinhouses
            .Setup(r => r.GetByTagAsync(_coinhouseTag, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CoinhouseDto?)null);

        CommandResult result = await _handler.HandleAsync(command);

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("Coinhouse"));
        Assert.That(result.ErrorMessage, Does.Contain("not found"));
    }

    [Test]
    public async Task Given_NoExistingAccount_When_HandlingCommand_Then_CreatesAccount()
    {
        DepositGoldCommand command = DepositGoldCommand.Create(_depositor, _coinhouseTag, 400, "Test deposit");
        CoinhouseAccountDto? createdAccount = null;

        _coinhouses
            .Setup(r => r.GetAccountForAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CoinhouseAccountDto?)null);

        _coinhouses
            .Setup(r => r.SaveAccountAsync(It.IsAny<CoinhouseAccountDto>(), It.IsAny<CancellationToken>()))
            .Callback<CoinhouseAccountDto, CancellationToken>((dto, _) => createdAccount = dto)
            .Returns(Task.CompletedTask);

        _transactions
            .Setup(r => r.RecordTransactionAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction t, CancellationToken _) =>
            {
                t.Id = 99;
                return t;
            });

        CommandResult result = await _handler.HandleAsync(command);

        Assert.That(result.Success, Is.True);
        Assert.That(createdAccount, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(createdAccount!.Id, Is.EqualTo(PersonaAccountId.ForCoinhouse(_depositor, _coinhouseTag)));
            Assert.That(createdAccount.CoinHouseId, Is.EqualTo(_coinhouse.Id));
            Assert.That(createdAccount.Debit, Is.EqualTo(400));
        });
    }

    [Test]
    public async Task Given_RepositoryThrows_When_HandlingCommand_Then_ReturnsFailure()
    {
        DepositGoldCommand command = DepositGoldCommand.Create(_depositor, _coinhouseTag, 100, "Test deposit");

        _coinhouses
            .Setup(r => r.GetByTagAsync(_coinhouseTag, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        CommandResult result = await _handler.HandleAsync(command);

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("Failed"));
    }

    [Test]
    public async Task Given_MultipleDeposits_When_HandlingCommands_Then_SumsBalances()
    {
        DepositGoldCommand first = DepositGoldCommand.Create(_depositor, _coinhouseTag, 150, "First");
        DepositGoldCommand second = DepositGoldCommand.Create(_depositor, _coinhouseTag, 350, "Second");

        _transactions
            .Setup(r => r.RecordTransactionAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction t, CancellationToken _) => t);

        await _handler.HandleAsync(first);
        await _handler.HandleAsync(second);

        Assert.That(_account.Debit, Is.EqualTo(500));
    }

    [Test]
    public async Task Given_ZeroDeposit_When_HandlingCommand_Then_CompletesSuccessfully()
    {
        DepositGoldCommand command = DepositGoldCommand.Create(_depositor, _coinhouseTag, 0, "Zero deposit");

        _transactions
            .Setup(r => r.RecordTransactionAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction t, CancellationToken _) => t);

        CommandResult result = await _handler.HandleAsync(command);

        Assert.That(result.Success, Is.True);
        Assert.That(_account.Debit, Is.EqualTo(0));
    }

    [Test]
    public void Given_CancellationRequested_When_HandlingCommand_Then_Throws()
    {
        DepositGoldCommand command = DepositGoldCommand.Create(_depositor, _coinhouseTag, 200, "Test deposit");
        using CancellationTokenSource cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await _handler.HandleAsync(command, cts.Token));
    }
}

