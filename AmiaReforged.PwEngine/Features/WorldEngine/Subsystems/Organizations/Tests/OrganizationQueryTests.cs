using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Application.Organizations.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Tests.Systems.WorldEngine.Helpers;
using NUnit.Framework;
using OrgEntity = AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Organizations.Organization;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Organizations.Tests;

[TestFixture]
public class OrganizationQueryTests
{
    private IOrganizationRepository _orgRepository = null!;
    private IOrganizationMemberRepository _memberRepository = null!;
    private GetOrganizationDetailsHandler _getDetailsHandler = null!;
    private GetOrganizationMembersHandler _getMembersHandler = null!;
    private GetCharacterOrganizationsHandler _getCharacterOrgsHandler = null!;

    private OrganizationId _testOrgId;
    private CharacterId _testCharacterId;

    [SetUp]
    public void SetUp()
    {
        // Arrange - Create repositories
        _orgRepository = new InMemoryOrganizationRepository();
        _memberRepository = new InMemoryOrganizationMemberRepository();

        // Arrange - Create handlers
        _getDetailsHandler = new GetOrganizationDetailsHandler(_orgRepository);
        _getMembersHandler = new GetOrganizationMembersHandler(_memberRepository);
        _getCharacterOrgsHandler = new GetCharacterOrganizationsHandler(_memberRepository, _orgRepository);

        // Arrange - Create test data
        _testOrgId = OrganizationId.New();
        _testCharacterId = new CharacterId(Guid.NewGuid());

        OrgEntity testOrg = OrgEntity.Create(_testOrgId, "Test Guild", "A test organization", OrganizationType.Guild);
        _orgRepository.Add(testOrg);
    }

    #region GetOrganizationDetails Query Tests

    [Test]
    public async Task GetOrganizationDetails_Found_ReturnsOrganization()
    {
        // Arrange
        GetOrganizationDetailsQuery query = new GetOrganizationDetailsQuery
        {
            OrganizationId = _testOrgId
        };

        // Act
        IOrganization? result = await _getDetailsHandler.HandleAsync(query);

        // Assert
        Assert.That(result, Is.Not.Null, "Query should return organization");
        Assert.That(result!.Name, Is.EqualTo("Test Guild"));
        Assert.That(result.Type, Is.EqualTo(OrganizationType.Guild));
    }

    [Test]
    public async Task GetOrganizationDetails_NotFound_ReturnsNull()
    {
        // Arrange
        OrganizationId nonExistentId = OrganizationId.New();
        GetOrganizationDetailsQuery query = new GetOrganizationDetailsQuery
        {
            OrganizationId = nonExistentId
        };

        // Act
        IOrganization? result = await _getDetailsHandler.HandleAsync(query);

        // Assert
        Assert.That(result, Is.Null, "Query should return null for non-existent org");
    }

    #endregion

    #region GetOrganizationMembers Query Tests

    [Test]
    public async Task GetOrganizationMembers_ReturnsAllActiveMembers()
    {
        // Arrange - Create multiple members
        OrganizationMember member1 = new OrganizationMember
        {
            Id = Guid.NewGuid(),
            CharacterId = _testCharacterId,
            OrganizationId = _testOrgId,
            Rank = OrganizationRank.Leader,
            Status = MembershipStatus.Active,
            JoinedDate = DateTime.UtcNow,
            Roles = []
        };

        OrganizationMember member2 = new OrganizationMember
        {
            Id = Guid.NewGuid(),
            CharacterId = new CharacterId(Guid.NewGuid()),
            OrganizationId = _testOrgId,
            Rank = OrganizationRank.Member,
            Status = MembershipStatus.Active,
            JoinedDate = DateTime.UtcNow,
            Roles = []
        };

        OrganizationMember member3 = new OrganizationMember
        {
            Id = Guid.NewGuid(),
            CharacterId = new CharacterId(Guid.NewGuid()),
            OrganizationId = _testOrgId,
            Rank = OrganizationRank.Member,
            Status = MembershipStatus.Departed,
            JoinedDate = DateTime.UtcNow.AddMonths(-6),
            DepartedDate = DateTime.UtcNow,
            Roles = []
        };

        _memberRepository.Add(member1);
        _memberRepository.Add(member2);
        _memberRepository.Add(member3);

        GetOrganizationMembersQuery query = new GetOrganizationMembersQuery
        {
            OrganizationId = _testOrgId,
            ActiveOnly = true
        };

        // Act
        List<OrganizationMember> result = await _getMembersHandler.HandleAsync(query);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count, Is.EqualTo(2), "Should only return active members");
        Assert.That(result.All(m => m.Status == MembershipStatus.Active), Is.True);
    }

    [Test]
    public async Task GetOrganizationMembers_IncludesInactive_ReturnsAll()
    {
        // Arrange - Create members with different statuses
        OrganizationMember member1 = new OrganizationMember
        {
            Id = Guid.NewGuid(),
            CharacterId = _testCharacterId,
            OrganizationId = _testOrgId,
            Rank = OrganizationRank.Member,
            Status = MembershipStatus.Active,
            JoinedDate = DateTime.UtcNow,
            Roles = []
        };

        OrganizationMember member2 = new OrganizationMember
        {
            Id = Guid.NewGuid(),
            CharacterId = new CharacterId(Guid.NewGuid()),
            OrganizationId = _testOrgId,
            Rank = OrganizationRank.Member,
            Status = MembershipStatus.Departed,
            JoinedDate = DateTime.UtcNow.AddMonths(-6),
            DepartedDate = DateTime.UtcNow,
            Roles = []
        };

        _memberRepository.Add(member1);
        _memberRepository.Add(member2);

        GetOrganizationMembersQuery query = new GetOrganizationMembersQuery
        {
            OrganizationId = _testOrgId,
            ActiveOnly = false
        };

        // Act
        List<OrganizationMember> result = await _getMembersHandler.HandleAsync(query);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count, Is.EqualTo(2), "Should return all members");
    }

    [Test]
    public async Task GetOrganizationMembers_EmptyOrganization_ReturnsEmptyList()
    {
        // Arrange
        GetOrganizationMembersQuery query = new GetOrganizationMembersQuery
        {
            OrganizationId = _testOrgId,
            ActiveOnly = true
        };

        // Act
        List<OrganizationMember> result = await _getMembersHandler.HandleAsync(query);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count, Is.EqualTo(0));
    }

    #endregion

    #region GetCharacterOrganizations Query Tests

    [Test]
    public async Task GetCharacterOrganizations_ReturnsMemberships()
    {
        // Arrange - Create memberships in multiple organizations
        OrganizationId org2Id = OrganizationId.New();
        OrgEntity org2 = OrgEntity.Create(org2Id, "Second Guild", "Another guild", OrganizationType.Guild);
        _orgRepository.Add(org2);

        OrganizationMember member1 = new OrganizationMember
        {
            Id = Guid.NewGuid(),
            CharacterId = _testCharacterId,
            OrganizationId = _testOrgId,
            Rank = OrganizationRank.Leader,
            Status = MembershipStatus.Active,
            JoinedDate = DateTime.UtcNow,
            Roles = []
        };

        OrganizationMember member2 = new OrganizationMember
        {
            Id = Guid.NewGuid(),
            CharacterId = _testCharacterId,
            OrganizationId = org2Id,
            Rank = OrganizationRank.Member,
            Status = MembershipStatus.Active,
            JoinedDate = DateTime.UtcNow,
            Roles = []
        };

        _memberRepository.Add(member1);
        _memberRepository.Add(member2);

        GetCharacterOrganizationsQuery query = new GetCharacterOrganizationsQuery
        {
            CharacterId = _testCharacterId,
            ActiveOnly = true
        };

        // Act
        List<OrganizationMember> result = await _getCharacterOrgsHandler.HandleAsync(query);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count, Is.EqualTo(2), "Should return both memberships");
    }

    [Test]
    public async Task GetCharacterOrganizations_ActiveOnly_ExcludesDeparted()
    {
        // Arrange
        OrganizationMember member1 = new OrganizationMember
        {
            Id = Guid.NewGuid(),
            CharacterId = _testCharacterId,
            OrganizationId = _testOrgId,
            Rank = OrganizationRank.Member,
            Status = MembershipStatus.Active,
            JoinedDate = DateTime.UtcNow,
            Roles = []
        };

        OrganizationId org2Id = OrganizationId.New();
        OrgEntity org2 = OrgEntity.Create(org2Id, "Old Guild", "Former guild", OrganizationType.Guild);
        _orgRepository.Add(org2);

        OrganizationMember member2 = new OrganizationMember
        {
            Id = Guid.NewGuid(),
            CharacterId = _testCharacterId,
            OrganizationId = org2Id,
            Rank = OrganizationRank.Member,
            Status = MembershipStatus.Departed,
            JoinedDate = DateTime.UtcNow.AddYears(-1),
            DepartedDate = DateTime.UtcNow.AddMonths(-6),
            Roles = []
        };

        _memberRepository.Add(member1);
        _memberRepository.Add(member2);

        GetCharacterOrganizationsQuery query = new GetCharacterOrganizationsQuery
        {
            CharacterId = _testCharacterId,
            ActiveOnly = true
        };

        // Act
        List<OrganizationMember> result = await _getCharacterOrgsHandler.HandleAsync(query);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count, Is.EqualTo(1), "Should only return active membership");
        Assert.That(result[0].Status, Is.EqualTo(MembershipStatus.Active));
    }

    [Test]
    public async Task GetCharacterOrganizations_NoMemberships_ReturnsEmptyList()
    {
        // Arrange
        GetCharacterOrganizationsQuery query = new GetCharacterOrganizationsQuery
        {
            CharacterId = new CharacterId(Guid.NewGuid()),
            ActiveOnly = true
        };

        // Act
        List<OrganizationMember> result = await _getCharacterOrgsHandler.HandleAsync(query);

        // Assert
        Assert.That(result, Is.Not.Null);
        Assert.That(result.Count, Is.EqualTo(0));
    }

    #endregion
}

