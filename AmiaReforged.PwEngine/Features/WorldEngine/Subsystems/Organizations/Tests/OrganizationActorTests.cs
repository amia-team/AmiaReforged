using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Application.Organizations.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Tests.Helpers;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Implementations;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Organizations;
using Moq;
using NUnit.Framework;
using OrgEntity = AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Organizations.Organization;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Organizations.Tests;

/// <summary>
/// Verifies explicit actor identity flows through the organization subsystem
/// instead of defaulting to self-as-actor.
/// </summary>
[TestFixture]
public class OrganizationActorTests
{
    private Mock<ICommandDispatcher> _commandsMock = null!;
    private OrganizationSubsystem _subsystem = null!;
    private OrganizationId _orgId;
    private CharacterId _targetId;
    private CharacterId _actorId;

    [SetUp]
    public void SetUp()
    {
        _commandsMock = new Mock<ICommandDispatcher>();
        _commandsMock
            .Setup(d => d.DispatchAsync(It.IsAny<ICommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(CommandResult.Ok());
        // Typed setups so the captured command keeps its runtime type
        _subsystem = new OrganizationSubsystem(
            _commandsMock.Object,
            Mock.Of<IQueryDispatcher>());

        _orgId = OrganizationId.New();
        _targetId = new CharacterId(Guid.NewGuid());
        _actorId = new CharacterId(Guid.NewGuid());
    }

    [Test]
    public async Task RemoveMember_DefaultActor_UsesSelf()
    {
        RemoveMemberCommand? captured = null;
        _commandsMock
            .Setup(d => d.DispatchAsync(It.IsAny<RemoveMemberCommand>(), It.IsAny<CancellationToken>()))
            .Callback<RemoveMemberCommand, CancellationToken>((cmd, _) => captured = cmd)
            .ReturnsAsync(CommandResult.Ok());

        await _subsystem.RemoveMemberAsync(_orgId, _targetId);

        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.RemovedBy, Is.EqualTo(_targetId));
    }

    [Test]
    public async Task RemoveMember_ExplicitActor_PassesThrough()
    {
        RemoveMemberCommand? captured = null;
        _commandsMock
            .Setup(d => d.DispatchAsync(It.IsAny<RemoveMemberCommand>(), It.IsAny<CancellationToken>()))
            .Callback<RemoveMemberCommand, CancellationToken>((cmd, _) => captured = cmd)
            .ReturnsAsync(CommandResult.Ok());

        await _subsystem.RemoveMemberAsync(_orgId, _targetId, _actorId);

        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.RemovedBy, Is.EqualTo(_actorId));
    }

    [Test]
    public async Task UpdateMemberRank_ExplicitActor_PassesThrough()
    {
        ChangeRankCommand? captured = null;
        _commandsMock
            .Setup(d => d.DispatchAsync(It.IsAny<ChangeRankCommand>(), It.IsAny<CancellationToken>()))
            .Callback<ChangeRankCommand, CancellationToken>((cmd, _) => captured = cmd)
            .ReturnsAsync(CommandResult.Ok());

        await _subsystem.UpdateMemberRankAsync(_orgId, _targetId, OrganizationRank.Member.ToString(), _actorId);

        Assert.That(captured, Is.Not.Null);
        Assert.That(captured!.ChangedBy, Is.EqualTo(_actorId));
        Assert.That(captured.NewRank, Is.EqualTo(OrganizationRank.Member));
    }

    [Test]
    public async Task RemoveMember_ByOfficer_SetsExpelledStatus()
    {
        InMemoryOrganizationRepository orgRepo = new();
        InMemoryOrganizationMemberRepository memberRepo = new();
        InMemoryEventBus eventBus = new();
        OrgEntity org = OrgEntity.Create(_orgId, "Test Guild", "desc", OrganizationType.Guild);
        orgRepo.Add(org);

        CharacterId officerId = new(Guid.NewGuid());
        memberRepo.Add(new OrganizationMember
        {
            Id = Guid.NewGuid(),
            CharacterId = officerId,
            OrganizationId = _orgId,
            Rank = OrganizationRank.Officer,
            Status = MembershipStatus.Active,
            JoinedDate = DateTime.UtcNow,
            Roles = []
        });
        memberRepo.Add(new OrganizationMember
        {
            Id = Guid.NewGuid(),
            CharacterId = _targetId,
            OrganizationId = _orgId,
            Rank = OrganizationRank.Recruit,
            Status = MembershipStatus.Active,
            JoinedDate = DateTime.UtcNow,
            Roles = []
        });

        RemoveMemberHandler handler = new(memberRepo, eventBus);
        CommandResult result = await handler.HandleAsync(new RemoveMemberCommand
        {
            OrganizationId = _orgId,
            CharacterId = _targetId,
            RemovedBy = officerId
        });

        Assert.That(result.Success, Is.True);
        OrganizationMember? updated = memberRepo.GetByCharacterAndOrganization(_targetId, _orgId);
        Assert.That(updated!.Status, Is.EqualTo(MembershipStatus.Expelled));
    }

    [Test]
    public async Task ChangeRank_SelfWithoutPermissions_Fails()
    {
        InMemoryOrganizationMemberRepository memberRepo = new();
        InMemoryEventBus eventBus = new();
        memberRepo.Add(new OrganizationMember
        {
            Id = Guid.NewGuid(),
            CharacterId = _targetId,
            OrganizationId = _orgId,
            Rank = OrganizationRank.Recruit,
            Status = MembershipStatus.Active,
            JoinedDate = DateTime.UtcNow,
            Roles = []
        });

        ChangeRankHandler handler = new(memberRepo, eventBus);
        CommandResult result = await handler.HandleAsync(new ChangeRankCommand
        {
            OrganizationId = _orgId,
            CharacterId = _targetId,
            NewRank = OrganizationRank.Member,
            ChangedBy = _targetId
        });

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("permission").IgnoreCase);
    }

    [Test]
    public async Task ChangeRank_ByOfficer_Succeeds()
    {
        InMemoryOrganizationMemberRepository memberRepo = new();
        InMemoryEventBus eventBus = new();
        CharacterId officerId = new(Guid.NewGuid());
        memberRepo.Add(new OrganizationMember
        {
            Id = Guid.NewGuid(),
            CharacterId = officerId,
            OrganizationId = _orgId,
            Rank = OrganizationRank.Officer,
            Status = MembershipStatus.Active,
            JoinedDate = DateTime.UtcNow,
            Roles = []
        });
        memberRepo.Add(new OrganizationMember
        {
            Id = Guid.NewGuid(),
            CharacterId = _targetId,
            OrganizationId = _orgId,
            Rank = OrganizationRank.Recruit,
            Status = MembershipStatus.Active,
            JoinedDate = DateTime.UtcNow,
            Roles = []
        });

        ChangeRankHandler handler = new(memberRepo, eventBus);
        CommandResult result = await handler.HandleAsync(new ChangeRankCommand
        {
            OrganizationId = _orgId,
            CharacterId = _targetId,
            NewRank = OrganizationRank.Member,
            ChangedBy = officerId
        });

        Assert.That(result.Success, Is.True);
        OrganizationMember? updated = memberRepo.GetByCharacterAndOrganization(_targetId, _orgId);
        Assert.That(updated!.Rank, Is.EqualTo(OrganizationRank.Member));
    }
}
