using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Accounts;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Tests.Helpers.WorldEngine;
using Moq;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Tests.Economy.Queries;

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
    private CoinhouseDto _coinhouseDto = null!;
    private CoinhouseAccountDto _accountDto = null!;

    [SetUp]
    public void Setup()
    {
        _mockCoinhouseRepo = new Mock<ICoinhouseRepository>();
        _handler = new GetBalanceQueryHandler(_mockCoinhouseRepo.Object);

        _persona = PersonaTestHelpers.CreateCharacterPersona().Id;
        _coinhouse = EconomyTestHelpers.CreateCoinhouseTag("cordor_bank");

        _coinhouseDto = new CoinhouseDto
        {
            Id = 1,
            Tag = _coinhouse,
            Settlement = 1,
            EngineId = Guid.NewGuid(),
            Persona = PersonaId.FromCoinhouse(_coinhouse)
        };

        _accountDto = new CoinhouseAccountDto
        {
            Id = PersonaAccountId.ForCoinhouse(_persona, _coinhouse),
            Debit = 1000,
            Credit = 0,
            CoinHouseId = _coinhouseDto.Id,
            OpenedAt = DateTime.UtcNow,
            LastAccessedAt = DateTime.UtcNow,
            Coinhouse = _coinhouseDto
        };

        _mockCoinhouseRepo
            .Setup(r => r.GetByTagAsync(It.IsAny<CoinhouseTag>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CoinhouseTag tag, CancellationToken _) =>
                tag.Value == _coinhouse.Value ? _coinhouseDto : null);

        _mockCoinhouseRepo
            .Setup(r => r.GetAccountForAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid _, CancellationToken __) => _accountDto);
    }

    #region Happy Path Tests

    [Test]
    public async Task Given_AccountWithBalance_When_QueryingBalance_Then_ReturnsCorrectBalance()
    {
        // Given
    _accountDto = _accountDto with { Debit = 1500, Credit = 0 };
        GetBalanceQuery query = new GetBalanceQuery(_persona, _coinhouse);

        // When
        int? balance = await _handler.HandleAsync(query);

        // Then
        Assert.That(balance, Is.Not.Null);
        Assert.That(balance, Is.EqualTo(1500));
    }

    [Test]
    public async Task Given_AccountWithDebitAndCredit_When_QueryingBalance_Then_ReturnsNetBalance()
    {
        // Given
    _accountDto = _accountDto with { Debit = 1000, Credit = 300 };
        GetBalanceQuery query = new GetBalanceQuery(_persona, _coinhouse);

        // When
        int? balance = await _handler.HandleAsync(query);

        // Then
        Assert.That(balance, Is.EqualTo(700)); // 1000 - 300
    }

    [Test]
    public async Task Given_ZeroBalance_When_QueryingBalance_Then_ReturnsZero()
    {
        // Given
    _accountDto = _accountDto with { Debit = 0, Credit = 0 };
        GetBalanceQuery query = new GetBalanceQuery(_persona, _coinhouse);

        // When
        int? balance = await _handler.HandleAsync(query);

        // Then
        Assert.That(balance, Is.EqualTo(0));
    }

    #endregion

    #region Not Found Tests

    [Test]
    public async Task Given_NonexistentCoinhouse_When_QueryingBalance_Then_ReturnsNull()
    {
        // Given
        GetBalanceQuery query = new GetBalanceQuery(_persona, _coinhouse);

        _mockCoinhouseRepo
            .Setup(r => r.GetByTagAsync(_coinhouse, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CoinhouseDto?)null);

        // When
        int? balance = await _handler.HandleAsync(query);

        // Then
        Assert.That(balance, Is.Null);
    }

    [Test]
    public async Task Given_NoAccount_When_QueryingBalance_Then_ReturnsNull()
    {
        // Given
        GetBalanceQuery query = new GetBalanceQuery(_persona, _coinhouse);

        _mockCoinhouseRepo
            .Setup(r => r.GetAccountForAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CoinhouseAccountDto?)null);

        // When
        int? balance = await _handler.HandleAsync(query);

        // Then
        Assert.That(balance, Is.Null);
    }

    #endregion

    #region Edge Cases

    [Test]
    public async Task Given_NegativeBalance_When_QueryingBalance_Then_ReturnsNegativeValue()
    {
        // Given - Account is in debt (credit > debit)
    _accountDto = _accountDto with { Debit = 500, Credit = 800 };
        GetBalanceQuery query = new GetBalanceQuery(_persona, _coinhouse);

        // When
        int? balance = await _handler.HandleAsync(query);

        // Then
        Assert.That(balance, Is.EqualTo(-300)); // 500 - 800
    }

    [Test]
    public async Task Given_LargeBalance_When_QueryingBalance_Then_ReturnsCorrectValue()
    {
        // Given
    _accountDto = _accountDto with { Debit = 999999999, Credit = 0 };
        GetBalanceQuery query = new GetBalanceQuery(_persona, _coinhouse);

        // When
        int? balance = await _handler.HandleAsync(query);

        // Then
        Assert.That(balance, Is.EqualTo(999999999));
    }

    #endregion

    #region Read-Only Verification

    [Test]
    public async Task Given_Query_When_ExecutingMultipleTimes_Then_BalanceDoesNotChange()
    {
        // Given
    _accountDto = _accountDto with { Debit = 1000, Credit = 0 };
        GetBalanceQuery query = new GetBalanceQuery(_persona, _coinhouse);

        // When
        int? balance1 = await _handler.HandleAsync(query);
        int? balance2 = await _handler.HandleAsync(query);
        int? balance3 = await _handler.HandleAsync(query);

        // Then
        Assert.That(balance1, Is.EqualTo(1000));
        Assert.That(balance2, Is.EqualTo(1000));
        Assert.That(balance3, Is.EqualTo(1000));
        Assert.That(_accountDto.Debit, Is.EqualTo(1000), "Account balance should not be modified by queries");
    }

    #endregion
}

