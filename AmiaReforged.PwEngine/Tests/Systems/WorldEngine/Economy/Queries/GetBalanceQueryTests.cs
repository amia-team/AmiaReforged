using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities.Economy.Treasuries;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Tests.Helpers.WorldEngine;
using Moq;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.Economy.Queries;

/// <summary>
/// BDD-style tests for GetBalanceQueryHandler.
/// Tests the handler's ability to retrieve persona balances from coinhouses.
/// </summary>
[TestFixture]
public class GetBalanceQueryHandlerTests
{
    private Mock<ICoinhouseRepository> _mockCoinhouseRepo = null!;
    private GetBalanceQueryHandler _handler = null!;

    private PersonaId _persona;
    private CoinhouseTag _coinhouse;
    private CoinHouse _testCoinhouse = null!;
    private CoinHouseAccount _testAccount = null!;

    [SetUp]
    public void Setup()
    {
        _mockCoinhouseRepo = new Mock<ICoinhouseRepository>();
        _handler = new GetBalanceQueryHandler(_mockCoinhouseRepo.Object);

        _persona = PersonaTestHelpers.CreateCharacterPersona().Id;
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
            Debit = 1000,
            Credit = 0,
            CoinHouseId = _testCoinhouse.Id,
            OpenedAt = DateTime.UtcNow,
            LastAccessedAt = DateTime.UtcNow
        };

        _testCoinhouse.Accounts!.Add(_testAccount);
    }

    #region Happy Path Tests

    [Test]
    public async Task Given_AccountWithBalance_When_QueryingBalance_Then_ReturnsCorrectBalance()
    {
        // Given
        _testAccount.Debit = 1500;
        _testAccount.Credit = 0;
        var query = new GetBalanceQuery(_persona, _coinhouse);

        _mockCoinhouseRepo.Setup(r => r.GetByTag(_coinhouse)).Returns(_testCoinhouse);
        _mockCoinhouseRepo.Setup(r => r.GetAccountFor(It.IsAny<Guid>())).Returns(_testAccount);

        // When
        var balance = await _handler.HandleAsync(query);

        // Then
        Assert.That(balance, Is.Not.Null);
        Assert.That(balance, Is.EqualTo(1500));
    }

    [Test]
    public async Task Given_AccountWithDebitAndCredit_When_QueryingBalance_Then_ReturnsNetBalance()
    {
        // Given
        _testAccount.Debit = 1000;
        _testAccount.Credit = 300; // Has some debt
        var query = new GetBalanceQuery(_persona, _coinhouse);

        _mockCoinhouseRepo.Setup(r => r.GetByTag(_coinhouse)).Returns(_testCoinhouse);
        _mockCoinhouseRepo.Setup(r => r.GetAccountFor(It.IsAny<Guid>())).Returns(_testAccount);

        // When
        var balance = await _handler.HandleAsync(query);

        // Then
        Assert.That(balance, Is.EqualTo(700)); // 1000 - 300
    }

    [Test]
    public async Task Given_ZeroBalance_When_QueryingBalance_Then_ReturnsZero()
    {
        // Given
        _testAccount.Debit = 0;
        _testAccount.Credit = 0;
        var query = new GetBalanceQuery(_persona, _coinhouse);

        _mockCoinhouseRepo.Setup(r => r.GetByTag(_coinhouse)).Returns(_testCoinhouse);
        _mockCoinhouseRepo.Setup(r => r.GetAccountFor(It.IsAny<Guid>())).Returns(_testAccount);

        // When
        var balance = await _handler.HandleAsync(query);

        // Then
        Assert.That(balance, Is.EqualTo(0));
    }

    #endregion

    #region Not Found Tests

    [Test]
    public async Task Given_NonexistentCoinhouse_When_QueryingBalance_Then_ReturnsNull()
    {
        // Given
        var query = new GetBalanceQuery(_persona, _coinhouse);

        _mockCoinhouseRepo.Setup(r => r.GetByTag(_coinhouse)).Returns((CoinHouse?)null);

        // When
        var balance = await _handler.HandleAsync(query);

        // Then
        Assert.That(balance, Is.Null);
    }

    [Test]
    public async Task Given_NoAccount_When_QueryingBalance_Then_ReturnsNull()
    {
        // Given
        var query = new GetBalanceQuery(_persona, _coinhouse);

        _mockCoinhouseRepo.Setup(r => r.GetByTag(_coinhouse)).Returns(_testCoinhouse);
        _mockCoinhouseRepo.Setup(r => r.GetAccountFor(It.IsAny<Guid>())).Returns((CoinHouseAccount?)null);

        // When
        var balance = await _handler.HandleAsync(query);

        // Then
        Assert.That(balance, Is.Null);
    }

    #endregion

    #region Edge Cases

    [Test]
    public async Task Given_NegativeBalance_When_QueryingBalance_Then_ReturnsNegativeValue()
    {
        // Given - Account is in debt (credit > debit)
        _testAccount.Debit = 500;
        _testAccount.Credit = 800;
        var query = new GetBalanceQuery(_persona, _coinhouse);

        _mockCoinhouseRepo.Setup(r => r.GetByTag(_coinhouse)).Returns(_testCoinhouse);
        _mockCoinhouseRepo.Setup(r => r.GetAccountFor(It.IsAny<Guid>())).Returns(_testAccount);

        // When
        var balance = await _handler.HandleAsync(query);

        // Then
        Assert.That(balance, Is.EqualTo(-300)); // 500 - 800
    }

    [Test]
    public async Task Given_LargeBalance_When_QueryingBalance_Then_ReturnsCorrectValue()
    {
        // Given
        _testAccount.Debit = 999999999;
        _testAccount.Credit = 0;
        var query = new GetBalanceQuery(_persona, _coinhouse);

        _mockCoinhouseRepo.Setup(r => r.GetByTag(_coinhouse)).Returns(_testCoinhouse);
        _mockCoinhouseRepo.Setup(r => r.GetAccountFor(It.IsAny<Guid>())).Returns(_testAccount);

        // When
        var balance = await _handler.HandleAsync(query);

        // Then
        Assert.That(balance, Is.EqualTo(999999999));
    }

    #endregion

    #region Read-Only Verification

    [Test]
    public async Task Given_Query_When_ExecutingMultipleTimes_Then_BalanceDoesNotChange()
    {
        // Given
        _testAccount.Debit = 1000;
        var query = new GetBalanceQuery(_persona, _coinhouse);

        _mockCoinhouseRepo.Setup(r => r.GetByTag(_coinhouse)).Returns(_testCoinhouse);
        _mockCoinhouseRepo.Setup(r => r.GetAccountFor(It.IsAny<Guid>())).Returns(_testAccount);

        // When
        var balance1 = await _handler.HandleAsync(query);
        var balance2 = await _handler.HandleAsync(query);
        var balance3 = await _handler.HandleAsync(query);

        // Then
        Assert.That(balance1, Is.EqualTo(1000));
        Assert.That(balance2, Is.EqualTo(1000));
        Assert.That(balance3, Is.EqualTo(1000));
        Assert.That(_testAccount.Debit, Is.EqualTo(1000), "Account balance should not be modified by queries");
    }

    #endregion
}

