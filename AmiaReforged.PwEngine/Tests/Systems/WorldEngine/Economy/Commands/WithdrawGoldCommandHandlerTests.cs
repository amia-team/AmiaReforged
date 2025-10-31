using System;
using System.Threading;
using System.Threading.Tasks;
using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities.Economy;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Accounts;
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
/// BDD-style tests for WithdrawGoldCommandHandler using the DTO-based repository contract.
/// </summary>
[TestFixture]
public class WithdrawGoldCommandHandlerTests
{
    private Mock<ICoinhouseRepository> _coinhouses = null!;
    private Mock<ITransactionRepository> _transactions = null!;
    private Mock<IEventBus> _eventBus = null!;
    private WithdrawGoldCommandHandler _handler = null!;

    private PersonaId _withdrawer;
    private CoinhouseTag _coinhouseTag;
    private CoinhouseDto _coinhouse = null!;
    private CoinhouseAccountDto _account = null!;

    [SetUp]
    public void Setup()
    {
        _coinhouses = new Mock<ICoinhouseRepository>();
        _transactions = new Mock<ITransactionRepository>();
        _eventBus = new Mock<IEventBus>();

        _handler = new WithdrawGoldCommandHandler(
            _coinhouses.Object,
            _transactions.Object,
            _eventBus.Object);

        _withdrawer = PersonaTestHelpers.CreateCharacterPersona().Id;
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
            Id = PersonaAccountId.ForCoinhouse(_withdrawer, _coinhouseTag),
            Debit = 1000,
            Credit = 0,
            CoinHouseId = _coinhouse.Id,
            OpenedAt = DateTime.UtcNow,
            LastAccessedAt = DateTime.UtcNow,
            Coinhouse = _coinhouse
        };

        _coinhouses
            .Setup(r => r.GetByTagAsync(It.IsAny<CoinhouseTag>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CoinhouseTag tag, CancellationToken _) =>
                tag.Value == _coinhouseTag.Value ? _coinhouse : null);

        _coinhouses
            .Setup(r => r.GetAccountForAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid _, CancellationToken __) => _account);

        _coinhouses
            .Setup(r => r.SaveAccountAsync(It.IsAny<CoinhouseAccountDto>(), It.IsAny<CancellationToken>()))
            .Callback<CoinhouseAccountDto, CancellationToken>((dto, _) => _account = dto)
            .Returns(Task.CompletedTask);
    }

    private void SetupTransactionSuccess()
    {
        _transactions
            .Setup(r => r.RecordTransactionAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Transaction t, CancellationToken _) =>
            {
                t.Id = 123;
                return t;
            });
    }

    #region Happy Path Tests

    [Test]
    public async Task Given_ValidWithdrawal_When_HandlingCommand_Then_ReturnsSuccess()
    {
        SetupTransactionSuccess();
        WithdrawGoldCommand command = WithdrawGoldCommand.Create(_withdrawer, _coinhouseTag, 500, "Test withdrawal");

        CommandResult result = await _handler.HandleAsync(command);

        Assert.That(result.Success, Is.True);
        Assert.That(result.ErrorMessage, Is.Null);
    }

    [Test]
    public async Task Given_ValidWithdrawal_When_HandlingCommand_Then_DecreasesAccountBalance()
    {
        SetupTransactionSuccess();
        int startingBalance = _account.Debit;
        int amount = 500;
        WithdrawGoldCommand command = WithdrawGoldCommand.Create(_withdrawer, _coinhouseTag, amount, "Test withdrawal");

        await _handler.HandleAsync(command);

        Assert.That(_account.Debit, Is.EqualTo(startingBalance - amount));
    }

    [Test]
    public async Task Given_ValidWithdrawal_When_HandlingCommand_Then_PublishesGoldWithdrawnEvent()
    {
        SetupTransactionSuccess();
        WithdrawGoldCommand command = WithdrawGoldCommand.Create(_withdrawer, _coinhouseTag, 500, "Test withdrawal");

        await _handler.HandleAsync(command);

        _eventBus.Verify(bus =>
            bus.PublishAsync(
                It.Is<GoldWithdrawnEvent>(e =>
                    e.Withdrawer == _withdrawer &&
                    e.Coinhouse == _coinhouseTag &&
                    e.Amount.Value == 500),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Test]
    public async Task Given_ValidWithdrawal_When_HandlingCommand_Then_RecordsTransaction()
    {
        Transaction? captured = null;
        _transactions
            .Setup(r => r.RecordTransactionAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()))
            .Callback<Transaction, CancellationToken>((t, _) => captured = t)
            .ReturnsAsync((Transaction t, CancellationToken _) =>
            {
                t.Id = 456;
                return t;
            });

        WithdrawGoldCommand command = WithdrawGoldCommand.Create(_withdrawer, _coinhouseTag, 500, "Test withdrawal");
        await _handler.HandleAsync(command);

        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.FromPersonaId, Is.EqualTo(_coinhouse.Persona.ToString()));
        Assert.That(captured.ToPersonaId, Is.EqualTo(_withdrawer.ToString()));
        Assert.That(captured.Amount, Is.EqualTo(500));
        Assert.That(captured.Memo, Does.Contain("Test withdrawal"));
    }

    [Test]
    public async Task Given_ExactBalanceWithdrawal_When_HandlingCommand_Then_BalanceBecomesZero()
    {
        SetupTransactionSuccess();
        WithdrawGoldCommand command = WithdrawGoldCommand.Create(_withdrawer, _coinhouseTag, _account.Debit, "Withdraw all");

        CommandResult result = await _handler.HandleAsync(command);

        Assert.That(result.Success, Is.True);
        Assert.That(_account.Debit, Is.EqualTo(0));
    }

    #endregion

    #region Validation Tests - Balance Checks

    [Test]
    public async Task Given_InsufficientBalance_When_HandlingCommand_Then_ReturnsFailure()
    {
        _account = _account with { Debit = 100 };
        WithdrawGoldCommand command = WithdrawGoldCommand.Create(_withdrawer, _coinhouseTag, 500, "Overdraft attempt");

        CommandResult result = await _handler.HandleAsync(command);

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("Insufficient balance"));
    }

    [Test]
    public async Task Given_InsufficientBalance_When_HandlingCommand_Then_DoesNotModifyBalance()
    {
        _account = _account with { Debit = 100 };
        int initialBalance = _account.Debit;
        WithdrawGoldCommand command = WithdrawGoldCommand.Create(_withdrawer, _coinhouseTag, 500, "Overdraft attempt");

        await _handler.HandleAsync(command);

        Assert.That(_account.Debit, Is.EqualTo(initialBalance));
    }

    [Test]
    public async Task Given_InsufficientBalance_When_HandlingCommand_Then_DoesNotPublishEvent()
    {
        _account = _account with { Debit = 100 };
        WithdrawGoldCommand command = WithdrawGoldCommand.Create(_withdrawer, _coinhouseTag, 500, "Overdraft attempt");

        await _handler.HandleAsync(command);

        _eventBus.Verify(bus =>
            bus.PublishAsync(It.IsAny<GoldWithdrawnEvent>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task Given_InsufficientBalance_When_HandlingCommand_Then_DoesNotRecordTransaction()
    {
        _account = _account with { Debit = 100 };
        WithdrawGoldCommand command = WithdrawGoldCommand.Create(_withdrawer, _coinhouseTag, 500, "Overdraft attempt");

        await _handler.HandleAsync(command);

        _transactions.Verify(repo =>
            repo.RecordTransactionAsync(It.IsAny<Transaction>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region Validation Tests - Other

    [Test]
    public async Task Given_NonexistentCoinhouse_When_HandlingCommand_Then_ReturnsFailure()
    {
        _coinhouses
            .Setup(r => r.GetByTagAsync(_coinhouseTag, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CoinhouseDto?)null);

        WithdrawGoldCommand command = WithdrawGoldCommand.Create(_withdrawer, _coinhouseTag, 500, "Test withdrawal");
        CommandResult result = await _handler.HandleAsync(command);

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("Coinhouse"));
    }

    [Test]
    public async Task Given_NoAccount_When_HandlingCommand_Then_ReturnsFailure()
    {
        _coinhouses
            .Setup(r => r.GetAccountForAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CoinhouseAccountDto?)null);

        WithdrawGoldCommand command = WithdrawGoldCommand.Create(_withdrawer, _coinhouseTag, 500, "Test withdrawal");
        CommandResult result = await _handler.HandleAsync(command);

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("No account"));
    }

    [Test]
    public async Task Given_RepositoryThrowsException_When_HandlingCommand_Then_ReturnsFailure()
    {
        _coinhouses
            .Setup(r => r.GetByTagAsync(_coinhouseTag, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Database error"));

        WithdrawGoldCommand command = WithdrawGoldCommand.Create(_withdrawer, _coinhouseTag, 500, "Test withdrawal");
        CommandResult result = await _handler.HandleAsync(command);

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("Failed"));
    }

    #endregion

    #region Business Logic Tests

    [Test]
    public async Task Given_ZeroAmountWithdrawal_When_HandlingCommand_Then_ReturnsSuccess()
    {
        SetupTransactionSuccess();
        WithdrawGoldCommand command = WithdrawGoldCommand.Create(_withdrawer, _coinhouseTag, 0, "Zero withdrawal test");

        CommandResult result = await _handler.HandleAsync(command);

        Assert.That(result.Success, Is.True);
        Assert.That(_account.Debit, Is.EqualTo(1000));
    }

    [Test]
    public async Task Given_MultipleWithdrawals_When_HandlingCommands_Then_DecreasesBalanceAccumulatively()
    {
        SetupTransactionSuccess();
        WithdrawGoldCommand first = WithdrawGoldCommand.Create(_withdrawer, _coinhouseTag, 300, "First");
        WithdrawGoldCommand second = WithdrawGoldCommand.Create(_withdrawer, _coinhouseTag, 200, "Second");

        await _handler.HandleAsync(first);
        await _handler.HandleAsync(second);

        Assert.That(_account.Debit, Is.EqualTo(500));
    }

    #endregion

    #region Concurrency Tests

    [Test]
    public void Given_CancellationRequested_When_HandlingCommand_Then_PropagatesCancellation()
    {
        WithdrawGoldCommand command = WithdrawGoldCommand.Create(_withdrawer, _coinhouseTag, 500, "Test withdrawal");
        using CancellationTokenSource cts = new CancellationTokenSource();
        cts.Cancel();

        Assert.ThrowsAsync<OperationCanceledException>(async () =>
            await _handler.HandleAsync(command, cts.Token));
    }

    #endregion
}

