using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Organizations;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.Organizations;

[TestFixture]
public class OrganizationMembershipTests
{
    private OrganizationId _orgId;
    private CharacterId _characterId;
    private OrganizationMember _member = null!;

    [SetUp]
    public void SetUp()
    {
        _orgId = OrganizationId.New();
        _characterId = new CharacterId(Guid.NewGuid());

        _member = new OrganizationMember
        {
            Id = Guid.NewGuid(),
            CharacterId = _characterId,
            OrganizationId = _orgId,
            Rank = OrganizationRank.Member,
            Status = MembershipStatus.Active,
            JoinedDate = DateTime.UtcNow,
            Roles = []
        };
    }

    [Test]
    public void OrganizationMember_CanBeCreated()
    {
        // Assert
        Assert.That(_member.CharacterId, Is.EqualTo(_characterId));
        Assert.That(_member.OrganizationId, Is.EqualTo(_orgId));
        Assert.That(_member.Rank, Is.EqualTo(OrganizationRank.Member));
        Assert.That(_member.Status, Is.EqualTo(MembershipStatus.Active));
    }

    [Test]
    public void OrganizationMember_CanHaveRoles()
    {
        // Arrange
        _member.Roles.Add(MemberRole.Treasurer);
        _member.Roles.Add(MemberRole.Recruiter);

        // Assert
        Assert.That(_member.Roles, Has.Count.EqualTo(2));
        Assert.That(_member.HasRole(MemberRole.Treasurer), Is.True);
        Assert.That(_member.HasRole(MemberRole.Diplomat), Is.False);
    }

    [Test]
    public void OrganizationMember_OfficerCanManageMembers()
    {
        // Arrange
        _member.Rank = OrganizationRank.Officer;

        // Assert
        Assert.That(_member.CanManageMembers(), Is.True);
    }

    [Test]
    public void OrganizationMember_RegularMemberCannotManageMembers()
    {
        // Arrange
        _member.Rank = OrganizationRank.Member;

        // Assert
        Assert.That(_member.CanManageMembers(), Is.False);
    }

    [Test]
    public void OrganizationMember_LeaderIdentified()
    {
        // Arrange
        _member.Rank = OrganizationRank.Leader;

        // Assert
        Assert.That(_member.IsLeader(), Is.True);
        Assert.That(_member.CanManageMembers(), Is.True);
    }

    [Test]
    public void OrganizationMember_CanBecomeInactive()
    {
        // Act
        _member.Status = MembershipStatus.Inactive;

        // Assert
        Assert.That(_member.Status, Is.EqualTo(MembershipStatus.Inactive));
    }

    [Test]
    public void OrganizationMember_CanDepart()
    {
        // Act
        _member.Status = MembershipStatus.Departed;
        _member.DepartedDate = DateTime.UtcNow;

        // Assert
        Assert.That(_member.Status, Is.EqualTo(MembershipStatus.Departed));
        Assert.That(_member.DepartedDate, Is.Not.Null);
    }

    [Test]
    public void MemberRole_HasPredefinedRoles()
    {
        // Assert
        Assert.That(MemberRole.Leader.Value, Is.EqualTo("Leader"));
        Assert.That(MemberRole.Treasurer.Value, Is.EqualTo("Treasurer"));
        Assert.That(MemberRole.Diplomat.Value, Is.EqualTo("Diplomat"));
    }

    [Test]
    public void MemberRole_CanBeCustom()
    {
        // Arrange
        MemberRole customRole = new MemberRole("Blacksmith");

        // Assert
        Assert.That(customRole.Value, Is.EqualTo("Blacksmith"));
    }

    [Test]
    public void OrganizationRank_HasHierarchy()
    {
        // Assert - verify ranks are ordered
        Assert.That(OrganizationRank.Leader > OrganizationRank.Officer, Is.True);
        Assert.That(OrganizationRank.Officer > OrganizationRank.Member, Is.True);
        Assert.That(OrganizationRank.Member > OrganizationRank.Recruit, Is.True);
    }
}

