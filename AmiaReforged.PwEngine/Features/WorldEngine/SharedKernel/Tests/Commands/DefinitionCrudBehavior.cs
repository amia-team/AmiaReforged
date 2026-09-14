using AmiaReforged.PwEngine.Database.Entities;
using AmiaReforged.PwEngine.Features.WorldEngine.Application.Industries.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Application.Industries.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Application.Items.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Application.Organizations.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Application.Regions.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Application.Regions.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Application.ResourceNodes.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Tests.Helpers;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions.Tests;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.ItemData;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Organizations;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Regions;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes.ResourceNodeData;
using Moq;
using NUnit.Framework;
using DomainOrganization = AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Organizations.Organization;

namespace AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Tests.Commands;

/// <summary>
/// Behavioral specs for the admin definition commands introduced by the F-1 fix
/// (industries, workstations, recipes, nodes, regions, interactions, orgs, items).
/// Each spec dispatches through the real <see cref="CommandDispatcher"/> against
/// in-memory repositories: a Fail here means the admin panel would 4xx.
/// </summary>
[TestFixture]
public class DefinitionCrudBehavior
{
    private Mock<IEventBus> _eventBusMock = null!;

    [SetUp]
    public void SetUp()
    {
        _eventBusMock = new Mock<IEventBus>();
        _eventBusMock
            .Setup(b => b.PublishAsync(It.IsAny<IDomainEvent>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private CommandDispatcher Dispatcher(params ICommandHandlerMarker[] handlers) =>
        new(handlers, _eventBusMock.Object);

    private static Industry TestIndustry(string tag) => new()
    {
        Tag = tag,
        Name = $"Name {tag}",
        Knowledge = [],
        Recipes = []
    };

    // === Industries ===

    [Test]
    public async Task Industry_CreateAndDuplicate_BehavesLikeAdminPanel()
    {
        InMemoryIndustryRepository repo = new();
        CommandDispatcher dispatcher = Dispatcher(new CreateIndustryHandler(repo));

        CommandResult created = await dispatcher.DispatchAsync(
            new CreateIndustryCommand { Industry = TestIndustry("smithing") });
        Assert.That(created.Success, Is.True);

        CommandResult duplicate = await dispatcher.DispatchAsync(
            new CreateIndustryCommand { Industry = TestIndustry("smithing") });
        Assert.That(duplicate.Success, Is.False);
        Assert.That(duplicate.ErrorMessage, Does.Contain("already exists"));

        _eventBusMock.Verify(b => b.PublishAsync(
            It.Is<CommandExecutedEvent<CreateIndustryCommand>>(e => e.Result.Success),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Test]
    public async Task Industry_UpdateMissing_ReturnsFail()
    {
        InMemoryIndustryRepository repo = new();
        CommandDispatcher dispatcher = Dispatcher(new UpdateIndustryHandler(repo));

        CommandResult result = await dispatcher.DispatchAsync(
            new UpdateIndustryCommand { Tag = "nope", Industry = TestIndustry("nope") });

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("No industry with tag"));
    }

    [Test]
    public async Task Industry_Delete_RemovesDefinition()
    {
        InMemoryIndustryRepository repo = new();
        CommandDispatcher dispatcher = Dispatcher(
            new CreateIndustryHandler(repo),
            new DeleteIndustryHandler(repo));
        await dispatcher.DispatchAsync(new CreateIndustryCommand { Industry = TestIndustry("x") });

        CommandResult deleted = await dispatcher.DispatchAsync(new DeleteIndustryCommand { Tag = "x" });
        Assert.That(deleted.Success, Is.True);

        CommandResult missing = await dispatcher.DispatchAsync(new DeleteIndustryCommand { Tag = "x" });
        Assert.That(missing.Success, Is.False);
    }

    [Test]
    public async Task Industry_Search_ReturnsMatches()
    {
        InMemoryIndustryRepository repo = new();
        repo.Add(TestIndustry("smithing"));
        repo.Add(TestIndustry("farming"));
        SearchIndustryDefinitionsHandler handler = new(repo);

        List<Industry> matches = await handler.HandleAsync(
            new SearchIndustryDefinitionsQuery { SearchTerm = "smith" });

        Assert.That(matches.Select(i => i.Tag), Is.EquivalentTo(new[] { "smithing" }));
    }

    // === Workstations (fake repo — no in-memory implementation exists) ===

    private sealed class FakeWorkstationRepository : IWorkstationRepository
    {
        private readonly Dictionary<string, Workstation> _store = new(StringComparer.OrdinalIgnoreCase);
        public bool WorkstationExists(string tag) => _store.ContainsKey(tag);
        public Workstation? GetByTag(WorkstationTag tag) => _store.GetValueOrDefault(tag.Value);
        public List<Workstation> All() => _store.Values.ToList();
        public void Add(Workstation workstation) => _store[workstation.Tag.Value] = workstation;
        public void Update(Workstation workstation) => _store[workstation.Tag.Value] = workstation;
        public bool Delete(string tag) => _store.Remove(tag);
        public List<Workstation> Search(string? searchTerm, int page, int pageSize, out int totalCount)
        {
            totalCount = _store.Count;
            return _store.Values.ToList();
        }
    }

    private static Workstation TestWorkstation(string tag) => new()
    {
        Tag = new WorkstationTag(tag),
        Name = $"Name {tag}"
    };

    [Test]
    public async Task Workstation_CreateDuplicateUpdateDelete_FollowCommandContract()
    {
        FakeWorkstationRepository repo = new();
        CommandDispatcher dispatcher = Dispatcher(
            new CreateWorkstationHandler(repo),
            new UpdateWorkstationHandler(repo),
            new UpsertWorkstationHandler(repo),
            new DeleteWorkstationHandler(repo));

        Assert.That((await dispatcher.DispatchAsync(
            new CreateWorkstationCommand { Workstation = TestWorkstation("forge") })).Success, Is.True);
        Assert.That((await dispatcher.DispatchAsync(
            new CreateWorkstationCommand { Workstation = TestWorkstation("forge") })).Success, Is.False);

        Assert.That((await dispatcher.DispatchAsync(new UpdateWorkstationCommand
            { Tag = "missing", Workstation = TestWorkstation("missing") })).Success, Is.False);

        Assert.That((await dispatcher.DispatchAsync(
            new UpsertWorkstationCommand { Workstation = TestWorkstation("forge") })).Success, Is.True);

        Assert.That((await dispatcher.DispatchAsync(
            new DeleteWorkstationCommand { Tag = "forge" })).Success, Is.True);
    }

    // === Regions ===

    [Test]
    public async Task Region_UpsertUpdateDelete_FollowCommandContract()
    {
        InMemoryRegionRepository repo = new();
        CommandDispatcher dispatcher = Dispatcher(
            new UpsertRegionHandler(repo),
            new UpdateRegionHandler(repo),
            new DeleteRegionHandler(repo));

        RegionDefinition def = new() { Tag = new RegionTag("dale"), Name = "Dale" };
        Assert.That((await dispatcher.DispatchAsync(
            new UpsertRegionCommand { Definition = def })).Success, Is.True);

        Assert.That((await dispatcher.DispatchAsync(new UpdateRegionCommand
            { Tag = "missing", Definition = def })).Success, Is.False);

        Assert.That((await dispatcher.DispatchAsync(
            new DeleteRegionCommand { Tag = "dale" })).Success, Is.True);
        Assert.That((await dispatcher.DispatchAsync(
            new DeleteRegionCommand { Tag = "dale" })).Success, Is.False);
    }

    [Test]
    public async Task Region_Search_FiltersByTerm()
    {
        InMemoryRegionRepository repo = new();
        repo.Add(new RegionDefinition { Tag = new RegionTag("dale"), Name = "Dale" });
        repo.Add(new RegionDefinition { Tag = new RegionTag("cyst"), Name = "Cystana" });
        SearchRegionDefinitionsHandler handler = new(repo);

        List<RegionDefinition> matches = await handler.HandleAsync(
            new SearchRegionDefinitionsQuery { SearchTerm = "cyst" });

        Assert.That(matches.Select(r => r.Tag.Value), Is.EquivalentTo(new[] { "cyst" }));
    }

    // === Resource nodes ===

    [Test]
    public async Task ResourceNode_CreateDuplicateDelete_FollowCommandContract()
    {
        InMemoryResourceNodeDefinitionRepository repo = new();
        CommandDispatcher dispatcher = Dispatcher(
            new CreateResourceNodeHandler(repo),
            new UpsertResourceNodeHandler(repo),
            new DeleteResourceNodeHandler(repo));

        ResourceNodeDefinition def = new(
            0, ResourceType.Ore, "node-1",
            new HarvestContext(ItemForm.None), [],
            Name: "Node 1");

        Assert.That((await dispatcher.DispatchAsync(
            new CreateResourceNodeCommand { Definition = def })).Success, Is.True);
        Assert.That((await dispatcher.DispatchAsync(
            new CreateResourceNodeCommand { Definition = def })).Success, Is.False);

        Assert.That((await dispatcher.DispatchAsync(
            new UpsertResourceNodeCommand { Definition = def })).Success, Is.True);
        Assert.That((await dispatcher.DispatchAsync(
            new DeleteResourceNodeCommand { Tag = "node-1" })).Success, Is.True);
    }

    // === Interactions ===

    [Test]
    public async Task Interaction_CreateDuplicateDelete_FollowCommandContract()
    {
        InMemoryInteractionDefinitionRepository repo = new();
        CommandDispatcher dispatcher = Dispatcher(
            new CreateInteractionDefinitionHandler(repo),
            new DeleteInteractionDefinitionHandler(repo));

        InteractionDefinition def = new() { Tag = "prospect", Name = "Prospect" };

        Assert.That((await dispatcher.DispatchAsync(
            new CreateInteractionDefinitionCommand { Definition = def })).Success, Is.True);
        Assert.That((await dispatcher.DispatchAsync(
            new CreateInteractionDefinitionCommand { Definition = def })).Success, Is.False);

        Assert.That((await dispatcher.DispatchAsync(
            new DeleteInteractionDefinitionCommand { Tag = "prospect" })).Success, Is.True);
    }

    // === Organizations ===

    [Test]
    public async Task Organization_UpdateMissing_ReturnsFail()
    {
        InMemoryOrganizationRepository repo = new();
        CommandDispatcher dispatcher = Dispatcher(new UpdateOrganizationHandler(repo));

        CommandResult result = await dispatcher.DispatchAsync(new UpdateOrganizationCommand
        {
            OrganizationId = OrganizationId.From(Guid.NewGuid()),
            Name = "New"
        });

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("not found"));
    }

    [Test]
    public async Task Organization_Disband_RemovesOrgAndMembersAndPublishesEvent()
    {
        InMemoryOrganizationRepository orgRepo = new();
        InMemoryOrganizationMemberRepository memberRepo = new();
        CommandDispatcher dispatcher = Dispatcher(
            new DisbandOrganizationHandler(orgRepo, memberRepo, _eventBusMock.Object));

        IOrganization org = DomainOrganization.CreateNew("Guild", "Desc", OrganizationType.Guild, null);
        orgRepo.Add(org);
        Guid charId = Guid.NewGuid();
        memberRepo.Add(new OrganizationMember
        {
            Id = Guid.NewGuid(),
            CharacterId = new CharacterId(charId),
            OrganizationId = org.Id,
            Rank = OrganizationRank.Member,
            Status = MembershipStatus.Active,
            JoinedDate = DateTime.UtcNow
        });

        CommandResult result = await dispatcher.DispatchAsync(
            new DisbandOrganizationCommand { OrganizationId = org.Id });

        Assert.That(result.Success, Is.True);
        Assert.That(orgRepo.GetById(org.Id), Is.Null);
        Assert.That(memberRepo.GetByOrganization(org.Id), Is.Empty);
        _eventBusMock.Verify(b => b.PublishAsync(
            It.Is<Subsystems.Organizations.Events.OrganizationDisbandedEvent>(
                e => e.OrganizationId == org.Id),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    // === Items ===

    private static ItemBlueprint TestBlueprint(string tag) => new(
        tag, tag, $"Name {tag}", $"Desc {tag}",
        [], ItemForm.None, 0,
        new AppearanceData(0, null, null), null);

    [Test]
    public async Task Item_Upsert_StoresBlueprint()
    {
        InMemoryItemDefinitionRepository repo = new();
        CommandDispatcher dispatcher = Dispatcher(new UpsertItemDefinitionHandler(repo));

        CommandResult result = await dispatcher.DispatchAsync(
            new UpsertItemDefinitionCommand { Blueprint = TestBlueprint("w_sword") });

        Assert.That(result.Success, Is.True);
        Assert.That(repo.GetByTag("w_sword"), Is.Not.Null);
    }

    [Test]
    public async Task Item_DeleteWithoutDatabase_FailsWith501Meaning()
    {
        InMemoryItemDefinitionRepository repo = new();
        CommandDispatcher dispatcher = Dispatcher(new DeleteItemDefinitionHandler(repo));

        CommandResult result = await dispatcher.DispatchAsync(
            new DeleteItemDefinitionCommand { Tag = "w_sword" });

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("database-backed"));
    }
}
