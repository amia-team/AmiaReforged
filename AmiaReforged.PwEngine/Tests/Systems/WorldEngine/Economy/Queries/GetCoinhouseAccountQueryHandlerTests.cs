using System;
using System.Collections.Generic;
using System.Linq;
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
    private Guid _ownerGuid;

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

        _ownerGuid = Guid.Parse(_persona.Value);

        _accountDto = new CoinhouseAccountDto
        {
            Id = PersonaAccountId.ForCoinhouse(_persona, _coinhouseTag),
            Debit = 5000,
            Credit = 1200,
            CoinHouseId = _coinhouseDto.Id,
            OpenedAt = DateTime.UtcNow.AddDays(-10),
            LastAccessedAt = DateTime.UtcNow.AddMinutes(-5),
            Coinhouse = _coinhouseDto,
            Holders = new[]
            {
                new CoinhouseAccountHolderDto
                {
                    HolderId = _ownerGuid,
                    Type = HolderType.Individual,
                    Role = HolderRole.Owner,
                    FirstName = "Owner",
                    LastName = string.Empty
                }
            }
        };

        _coinhouses
            .Setup(r => r.GetAccountForAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) =>
                id == _accountDto.Id ? _accountDto : null);

        _coinhouses
            .Setup(r => r.GetByIdAsync(It.IsAny<long>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((long id, CancellationToken _) =>
                id == _coinhouseDto.Id ? _coinhouseDto : null);

        _coinhouses
            .Setup(r => r.GetByTagAsync(It.IsAny<CoinhouseTag>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CoinhouseTag tag, CancellationToken _) =>
                string.Equals(tag.Value, _coinhouseTag.Value, StringComparison.OrdinalIgnoreCase)
                    ? _coinhouseDto
                    : null);
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
        Assert.That(result.Holders, Is.Not.Null);
        Assert.That(result.Holders, Has.Count.EqualTo(1));
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

        _coinhouses
            .Setup(r => r.GetByTagAsync(It.IsAny<CoinhouseTag>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CoinhouseDto?)null);

        GetCoinhouseAccountQuery query = new(_persona, _coinhouseTag);
        CoinhouseAccountQueryResult? result = await _handler.HandleAsync(query);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task GivenSharedHolder_WhenQuerying_ReturnsSummary()
    {
        PersonaId sharedPersona = PersonaId.FromCharacter(CharacterId.New());
        Guid sharedGuid = Guid.Parse(sharedPersona.Value);

        CoinhouseAccountDto sharedAccount = _accountDto with
        {
            Holders = new[]
            {
                new CoinhouseAccountHolderDto
                {
                    HolderId = _ownerGuid,
                    Type = HolderType.Individual,
                    Role = HolderRole.Owner,
                    FirstName = "Owner",
                    LastName = string.Empty
                },
                new CoinhouseAccountHolderDto
                {
                    HolderId = sharedGuid,
                    Type = HolderType.Individual,
                    Role = HolderRole.AuthorizedUser,
                    FirstName = "Shared",
                    LastName = string.Empty
                }
            }
        };

        _coinhouses
            .Setup(r => r.GetAccountForAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, CancellationToken _) => id == sharedAccount.Id ? sharedAccount : null);

        _coinhouses
            .Setup(r => r.GetAccountsForHolderAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid holderId, CancellationToken _) =>
                holderId == sharedGuid
                    ? new List<CoinhouseAccountDto> { sharedAccount }
                    : (IReadOnlyList<CoinhouseAccountDto>)new List<CoinhouseAccountDto>());

        GetCoinhouseAccountQuery query = new(sharedPersona, _coinhouseTag);

        CoinhouseAccountQueryResult? result = await _handler.HandleAsync(query);

        Assert.That(result, Is.Not.Null);
        Assert.That(result!.AccountExists, Is.True);
        Assert.That(result.Account, Is.Not.Null);
        Assert.That(result.Holders.Any(h => h.HolderId == sharedGuid), Is.True);
    }
}
