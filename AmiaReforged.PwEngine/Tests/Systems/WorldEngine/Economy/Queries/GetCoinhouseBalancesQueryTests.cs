using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities.Economy.Treasuries;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.DTOs;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Tests.Helpers.WorldEngine;
using Moq;
using NUnit.Framework;
namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.Economy.Queries;
[TestFixture]
public class GetCoinhouseBalancesQueryTests
{
    private Mock<ICoinhouseRepository> _mockCoinhouseRepo = null!;
    private GetCoinhouseBalancesQueryHandler _handler = null!;
    private PersonaId _persona;
    private CoinhouseTag _coinhouse;
    private CoinHouse _testCoinhouse = null!;
    private CoinHouseAccount _testAccount = null!;
    [SetUp]
    public void Setup()
    {
        _mockCoinhouseRepo = new Mock<ICoinhouseRepository>();
        _handler = new GetCoinhouseBalancesQueryHandler(_mockCoinhouseRepo.Object);
        _persona = PersonaTestHelpers.CreateCharacterPersona().Id;
        _coinhouse = EconomyTestHelpers.CreateCoinhouseTag("cordor_bank");
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
            CoinHouse = _testCoinhouse,
            OpenedAt = DateTime.UtcNow,
            LastAccessedAt = DateTime.UtcNow
        };
        _testCoinhouse.Accounts!.Add(_testAccount);
    }
    [Test]
    public async Task Given_PersonaWithAccount_When_QueryingBalances_Then_ReturnsBalances()
    {
        GetCoinhouseBalancesQuery query = new GetCoinhouseBalancesQuery(_persona);
        _mockCoinhouseRepo.Setup(r => r.GetAccountFor(It.IsAny<Guid>())).Returns(_testAccount);
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
        _testAccount.LastAccessedAt = lastAccessed;
        GetCoinhouseBalancesQuery query = new GetCoinhouseBalancesQuery(_persona);
        _mockCoinhouseRepo.Setup(r => r.GetAccountFor(It.IsAny<Guid>())).Returns(_testAccount);
        IReadOnlyList<BalanceDto> balances = await _handler.HandleAsync(query);
        Assert.That(balances[0].LastAccessedAt, Is.EqualTo(lastAccessed));
    }
    [Test]
    public async Task Given_AccountWithZeroBalance_When_QueryingBalances_Then_IncludesZeroBalance()
    {
        _testAccount.Debit = 0;
        _testAccount.Credit = 0;
        GetCoinhouseBalancesQuery query = new GetCoinhouseBalancesQuery(_persona);
        _mockCoinhouseRepo.Setup(r => r.GetAccountFor(It.IsAny<Guid>())).Returns(_testAccount);
        IReadOnlyList<BalanceDto> balances = await _handler.HandleAsync(query);
        Assert.That(balances, Has.Count.EqualTo(1));
        Assert.That(balances[0].Balance, Is.EqualTo(0));
    }
    [Test]
    public async Task Given_PersonaWithNoAccounts_When_QueryingBalances_Then_ReturnsEmptyList()
    {
        GetCoinhouseBalancesQuery query = new GetCoinhouseBalancesQuery(_persona);
        _mockCoinhouseRepo.Setup(r => r.GetAccountFor(It.IsAny<Guid>())).Returns((CoinHouseAccount?)null);
        IReadOnlyList<BalanceDto> balances = await _handler.HandleAsync(query);
        Assert.That(balances, Is.Not.Null);
        Assert.That(balances, Is.Empty);
    }
    [Test]
    public async Task Given_Query_When_ExecutingMultipleTimes_Then_ResultsAreConsistent()
    {
        GetCoinhouseBalancesQuery query = new GetCoinhouseBalancesQuery(_persona);
        _mockCoinhouseRepo.Setup(r => r.GetAccountFor(It.IsAny<Guid>())).Returns(_testAccount);
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
        _testAccount.Debit = 100;
        _testAccount.Credit = 500;
        GetCoinhouseBalancesQuery query = new GetCoinhouseBalancesQuery(_persona);
        _mockCoinhouseRepo.Setup(r => r.GetAccountFor(It.IsAny<Guid>())).Returns(_testAccount);
        IReadOnlyList<BalanceDto> balances = await _handler.HandleAsync(query);
        Assert.That(balances[0].Balance, Is.EqualTo(-400));
    }
}
