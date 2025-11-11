using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Application.Organizations.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Tests.Systems.WorldEngine.Helpers;
using NUnit.Framework;
using OrgEntity = AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Organizations.Organization;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Organizations.Tests;

[TestFixture]
public class OrganizationCommandTests
{
    private IOrganizationRepository _orgRepository = null!;
    private IOrganizationMemberRepository _memberRepository = null!;
    private IEventBus _eventBus = null!;
    private AddMemberHandler _addMemberHandler = null!;
    private RemoveMemberHandler _removeMemberHandler = null!;
    private AssignRoleHandler _assignRoleHandler = null!;
    private RevokeRoleHandler _revokeRoleHandler = null!;

    private OrganizationId _testOrgId;
    private CharacterId _testCharacterId;
    private CharacterId _bannedCharacterId;

    [SetUp]
    public void SetUp()
    {
        // Arrange - Create repositories
        _orgRepository = new InMemoryOrganizationRepository();
        _memberRepository = new InMemoryOrganizationMemberRepository();
        _eventBus = new InMemoryEventBus();

        // Arrange - Create handlers
        _addMemberHandler = new AddMemberHandler(_memberRepository, _orgRepository, _eventBus);
        _removeMemberHandler = new RemoveMemberHandler(_memberRepository, _eventBus);
        _assignRoleHandler = new AssignRoleHandler(_memberRepository);
        _revokeRoleHandler = new RevokeRoleHandler(_memberRepository);

        // Arrange - Create test data
        _testOrgId = OrganizationId.New();
        _testCharacterId = new CharacterId(Guid.NewGuid());
        _bannedCharacterId = new CharacterId(Guid.NewGuid());

        OrgEntity testOrg = OrgEntity.Create(_testOrgId, "Test Guild", "A test guild", OrganizationType.Guild);
        testOrg.BanList.Add(_bannedCharacterId);
        _orgRepository.Add(testOrg);
    }

    #region AddMember Command Tests

    [Test]
    public async Task AddMember_Success_CreatesActiveMembership()
    {
        // Arrange
        AddMemberCommand command = new AddMemberCommand
        {
            OrganizationId = _testOrgId,
            CharacterId = _testCharacterId,
            InitialRank = OrganizationRank.Recruit,
            InitialRoles = []
        };

        // Act
        CommandResult result = await _addMemberHandler.HandleAsync(command);

        // Assert
        Assert.That(result.Success, Is.True, "Command should succeed");

        OrganizationMember? member = _memberRepository.GetByCharacterAndOrganization(_testCharacterId, _testOrgId);
        Assert.That(member, Is.Not.Null, "Member should be created");
        Assert.That(member!.Rank, Is.EqualTo(OrganizationRank.Recruit));
        Assert.That(member.Status, Is.EqualTo(MembershipStatus.Active));
    }

    [Test]
    public async Task AddMember_AlreadyMember_Fails()
    {
        // Arrange - Add member first time
        OrganizationMember member = new OrganizationMember
        {
            Id = Guid.NewGuid(),
            CharacterId = _testCharacterId,
            OrganizationId = _testOrgId,
            Rank = OrganizationRank.Member,
            Status = MembershipStatus.Active,
            JoinedDate = DateTime.UtcNow,
            Roles = []
        };
        _memberRepository.Add(member);

        AddMemberCommand command = new AddMemberCommand
        {
            OrganizationId = _testOrgId,
            CharacterId = _testCharacterId,
            InitialRank = OrganizationRank.Recruit
        };

        // Act
        CommandResult result = await _addMemberHandler.HandleAsync(command);

        // Assert
        Assert.That(result.Success, Is.False, "Should fail - already a member");
        Assert.That(result.ErrorMessage, Does.Contain("already").IgnoreCase);
    }

    [Test]
    public async Task AddMember_BannedCharacter_Fails()
    {
        // Arrange
        AddMemberCommand command = new AddMemberCommand
        {
            OrganizationId = _testOrgId,
            CharacterId = _bannedCharacterId,
            InitialRank = OrganizationRank.Recruit
        };

        // Act
        CommandResult result = await _addMemberHandler.HandleAsync(command);

        // Assert
        Assert.That(result.Success, Is.False, "Should fail - character is banned");
        Assert.That(result.ErrorMessage, Does.Contain("banned").IgnoreCase);
    }

    [Test]
    public async Task AddMember_OrganizationNotFound_Fails()
    {
        // Arrange
        OrganizationId nonExistentOrgId = OrganizationId.New();
        AddMemberCommand command = new AddMemberCommand
        {
            OrganizationId = nonExistentOrgId,
            CharacterId = _testCharacterId,
            InitialRank = OrganizationRank.Recruit
        };

        // Act
        CommandResult result = await _addMemberHandler.HandleAsync(command);

        // Assert
        Assert.That(result.Success, Is.False, "Should fail - organization doesn't exist");
        Assert.That(result.ErrorMessage, Does.Contain("not found").IgnoreCase);
    }

    #endregion

    #region RemoveMember Command Tests

    [Test]
    public async Task RemoveMember_Success_SetsDepartedStatus()
    {
        // Arrange - Create member first
        OrganizationMember member = new OrganizationMember
        {
            Id = Guid.NewGuid(),
            CharacterId = _testCharacterId,
            OrganizationId = _testOrgId,
            Rank = OrganizationRank.Member,
            Status = MembershipStatus.Active,
            JoinedDate = DateTime.UtcNow,
            Roles = []
        };
        _memberRepository.Add(member);

        RemoveMemberCommand command = new RemoveMemberCommand
        {
            OrganizationId = _testOrgId,
            CharacterId = _testCharacterId,
            RemovedBy = _testCharacterId
        };

        // Act
        CommandResult result = await _removeMemberHandler.HandleAsync(command);

        // Assert
        Assert.That(result.Success, Is.True, "Command should succeed");

        OrganizationMember? updatedMember = _memberRepository.GetByCharacterAndOrganization(_testCharacterId, _testOrgId);
        Assert.That(updatedMember, Is.Not.Null);
        Assert.That(updatedMember!.Status, Is.EqualTo(MembershipStatus.Departed));
        Assert.That(updatedMember.DepartedDate, Is.Not.Null);
    }

    [Test]
    public async Task RemoveMember_NotAMember_Fails()
    {
        // Arrange
        RemoveMemberCommand command = new RemoveMemberCommand
        {
            OrganizationId = _testOrgId,
            CharacterId = _testCharacterId,
            RemovedBy = _testCharacterId
        };

        // Act
        CommandResult result = await _removeMemberHandler.HandleAsync(command);

        // Assert
        Assert.That(result.Success, Is.False, "Should fail - not a member");
        Assert.That(result.ErrorMessage, Does.Contain("not").IgnoreCase);
    }

    #endregion

    #region AssignRole Command Tests

    [Test]
    public async Task AssignRole_Success_AddsRoleToMember()
    {
        // Arrange - Create member
        OrganizationMember member = new OrganizationMember
        {
            Id = Guid.NewGuid(),
            CharacterId = _testCharacterId,
            OrganizationId = _testOrgId,
            Rank = OrganizationRank.Officer,
            Status = MembershipStatus.Active,
            JoinedDate = DateTime.UtcNow,
            Roles = []
        };
        _memberRepository.Add(member);

        AssignRoleCommand command = new AssignRoleCommand
        {
            OrganizationId = _testOrgId,
            CharacterId = _testCharacterId,
            Role = MemberRole.Treasurer,
            AssignedBy = _testCharacterId
        };

        // Act
        CommandResult result = await _assignRoleHandler.HandleAsync(command);

        // Assert
        Assert.That(result.Success, Is.True, "Command should succeed");

        OrganizationMember? updatedMember = _memberRepository.GetByCharacterAndOrganization(_testCharacterId, _testOrgId);
        Assert.That(updatedMember!.HasRole(MemberRole.Treasurer), Is.True);
    }

    [Test]
    public async Task AssignRole_AlreadyHasRole_Fails()
    {
        // Arrange - Create member with role
        OrganizationMember member = new OrganizationMember
        {
            Id = Guid.NewGuid(),
            CharacterId = _testCharacterId,
            OrganizationId = _testOrgId,
            Rank = OrganizationRank.Officer,
            Status = MembershipStatus.Active,
            JoinedDate = DateTime.UtcNow,
            Roles = [MemberRole.Treasurer]
        };
        _memberRepository.Add(member);

        AssignRoleCommand command = new AssignRoleCommand
        {
            OrganizationId = _testOrgId,
            CharacterId = _testCharacterId,
            Role = MemberRole.Treasurer,
            AssignedBy = _testCharacterId
        };

        // Act
        CommandResult result = await _assignRoleHandler.HandleAsync(command);

        // Assert
        Assert.That(result.Success, Is.False, "Should fail - already has role");
        Assert.That(result.ErrorMessage, Does.Contain("already").IgnoreCase);
    }

    #endregion

    #region RevokeRole Command Tests

    [Test]
    public async Task RevokeRole_Success_RemovesRoleFromMember()
    {
        // Arrange - Create member with role
        OrganizationMember member = new OrganizationMember
        {
            Id = Guid.NewGuid(),
            CharacterId = _testCharacterId,
            OrganizationId = _testOrgId,
            Rank = OrganizationRank.Officer,
            Status = MembershipStatus.Active,
            JoinedDate = DateTime.UtcNow,
            Roles = [MemberRole.Treasurer, MemberRole.Recruiter]
        };
        _memberRepository.Add(member);

        RevokeRoleCommand command = new RevokeRoleCommand
        {
            OrganizationId = _testOrgId,
            CharacterId = _testCharacterId,
            Role = MemberRole.Treasurer,
            RevokedBy = _testCharacterId
        };

        // Act
        CommandResult result = await _revokeRoleHandler.HandleAsync(command);

        // Assert
        Assert.That(result.Success, Is.True, "Command should succeed");

        OrganizationMember? updatedMember = _memberRepository.GetByCharacterAndOrganization(_testCharacterId, _testOrgId);
        Assert.That(updatedMember!.HasRole(MemberRole.Treasurer), Is.False);
        Assert.That(updatedMember.HasRole(MemberRole.Recruiter), Is.True, "Other roles should remain");
    }

    [Test]
    public async Task RevokeRole_DoesNotHaveRole_Fails()
    {
        // Arrange - Create member without the role
        OrganizationMember member = new OrganizationMember
        {
            Id = Guid.NewGuid(),
            CharacterId = _testCharacterId,
            OrganizationId = _testOrgId,
            Rank = OrganizationRank.Officer,
            Status = MembershipStatus.Active,
            JoinedDate = DateTime.UtcNow,
            Roles = [MemberRole.Recruiter]
        };
        _memberRepository.Add(member);

        RevokeRoleCommand command = new RevokeRoleCommand
        {
            OrganizationId = _testOrgId,
            CharacterId = _testCharacterId,
            Role = MemberRole.Treasurer,
            RevokedBy = _testCharacterId
        };

        // Act
        CommandResult result = await _revokeRoleHandler.HandleAsync(command);

        // Assert
        Assert.That(result.Success, Is.False, "Should fail - doesn't have role");
        Assert.That(result.ErrorMessage, Does.Contain("does not have").IgnoreCase);
    }

    #endregion
}

