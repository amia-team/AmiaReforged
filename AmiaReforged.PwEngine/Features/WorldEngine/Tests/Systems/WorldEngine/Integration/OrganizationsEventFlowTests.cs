using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Application.Organizations.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Organizations;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Organizations.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Tests.Systems.WorldEngine.Helpers;
using NUnit.Framework;
using Organization = AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Organizations.Organization;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Tests.Systems.WorldEngine.Integration;

/// <summary>
/// Integration tests for Organizations event flows.
/// Verifies that commands publish events and handlers react correctly.
/// </summary>
[TestFixture]
public class OrganizationsEventFlowTests
{
    private InMemoryEventBus _eventBus = null!;
    private IOrganizationRepository _organizationRepository = null!;
    private IOrganizationMemberRepository _memberRepository = null!;
    private IPersonaRepository _personaRepository = null!;
    private CreateOrganizationHandler _createHandler = null!;
    private AddMemberHandler _addMemberHandler = null!;
    private RemoveMemberHandler _removeMemberHandler = null!;
    private ChangeRankHandler _changeRankHandler = null!;

    [SetUp]
    public void SetUp()
    {
        // Set up repositories
        _organizationRepository = new InMemoryOrganizationRepository();
        _memberRepository = new InMemoryOrganizationMemberRepository();
        _personaRepository = InMemoryPersonaRepository.Create();
        _eventBus = new InMemoryEventBus();

        // Create command handlers
        _createHandler = new CreateOrganizationHandler(
            _organizationRepository,
            _personaRepository,
            _eventBus);
        _addMemberHandler = new AddMemberHandler(
            _memberRepository,
            _organizationRepository,
            _eventBus);
        _removeMemberHandler = new RemoveMemberHandler(
            _memberRepository,
            _eventBus);
        _changeRankHandler = new ChangeRankHandler(
            _memberRepository,
            _eventBus);
    }

    [Test]
    public async Task CreateOrganization_ShouldPublish_OrganizationCreatedEvent()
    {
        // Arrange
        CreateOrganizationCommand command = new CreateOrganizationCommand
        {
            Name = "Test Guild",
            Description = "A test guild",
            Type = OrganizationType.Guild,
            ParentOrganizationId = null
        };

        // Act
        CommandResult result = await _createHandler.HandleAsync(command);

        // Assert - Verify command succeeded
        Assert.That(result.Success, Is.True, "Command should succeed");

        // Assert - Verify event was published
        IReadOnlyList<IDomainEvent> events = _eventBus.PublishedEvents;
        Assert.That(events, Has.Count.EqualTo(1), "Should publish exactly one event");

        OrganizationCreatedEvent? evt = events.OfType<OrganizationCreatedEvent>().FirstOrDefault();
        Assert.That(evt, Is.Not.Null, "Should publish OrganizationCreatedEvent");
        Assert.That(evt!.Name, Is.EqualTo("Test Guild"), "Event should contain correct name");
        Assert.That(evt.Type, Is.EqualTo(OrganizationType.Guild), "Event should contain correct type");
        Assert.That(evt.ParentOrganizationId, Is.Null, "Event should have no parent");
    }

    [Test]
    public async Task AddMember_ShouldPublish_MemberJoinedOrganizationEvent()
    {
        // Arrange - Create organization first
        IOrganization org = Organization.CreateNew("Test Guild", "Test", OrganizationType.Guild);
        _organizationRepository.Add(org);
        _eventBus.ClearPublishedEvents();

        CharacterId characterId = new CharacterId(Guid.NewGuid());
        AddMemberCommand command = new AddMemberCommand
        {
            OrganizationId = org.Id,
            CharacterId = characterId,
            InitialRank = OrganizationRank.Recruit
        };

        // Act
        CommandResult result = await _addMemberHandler.HandleAsync(command);

        // Assert - Verify command succeeded
        Assert.That(result.Success, Is.True, "Command should succeed");

        // Assert - Verify event was published
        IReadOnlyList<IDomainEvent> events = _eventBus.PublishedEvents;
        Assert.That(events, Has.Count.EqualTo(1), "Should publish exactly one event");

        MemberJoinedOrganizationEvent? evt = events.OfType<MemberJoinedOrganizationEvent>().FirstOrDefault();
        Assert.That(evt, Is.Not.Null, "Should publish MemberJoinedOrganizationEvent");
        Assert.That(evt!.MemberId, Is.EqualTo(characterId), "Event should contain correct member ID");
        Assert.That(evt.OrganizationId, Is.EqualTo(org.Id), "Event should contain correct organization ID");
        Assert.That(evt.InitialRank, Is.EqualTo(OrganizationRank.Recruit), "Event should contain correct rank");
    }

    [Test]
    public async Task RemoveMember_ShouldPublish_MemberLeftOrganizationEvent()
    {
        // Arrange - Create organization and member
        IOrganization org = Organization.CreateNew("Test Guild", "Test", OrganizationType.Guild);
        _organizationRepository.Add(org);

        CharacterId characterId = new CharacterId(Guid.NewGuid());
        CharacterId removerId = new CharacterId(Guid.NewGuid());

        // Add member
        OrganizationMember member = new OrganizationMember
        {
            Id = Guid.NewGuid(),
            CharacterId = characterId,
            OrganizationId = org.Id,
            Rank = OrganizationRank.Member,
            Status = MembershipStatus.Active,
            JoinedDate = DateTime.UtcNow,
            Roles = new List<MemberRole>()
        };
        _memberRepository.Add(member);

        // Add remover (must be officer or higher)
        OrganizationMember remover = new OrganizationMember
        {
            Id = Guid.NewGuid(),
            CharacterId = removerId,
            OrganizationId = org.Id,
            Rank = OrganizationRank.Officer,
            Status = MembershipStatus.Active,
            JoinedDate = DateTime.UtcNow,
            Roles = new List<MemberRole>()
        };
        _memberRepository.Add(remover);
        _eventBus.ClearPublishedEvents();

        RemoveMemberCommand command = new RemoveMemberCommand
        {
            OrganizationId = org.Id,
            CharacterId = characterId,
            RemovedBy = removerId,
            Reason = "Test removal"
        };

        // Act
        CommandResult result = await _removeMemberHandler.HandleAsync(command);

        // Assert - Verify command succeeded
        Assert.That(result.Success, Is.True, "Command should succeed");

        // Assert - Verify event was published
        IReadOnlyList<IDomainEvent> events = _eventBus.PublishedEvents;
        Assert.That(events, Has.Count.EqualTo(1), "Should publish exactly one event");

        MemberLeftOrganizationEvent? evt = events.OfType<MemberLeftOrganizationEvent>().FirstOrDefault();
        Assert.That(evt, Is.Not.Null, "Should publish MemberLeftOrganizationEvent");
        Assert.That(evt!.MemberId, Is.EqualTo(characterId), "Event should contain correct member ID");
        Assert.That(evt.OrganizationId, Is.EqualTo(org.Id), "Event should contain correct organization ID");
    }

    [Test]
    public async Task ChangeRank_ShouldPublish_MemberRoleChangedEvent()
    {
        // Arrange - Create organization and members
        IOrganization org = Organization.CreateNew("Test Guild", "Test", OrganizationType.Guild);
        _organizationRepository.Add(org);

        CharacterId characterId = new CharacterId(Guid.NewGuid());
        CharacterId officerId = new CharacterId(Guid.NewGuid());

        // Add member
        OrganizationMember member = new OrganizationMember
        {
            Id = Guid.NewGuid(),
            CharacterId = characterId,
            OrganizationId = org.Id,
            Rank = OrganizationRank.Recruit,
            Status = MembershipStatus.Active,
            JoinedDate = DateTime.UtcNow,
            Roles = new List<MemberRole>()
        };
        _memberRepository.Add(member);

        // Add officer who can promote
        OrganizationMember officer = new OrganizationMember
        {
            Id = Guid.NewGuid(),
            CharacterId = officerId,
            OrganizationId = org.Id,
            Rank = OrganizationRank.Officer,
            Status = MembershipStatus.Active,
            JoinedDate = DateTime.UtcNow,
            Roles = new List<MemberRole>()
        };
        _memberRepository.Add(officer);
        _eventBus.ClearPublishedEvents();

        ChangeRankCommand command = new ChangeRankCommand
        {
            OrganizationId = org.Id,
            CharacterId = characterId,
            NewRank = OrganizationRank.Member,
            ChangedBy = officerId
        };

        // Act
        CommandResult result = await _changeRankHandler.HandleAsync(command);

        // Assert - Verify command succeeded
        Assert.That(result.Success, Is.True, "Command should succeed");

        // Assert - Verify event was published
        IReadOnlyList<IDomainEvent> events = _eventBus.PublishedEvents;
        Assert.That(events, Has.Count.EqualTo(1), "Should publish exactly one event");

        MemberRoleChangedEvent? evt = events.OfType<MemberRoleChangedEvent>().FirstOrDefault();
        Assert.That(evt, Is.Not.Null, "Should publish MemberRoleChangedEvent");
        Assert.That(evt!.MemberId, Is.EqualTo(characterId), "Event should contain correct member ID");
        Assert.That(evt.OrganizationId, Is.EqualTo(org.Id), "Event should contain correct organization ID");
        Assert.That(evt.NewRank, Is.EqualTo(OrganizationRank.Member), "Event should contain new rank");
        Assert.That(evt.PreviousRank, Is.EqualTo(OrganizationRank.Recruit), "Event should contain previous rank");
    }

    [Test]
    public async Task CompleteWorkflow_ShouldPublish_EventsInOrder()
    {
        // Arrange - Create organization
        CreateOrganizationCommand createCommand = new CreateOrganizationCommand
        {
            Name = "Test Guild",
            Description = "Test",
            Type = OrganizationType.Guild
        };

        // Act - Complete workflow: create org, add member, add officer, promote member, remove
        CommandResult createResult = await _createHandler.HandleAsync(createCommand);
        OrganizationId orgId = (OrganizationId)createResult.Data!["OrganizationId"]!;

        CharacterId characterId = new CharacterId(Guid.NewGuid());
        CharacterId officerId = new CharacterId(Guid.NewGuid());

        // Add officer first (can manage members)
        CommandResult addOfficerResult = await _addMemberHandler.HandleAsync(new AddMemberCommand
        {
            OrganizationId = orgId,
            CharacterId = officerId,
            InitialRank = OrganizationRank.Officer
        });

        // Add regular member
        CommandResult addResult = await _addMemberHandler.HandleAsync(new AddMemberCommand
        {
            OrganizationId = orgId,
            CharacterId = characterId,
            InitialRank = OrganizationRank.Recruit
        });

        // Officer promotes the member
        CommandResult changeRankResult = await _changeRankHandler.HandleAsync(new ChangeRankCommand
        {
            OrganizationId = orgId,
            CharacterId = characterId,
            NewRank = OrganizationRank.Member,
            ChangedBy = officerId // Officer promotes
        });

        CommandResult removeResult = await _removeMemberHandler.HandleAsync(new RemoveMemberCommand
        {
            OrganizationId = orgId,
            CharacterId = characterId,
            RemovedBy = characterId, // Self-removal
            Reason = "Voluntary departure"
        });

        // Assert - Verify all commands succeeded
        Assert.That(createResult.Success, Is.True, "Create should succeed");
        Assert.That(addOfficerResult.Success, Is.True, "Add officer should succeed");
        Assert.That(addResult.Success, Is.True, "Add member should succeed");
        Assert.That(changeRankResult.Success, Is.True, "Change rank should succeed");
        Assert.That(removeResult.Success, Is.True, "Remove member should succeed");

        // Assert - Verify all events published in order
        IReadOnlyList<IDomainEvent> events = _eventBus.PublishedEvents;
        Assert.That(events, Has.Count.EqualTo(5), "Should publish five events");

        Assert.That(events[0], Is.TypeOf<OrganizationCreatedEvent>(), "First event should be OrganizationCreated");
        Assert.That(events[1], Is.TypeOf<MemberJoinedOrganizationEvent>(), "Second event should be MemberJoined (officer)");
        Assert.That(events[2], Is.TypeOf<MemberJoinedOrganizationEvent>(), "Third event should be MemberJoined (member)");
        Assert.That(events[3], Is.TypeOf<MemberRoleChangedEvent>(), "Fourth event should be MemberRoleChanged");
        Assert.That(events[4], Is.TypeOf<MemberLeftOrganizationEvent>(), "Fifth event should be MemberLeft");
    }
}

