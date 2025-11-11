using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Tests.Helpers;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Accounts;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.DTOs;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Queries;
using Moq;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Tests.Queries;
[TestFixture]
public class GetCoinhouseBalancesQueryTests
{
    private Mock<ICoinhouseRepository> _mockCoinhouseRepo = null!;
    private GetCoinhouseBalancesQueryHandler _handler = null!;
    private PersonaId _persona;
    private CoinhouseTag _coinhouse;
    private CoinhouseDto _coinhouseDto = null!;
    private CoinhouseAccountDto _accountDto = null!;
    [SetUp]
    public void Setup()
    {
        _mockCoinhouseRepo = new Mock<ICoinhouseRepository>();
        _handler = new GetCoinhouseBalancesQueryHandler(_mockCoinhouseRepo.Object);
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
            .Setup(r => r.GetAccountsForHolderAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid _, CancellationToken __) => (IReadOnlyList<CoinhouseAccountDto>)new List<CoinhouseAccountDto> { _accountDto });

        _mockCoinhouseRepo
            .Setup(r => r.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((long id, CancellationToken _) =>
                id == _coinhouseDto.Id ? _coinhouseDto : null);
    }
    [Test]
    public async Task Given_PersonaWithAccount_When_QueryingBalances_Then_ReturnsBalances()
    {
        GetCoinhouseBalancesQuery query = new GetCoinhouseBalancesQuery(_persona);
        IReadOnlyList<BalanceDto> balances = await _handler.HandleAsync(query);
        Assert.That(balances, Is.Not.Null);
        Assert.That(balances, Has.Count.EqualTo(1));
        Assert.That(balances[0].Balance, Is.EqualTo(1000));
        Assert.That(balances[0].PersonaId, Is.EqualTo(_persona));
        Assert.That(balances[0].Coinhouse, Is.EqualTo(_coinhouse));
    }
    [Test]
    public async Task Given_PersonaWithAccount_When_QueryingBalances_Then_IncludesLastAccessTime()
    {
        DateTime lastAccessed = DateTime.UtcNow.AddHours(-2);
        _accountDto = _accountDto with { LastAccessedAt = lastAccessed };
        GetCoinhouseBalancesQuery query = new GetCoinhouseBalancesQuery(_persona);
        IReadOnlyList<BalanceDto> balances = await _handler.HandleAsync(query);
        Assert.That(balances[0].LastAccessedAt, Is.EqualTo(lastAccessed));
    }
    [Test]
    public async Task Given_AccountWithZeroBalance_When_QueryingBalances_Then_IncludesZeroBalance()
    {
        _accountDto = _accountDto with { Debit = 0, Credit = 0 };
        GetCoinhouseBalancesQuery query = new GetCoinhouseBalancesQuery(_persona);
        IReadOnlyList<BalanceDto> balances = await _handler.HandleAsync(query);
        Assert.That(balances, Has.Count.EqualTo(1));
        Assert.That(balances[0].Balance, Is.EqualTo(0));
    }
    [Test]
    public async Task Given_PersonaWithNoAccounts_When_QueryingBalances_Then_ReturnsEmptyList()
    {
        GetCoinhouseBalancesQuery query = new GetCoinhouseBalancesQuery(_persona);
        _mockCoinhouseRepo
            .Setup(r => r.GetAccountsForHolderAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<CoinhouseAccountDto>());
        IReadOnlyList<BalanceDto> balances = await _handler.HandleAsync(query);
        Assert.That(balances, Is.Not.Null);
        Assert.That(balances, Is.Empty);
    }
    [Test]
    public async Task Given_Query_When_ExecutingMultipleTimes_Then_ResultsAreConsistent()
    {
        GetCoinhouseBalancesQuery query = new GetCoinhouseBalancesQuery(_persona);
        IReadOnlyList<BalanceDto> balances1 = await _handler.HandleAsync(query);
        IReadOnlyList<BalanceDto> balances2 = await _handler.HandleAsync(query);
        IReadOnlyList<BalanceDto> balances3 = await _handler.HandleAsync(query);
        Assert.That(balances1, Has.Count.EqualTo(1));
        Assert.That(balances2, Has.Count.EqualTo(1));
        Assert.That(balances3, Has.Count.EqualTo(1));
        Assert.That(balances1[0].Balance, Is.EqualTo(balances2[0].Balance));
        Assert.That(balances2[0].Balance, Is.EqualTo(balances3[0].Balance));
    }
    [Test]
    public async Task Given_NegativeBalance_When_QueryingBalances_Then_ReturnsNegativeValue()
    {
        _accountDto = _accountDto with { Debit = 100, Credit = 500 };
        GetCoinhouseBalancesQuery query = new GetCoinhouseBalancesQuery(_persona);
        IReadOnlyList<BalanceDto> balances = await _handler.HandleAsync(query);
        Assert.That(balances[0].Balance, Is.EqualTo(-400));
    }
}
