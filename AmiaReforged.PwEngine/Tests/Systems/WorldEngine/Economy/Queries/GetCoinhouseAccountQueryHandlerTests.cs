using System;
using System.Threading;
using System.Threading.Tasks;
using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Accounts;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using Moq;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.Economy.Queries;

[TestFixture]
public class GetCoinhouseAccountQueryHandlerTests
{
    private Mock<ICoinhouseRepository> _coinhouses = null!;
    private GetCoinhouseAccountQueryHandler _handler = null!;

    private PersonaId _persona;
    private CoinhouseTag _coinhouseTag;
    private CoinhouseDto _coinhouseDto = null!;
    private CoinhouseAccountDto _accountDto = null!;

    [SetUp]
    public void Setup()
    {
        _coinhouses = new Mock<ICoinhouseRepository>();
        _handler = new GetCoinhouseAccountQueryHandler(_coinhouses.Object);

        _persona = PersonaId.FromCharacter(CharacterId.New());
        _coinhouseTag = new CoinhouseTag("test-bank");

        _coinhouseDto = new CoinhouseDto
        {
            Id = 123,
            Tag = _coinhouseTag,
            Settlement = 7,
            EngineId = Guid.NewGuid(),
            Persona = PersonaId.FromCoinhouse(_coinhouseTag)
        };

        _accountDto = new CoinhouseAccountDto
        {
            Id = PersonaAccountId.From(_persona),
            Debit = 5000,
            Credit = 1200,
            CoinHouseId = _coinhouseDto.Id,
            OpenedAt = DateTime.UtcNow.AddDays(-10),
            LastAccessedAt = DateTime.UtcNow.AddMinutes(-5),
            Coinhouse = _coinhouseDto
        };

        _coinhouses
            .Setup(r => r.GetAccountForAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) =>
                id == _accountDto.Id ? _accountDto : null);

        _coinhouses
            .Setup(r => r.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((long id, CancellationToken _) =>
                id == _coinhouseDto.Id ? _coinhouseDto : null);
    }

    [Test]
    public async Task GivenExistingAccount_WhenQueryMatchesCoinhouse_ReturnsSummary()
    {
        GetCoinhouseAccountQuery query = new(_persona, _coinhouseTag);

        CoinhouseAccountQueryResult? result = await _handler.HandleAsync(query);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.AccountExists, Is.True);
        Assert.That(result.Account, Is.Not.Null);
        Assert.That(result.Account!.CoinhouseId, Is.EqualTo(_coinhouseDto.Id));
        Assert.That(result.Account.CoinhouseTag.Value, Is.EqualTo(_coinhouseTag.Value));
        Assert.That(result.Account.Debit, Is.EqualTo(_accountDto.Debit));
        Assert.That(result.Account.Credit, Is.EqualTo(_accountDto.Credit));
        Assert.That(result.Account.LastAccessedAt, Is.EqualTo(_accountDto.LastAccessedAt));
    }

    [Test]
    public async Task GivenAccountWithoutCoinhouseProjection_WhenRepositoryProvidesCoinhouse_ReturnsSummary()
    {
        CoinhouseAccountDto accountWithoutProjection = _accountDto with { Coinhouse = null };

        _coinhouses
            .Setup(r => r.GetAccountForAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(accountWithoutProjection);

        GetCoinhouseAccountQuery query = new(_persona, _coinhouseTag);
        CoinhouseAccountQueryResult? result = await _handler.HandleAsync(query);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.AccountExists, Is.True);
        Assert.That(result.Account, Is.Not.Null);
        Assert.That(result.Account!.CoinhouseTag.Value, Is.EqualTo(_coinhouseTag.Value));
    }

    [Test]
    public async Task GivenAccountForDifferentCoinhouse_WhenQueryingTargetCoinhouse_ReturnsNull()
    {
        CoinhouseDto otherCoinhouse = _coinhouseDto with { Id = 987, Tag = new CoinhouseTag("other-bank") };
        CoinhouseAccountDto mismatchedAccount = _accountDto with
        {
            Coinhouse = otherCoinhouse,
            CoinHouseId = otherCoinhouse.Id
        };

        _coinhouses
            .Setup(r => r.GetAccountForAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(mismatchedAccount);

        GetCoinhouseAccountQuery query = new(_persona, _coinhouseTag);
        CoinhouseAccountQueryResult? result = await _handler.HandleAsync(query);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GivenMissingAccount_WhenQuerying_ReturnsNull()
    {
        _coinhouses
            .Setup(r => r.GetAccountForAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CoinhouseAccountDto?)null);

        GetCoinhouseAccountQuery query = new(_persona, _coinhouseTag);
        CoinhouseAccountQueryResult? result = await _handler.HandleAsync(query);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GivenCoinhouseNotFound_WhenQuerying_ReturnsNull()
    {
        CoinhouseAccountDto accountWithoutProjection = _accountDto with { Coinhouse = null };

        _coinhouses
            .Setup(r => r.GetAccountForAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(accountWithoutProjection);

        _coinhouses
            .Setup(r => r.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CoinhouseDto?)null);

        GetCoinhouseAccountQuery query = new(_persona, _coinhouseTag);
        CoinhouseAccountQueryResult? result = await _handler.HandleAsync(query);

        Assert.That(result, Is.Null);
    }
}
