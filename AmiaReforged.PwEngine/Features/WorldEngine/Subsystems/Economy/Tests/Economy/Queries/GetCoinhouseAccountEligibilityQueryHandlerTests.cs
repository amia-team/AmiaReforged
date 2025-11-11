using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Accounts;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Banks.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Organizations;
using AmiaReforged.PwEngine.Features.WorldEngine.Tests.Helpers.WorldEngine;
using Moq;
using NUnit.Framework;
using Organization = AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Organizations.Organization;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Tests.Economy.Queries;

[TestFixture]
public class GetCoinhouseAccountEligibilityQueryHandlerTests
{
    private Mock<ICoinhouseRepository> _coinhouses = null!;
    private Mock<IOrganizationMemberRepository> _organizationMembers = null!;
    private Mock<IOrganizationRepository> _organizations = null!;
    private GetCoinhouseAccountEligibilityQueryHandler _handler = null!;

    private CoinhouseTag _coinhouseTag;
    private CoinhouseDto _coinhouse = null!;

    [SetUp]
    public void SetUp()
    {
        _coinhouses = new Mock<ICoinhouseRepository>();
        _organizationMembers = new Mock<IOrganizationMemberRepository>();
        _organizations = new Mock<IOrganizationRepository>();

        _handler = new GetCoinhouseAccountEligibilityQueryHandler(
            _coinhouses.Object,
            _organizationMembers.Object,
            _organizations.Object);

        _coinhouseTag = EconomyTestHelpers.CreateCoinhouseTag("cordor_bank");
        _coinhouse = new CoinhouseDto
        {
            Id = 1,
            Tag = _coinhouseTag,
            Settlement = 10,
            EngineId = Guid.NewGuid(),
            Persona = PersonaId.FromCoinhouse(_coinhouseTag)
        };

        _coinhouses
            .Setup(r => r.GetByTagAsync(_coinhouseTag, It.IsAny<CancellationToken>()))
            .ReturnsAsync(_coinhouse);

        _coinhouses
            .Setup(r => r.GetAccountForAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((CoinhouseAccountDto?)null);
    }

    [Test]
    public async Task Given_UnknownCoinhouse_When_QueryingEligibility_Then_ReportsUnavailable()
    {
        PersonaId persona = PersonaTestHelpers.CreateCharacterPersona().Id;
        _coinhouses
            .Setup(r => r.GetByTagAsync(_coinhouseTag, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CoinhouseDto?)null);

        GetCoinhouseAccountEligibilityQuery query = new(persona, _coinhouseTag);
        CoinhouseAccountEligibilityResult result = await _handler.HandleAsync(query);

        Assert.That(result.CoinhouseExists, Is.False);
        Assert.That(result.CanOpenPersonalAccount, Is.False);
        Assert.That(result.CoinhouseError, Does.Contain("could not be found"));
    }

    [Test]
    public async Task Given_CharacterWithoutAccount_When_QueryingEligibility_Then_PersonalAccountAllowed()
    {
        PersonaId persona = PersonaTestHelpers.CreateCharacterPersona().Id;
        Guid expectedAccountId = PersonaAccountId.ForCoinhouse(persona, _coinhouseTag);

        _coinhouses
            .Setup(r => r.GetAccountForAsync(expectedAccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CoinhouseAccountDto?)null);

        GetCoinhouseAccountEligibilityQuery query = new(persona, _coinhouseTag);
        CoinhouseAccountEligibilityResult result = await _handler.HandleAsync(query);

        Assert.That(result.CoinhouseExists, Is.True);
        Assert.That(result.CanOpenPersonalAccount, Is.True);
        Assert.That(result.PersonalAccountBlockedReason, Is.Null);
    }

    [Test]
    public async Task Given_ExistingPersonalAccount_When_QueryingEligibility_Then_PersonalAccountBlocked()
    {
        PersonaId persona = PersonaTestHelpers.CreateCharacterPersona().Id;
        Guid expectedAccountId = PersonaAccountId.ForCoinhouse(persona, _coinhouseTag);

        CoinhouseAccountDto existingAccount = new CoinhouseAccountDto
        {
            Id = expectedAccountId,
            Debit = 100,
            Credit = 0,
            CoinHouseId = _coinhouse.Id,
            OpenedAt = DateTime.UtcNow.AddDays(-1),
            LastAccessedAt = DateTime.UtcNow,
            Coinhouse = _coinhouse
        };

        _coinhouses
            .Setup(r => r.GetAccountForAsync(expectedAccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingAccount);

        GetCoinhouseAccountEligibilityQuery query = new(persona, _coinhouseTag);
        CoinhouseAccountEligibilityResult result = await _handler.HandleAsync(query);

        Assert.That(result.CanOpenPersonalAccount, Is.False);
        Assert.That(result.PersonalAccountBlockedReason, Does.Contain("already maintain"));
    }

    [Test]
    public async Task Given_OrganizationLeader_When_QueryingEligibility_Then_OrganizationOptionProvided()
    {
        PersonaId requestor = PersonaTestHelpers.CreateCharacterPersona().Id;
        Guid requestorGuid = Guid.Parse(requestor.Value);
        CharacterId characterId = CharacterId.From(requestorGuid);

        OrganizationId organizationId = OrganizationId.New();
        OrganizationMember leaderMembership = new OrganizationMember
        {
            Id = Guid.NewGuid(),
            CharacterId = characterId,
            OrganizationId = organizationId,
            Rank = OrganizationRank.Leader,
            Status = MembershipStatus.Active,
            JoinedDate = DateTime.UtcNow
        };

        IOrganization organization = Organization.Create(
            organizationId,
            "Merchants Guild",
            "Guild for traders",
            OrganizationType.Guild);

        _organizationMembers
            .Setup(r => r.GetByCharacter(characterId))
            .Returns(new List<OrganizationMember> { leaderMembership });

        _organizations
            .Setup(r => r.GetById(organizationId))
            .Returns(organization);

        PersonaId organizationPersona = PersonaId.FromOrganization(organizationId);
        Guid organizationAccountId = PersonaAccountId.ForCoinhouse(organizationPersona, _coinhouseTag);

        _coinhouses
            .Setup(r => r.GetAccountForAsync(organizationAccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((CoinhouseAccountDto?)null);

        GetCoinhouseAccountEligibilityQuery query = new(requestor, _coinhouseTag);
        CoinhouseAccountEligibilityResult result = await _handler.HandleAsync(query);

        Assert.That(result.Organizations, Has.Count.EqualTo(1));
        OrganizationAccountEligibility option = result.Organizations[0];
        Assert.That(option.OrganizationId, Is.EqualTo(organizationId));
        Assert.That(option.CanOpen, Is.True);
        Assert.That(option.AlreadyHasAccount, Is.False);
        Assert.That(option.BlockedReason, Is.Null);
    }

    [Test]
    public async Task Given_OrganizationWithExistingAccount_When_QueryingEligibility_Then_OrganizationBlocked()
    {
        PersonaId requestor = PersonaTestHelpers.CreateCharacterPersona().Id;
        Guid requestorGuid = Guid.Parse(requestor.Value);
        CharacterId characterId = CharacterId.From(requestorGuid);

        OrganizationId organizationId = OrganizationId.New();
        OrganizationMember leaderMembership = new OrganizationMember
        {
            Id = Guid.NewGuid(),
            CharacterId = characterId,
            OrganizationId = organizationId,
            Rank = OrganizationRank.Leader,
            Status = MembershipStatus.Active,
            JoinedDate = DateTime.UtcNow
        };

        IOrganization organization = Organization.Create(
            organizationId,
            "Carpenters Guild",
            "Guild for carpenters",
            OrganizationType.Guild);

        _organizationMembers
            .Setup(r => r.GetByCharacter(characterId))
            .Returns(new List<OrganizationMember> { leaderMembership });

        _organizations
            .Setup(r => r.GetById(organizationId))
            .Returns(organization);

        PersonaId organizationPersona = PersonaId.FromOrganization(organizationId);
        Guid organizationAccountId = PersonaAccountId.ForCoinhouse(organizationPersona, _coinhouseTag);
        CoinhouseAccountDto existingAccount = new CoinhouseAccountDto
        {
            Id = organizationAccountId,
            Debit = 0,
            Credit = 0,
            CoinHouseId = _coinhouse.Id,
            OpenedAt = DateTime.UtcNow,
            LastAccessedAt = DateTime.UtcNow,
            Coinhouse = _coinhouse
        };

        _coinhouses
            .Setup(r => r.GetAccountForAsync(organizationAccountId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingAccount);

        GetCoinhouseAccountEligibilityQuery query = new(requestor, _coinhouseTag);
        CoinhouseAccountEligibilityResult result = await _handler.HandleAsync(query);

        Assert.That(result.Organizations, Has.Count.EqualTo(1));
        OrganizationAccountEligibility option = result.Organizations[0];
        Assert.That(option.CanOpen, Is.False);
        Assert.That(option.AlreadyHasAccount, Is.True);
        Assert.That(option.BlockedReason, Does.Contain("already maintains"));
    }
}
