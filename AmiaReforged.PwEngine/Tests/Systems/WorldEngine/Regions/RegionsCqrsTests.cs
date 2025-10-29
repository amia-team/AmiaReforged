using AmiaReforged.PwEngine.Features.WorldEngine.Regions;
using AmiaReforged.PwEngine.Features.WorldEngine.Regions.Application;
using AmiaReforged.PwEngine.Features.WorldEngine.Regions.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Regions.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Regions.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.ResourceNodes.ResourceNodeData;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.Regions;

/// <summary>
/// BDD-style tests for Regions CQRS implementation.
/// Tests demonstrate the command/query pattern with event publishing.
/// These tests do NOT interact with NWN objects and run entirely in-memory.
/// </summary>
[TestFixture]
public class RegionsCqrsTests
{
    private IRegionRepository _repository = null!;
    private IEventBus _eventBus = null!;
    private List<IDomainEvent> _publishedEvents = null!;

    private RegisterRegionCommandHandler _registerHandler = null!;
    private UpdateRegionCommandHandler _updateHandler = null!;
    private RemoveRegionCommandHandler _removeHandler = null!;
    private ClearAllRegionsCommandHandler _clearAllHandler = null!;

    private GetRegionByTagQueryHandler _getByTagHandler = null!;
    private GetAllRegionsQueryHandler _getAllHandler = null!;
    private GetRegionBySettlementQueryHandler _getBySettlementHandler = null!;
    private GetSettlementsForRegionQueryHandler _getSettlementsHandler = null!;

    private const string TestRegionTag = "test_region";
    private const string TestRegionName = "Test Region";
    private const string TestAreaResRef = "test_area";

    [SetUp]
    public void SetUp()
    {
        _repository = new InMemoryRegionRepository();
        _publishedEvents = new List<IDomainEvent>();
        _eventBus = new TestEventBus(_publishedEvents);

        // Initialize command handlers
        _registerHandler = new RegisterRegionCommandHandler(_repository, _eventBus);
        _updateHandler = new UpdateRegionCommandHandler(_repository, _eventBus);
        _removeHandler = new RemoveRegionCommandHandler(_repository, _eventBus);
        _clearAllHandler = new ClearAllRegionsCommandHandler(_repository, _eventBus);

        // Initialize query handlers
        _getByTagHandler = new GetRegionByTagQueryHandler(_repository);
        _getAllHandler = new GetAllRegionsQueryHandler(_repository);
        _getBySettlementHandler = new GetRegionBySettlementQueryHandler(_repository);
        _getSettlementsHandler = new GetSettlementsForRegionQueryHandler(_repository);
    }

    #region Register Region Tests

    [Test]
    public async Task RegisterRegion_WithValidData_ShouldSucceed()
    {
        // Given
        List<AreaDefinition> areas = new()
        {
            new AreaDefinition(
                new AreaTag(TestAreaResRef),
                new List<string> { "wilderness" },
                new EnvironmentData(Climate.Temperate, EconomyQuality.Average, new QualityRange()))
        };

        List<SettlementId> settlements = new() { SettlementId.Parse(1), SettlementId.Parse(2) };

        RegisterRegionCommand command = new(
            new RegionTag(TestRegionTag),
            TestRegionName,
            areas,
            settlements);

        // When
        CommandResult result = await _registerHandler.HandleAsync(command, CancellationToken.None);

        // Then
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data, Is.Not.Null);
        Assert.That(result.Data!["tag"], Is.EqualTo(TestRegionTag));

        // And the region should be in the repository
        Assert.That(_repository.Exists(new RegionTag(TestRegionTag)), Is.True);

        // And an event should be published
        Assert.That(_publishedEvents, Has.Count.EqualTo(1));
        RegionRegisteredEvent? evt = _publishedEvents[0] as RegionRegisteredEvent;
        Assert.That(evt, Is.Not.Null);
        Assert.That(evt!.Tag.Value, Is.EqualTo(TestRegionTag));
        Assert.That(evt.Name, Is.EqualTo(TestRegionName));
        Assert.That(evt.AreaCount, Is.EqualTo(1));
        Assert.That(evt.SettlementCount, Is.EqualTo(2));
    }

    [Test]
    public void RegisterRegion_WithEmptyTag_ShouldThrowException()
    {
        // Given / When / Then - RegionTag constructor should throw for empty tag
        Assert.Throws<ArgumentException>(() => new RegionTag(""));
    }

    [Test]
    public async Task RegisterRegion_WithEmptyName_ShouldFail()
    {
        // Given
        RegisterRegionCommand command = new(
            new RegionTag(TestRegionTag),
            "",
            new List<AreaDefinition>(),
            new List<SettlementId>());

        // When
        CommandResult result = await _registerHandler.HandleAsync(command, CancellationToken.None);

        // Then
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("name cannot be empty"));
        Assert.That(_publishedEvents, Is.Empty);
    }

    [Test]
    public async Task RegisterRegion_WithNoAreas_ShouldFail()
    {
        // Given
        RegisterRegionCommand command = new(
            new RegionTag(TestRegionTag),
            TestRegionName,
            new List<AreaDefinition>(),
            new List<SettlementId>());

        // When
        CommandResult result = await _registerHandler.HandleAsync(command, CancellationToken.None);

        // Then
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("at least one area"));
        Assert.That(_publishedEvents, Is.Empty);
    }

    [Test]
    public async Task RegisterRegion_WithDuplicateTag_ShouldFail()
    {
        // Given - first region registered successfully
        List<AreaDefinition> areas = new()
        {
            new AreaDefinition(
                new AreaTag(TestAreaResRef),
                new List<string> { "wilderness" },
                new EnvironmentData(Climate.Temperate, EconomyQuality.Average, new QualityRange()))
        };

        RegisterRegionCommand command1 = new(
            new RegionTag(TestRegionTag),
            TestRegionName,
            areas,
            new List<SettlementId>());

        await _registerHandler.HandleAsync(command1, CancellationToken.None);
        _publishedEvents.Clear();

        // When - attempting to register duplicate
        RegisterRegionCommand command2 = new(
            new RegionTag(TestRegionTag),
            "Different Name",
            areas,
            new List<SettlementId>());

        CommandResult result = await _registerHandler.HandleAsync(command2, CancellationToken.None);

        // Then
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("already exists"));
        Assert.That(_publishedEvents, Is.Empty);
    }

    #endregion

    #region Update Region Tests

    [Test]
    public async Task UpdateRegion_WithValidData_ShouldSucceed()
    {
        // Given - existing region
        await RegisterTestRegion();
        _publishedEvents.Clear();

        // When - updating the region
        UpdateRegionCommand command = new(
            new RegionTag(TestRegionTag),
            Name: "Updated Region Name");

        CommandResult result = await _updateHandler.HandleAsync(command, CancellationToken.None);

        // Then
        Assert.That(result.Success, Is.True);

        // And the repository should reflect the update
        RegionDefinition? region = await _getByTagHandler.HandleAsync(
            new GetRegionByTagQuery(new RegionTag(TestRegionTag)),
            CancellationToken.None);

        Assert.That(region, Is.Not.Null);
        Assert.That(region!.Name, Is.EqualTo("Updated Region Name"));

        // And an event should be published
        Assert.That(_publishedEvents, Has.Count.EqualTo(1));
        RegionUpdatedEvent? evt = _publishedEvents[0] as RegionUpdatedEvent;
        Assert.That(evt, Is.Not.Null);
        Assert.That(evt!.Tag.Value, Is.EqualTo(TestRegionTag));
    }

    [Test]
    public async Task UpdateRegion_NonExistent_ShouldFail()
    {
        // Given - no region exists
        UpdateRegionCommand command = new(
            new RegionTag("nonexistent"),
            Name: "Some Name");

        // When
        CommandResult result = await _updateHandler.HandleAsync(command, CancellationToken.None);

        // Then
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("not found"));
        Assert.That(_publishedEvents, Is.Empty);
    }

    [Test]
    public async Task UpdateRegion_WithEmptyName_ShouldFail()
    {
        // Given - existing region
        await RegisterTestRegion();
        _publishedEvents.Clear();

        // When - updating with empty name
        UpdateRegionCommand command = new(
            new RegionTag(TestRegionTag),
            Name: "");

        CommandResult result = await _updateHandler.HandleAsync(command, CancellationToken.None);

        // Then
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("name cannot be empty"));
    }

    #endregion

    #region Remove Region Tests

    [Test]
    public async Task RemoveRegion_WithExistingRegion_ShouldSucceed()
    {
        // Given - existing region
        await RegisterTestRegion();
        _publishedEvents.Clear();

        // When - removing the region
        RemoveRegionCommand command = new(new RegionTag(TestRegionTag));
        CommandResult result = await _removeHandler.HandleAsync(command, CancellationToken.None);

        // Then
        Assert.That(result.Success, Is.True);

        // And the region should no longer exist
        Assert.That(_repository.Exists(new RegionTag(TestRegionTag)), Is.False);

        // And an event should be published
        Assert.That(_publishedEvents, Has.Count.EqualTo(1));
        RegionRemovedEvent? evt = _publishedEvents[0] as RegionRemovedEvent;
        Assert.That(evt, Is.Not.Null);
        Assert.That(evt!.Tag.Value, Is.EqualTo(TestRegionTag));
    }

    [Test]
    public async Task RemoveRegion_NonExistent_ShouldFail()
    {
        // Given - no region exists
        RemoveRegionCommand command = new(new RegionTag("nonexistent"));

        // When
        CommandResult result = await _removeHandler.HandleAsync(command, CancellationToken.None);

        // Then
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("not found"));
        Assert.That(_publishedEvents, Is.Empty);
    }

    #endregion

    #region Clear All Regions Tests

    [Test]
    public async Task ClearAllRegions_WithMultipleRegions_ShouldSucceed()
    {
        // Given - multiple regions
        await RegisterTestRegion();
        await RegisterTestRegion("region2", "Region Two");
        await RegisterTestRegion("region3", "Region Three");
        _publishedEvents.Clear();

        // When - clearing all regions
        ClearAllRegionsCommand command = new();
        CommandResult result = await _clearAllHandler.HandleAsync(command, CancellationToken.None);

        // Then
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data!["clearedCount"], Is.EqualTo(3));

        // And no regions should remain
        List<RegionDefinition> allRegions = await _getAllHandler.HandleAsync(
            new GetAllRegionsQuery(),
            CancellationToken.None);
        Assert.That(allRegions, Is.Empty);

        // And an event should be published
        Assert.That(_publishedEvents, Has.Count.EqualTo(1));
        AllRegionsClearedEvent? evt = _publishedEvents[0] as AllRegionsClearedEvent;
        Assert.That(evt, Is.Not.Null);
        Assert.That(evt!.RegionCount, Is.EqualTo(3));
    }

    #endregion

    #region Query Tests

    [Test]
    public async Task GetRegionByTag_WithExistingRegion_ShouldReturnRegion()
    {
        // Given - existing region
        await RegisterTestRegion();

        // When - querying by tag
        GetRegionByTagQuery query = new(new RegionTag(TestRegionTag));
        RegionDefinition? region = await _getByTagHandler.HandleAsync(query, CancellationToken.None);

        // Then
        Assert.That(region, Is.Not.Null);
        Assert.That(region!.Tag.Value, Is.EqualTo(TestRegionTag));
        Assert.That(region.Name, Is.EqualTo(TestRegionName));
    }

    [Test]
    public async Task GetRegionByTag_WithNonExistent_ShouldReturnNull()
    {
        // Given - no regions
        GetRegionByTagQuery query = new(new RegionTag("nonexistent"));

        // When
        RegionDefinition? region = await _getByTagHandler.HandleAsync(query, CancellationToken.None);

        // Then
        Assert.That(region, Is.Null);
    }

    [Test]
    public async Task GetAllRegions_WithMultipleRegions_ShouldReturnAll()
    {
        // Given - multiple regions
        await RegisterTestRegion();
        await RegisterTestRegion("region2", "Region Two");
        await RegisterTestRegion("region3", "Region Three");

        // When - querying all regions
        GetAllRegionsQuery query = new();
        List<RegionDefinition> regions = await _getAllHandler.HandleAsync(query, CancellationToken.None);

        // Then
        Assert.That(regions, Has.Count.EqualTo(3));
        Assert.That(regions.Select(r => r.Tag.Value), Contains.Item(TestRegionTag));
        Assert.That(regions.Select(r => r.Tag.Value), Contains.Item("region2"));
        Assert.That(regions.Select(r => r.Tag.Value), Contains.Item("region3"));
    }

    [Test]
    public async Task GetRegionBySettlement_WithExistingSettlement_ShouldReturnRegion()
    {
        // Given - region with settlements
        List<SettlementId> settlements = new() { SettlementId.Parse(1), SettlementId.Parse(2) };
        await RegisterTestRegion(settlements: settlements);

        // When - querying by settlement
        GetRegionBySettlementQuery query = new(SettlementId.Parse(1));
        RegionDefinition? region = await _getBySettlementHandler.HandleAsync(query, CancellationToken.None);

        // Then
        Assert.That(region, Is.Not.Null);
        Assert.That(region!.Tag.Value, Is.EqualTo(TestRegionTag));
    }

    [Test]
    public async Task GetRegionBySettlement_WithNonExistentSettlement_ShouldReturnNull()
    {
        // Given - region without the queried settlement
        await RegisterTestRegion();

        // When - querying for settlement not in any region
        GetRegionBySettlementQuery query = new(SettlementId.Parse(999));
        RegionDefinition? region = await _getBySettlementHandler.HandleAsync(query, CancellationToken.None);

        // Then
        Assert.That(region, Is.Null);
    }

    [Test]
    public async Task GetSettlementsForRegion_WithExistingRegion_ShouldReturnSettlements()
    {
        // Given - region with settlements
        List<SettlementId> settlements = new() { SettlementId.Parse(1), SettlementId.Parse(2), SettlementId.Parse(3) };
        await RegisterTestRegion(settlements: settlements);

        // When - querying settlements
        GetSettlementsForRegionQuery query = new(new RegionTag(TestRegionTag));
        IReadOnlyCollection<SettlementId> result = await _getSettlementsHandler.HandleAsync(query, CancellationToken.None);

        // Then
        Assert.That(result, Has.Count.EqualTo(3));
        Assert.That(result.Select(s => s.Value), Contains.Item(1));
        Assert.That(result.Select(s => s.Value), Contains.Item(2));
        Assert.That(result.Select(s => s.Value), Contains.Item(3));
    }

    [Test]
    public async Task GetSettlementsForRegion_WithNonExistentRegion_ShouldReturnEmpty()
    {
        // Given - no regions
        GetSettlementsForRegionQuery query = new(new RegionTag("nonexistent"));

        // When
        IReadOnlyCollection<SettlementId> result = await _getSettlementsHandler.HandleAsync(query, CancellationToken.None);

        // Then
        Assert.That(result, Is.Empty);
    }

    #endregion

    #region Helper Methods

    private async Task RegisterTestRegion(
        string tag = TestRegionTag,
        string name = TestRegionName,
        List<SettlementId>? settlements = null)
    {
        List<AreaDefinition> areas = new()
        {
            new AreaDefinition(
                new AreaTag(TestAreaResRef),
                new List<string> { "wilderness" },
                new EnvironmentData(Climate.Temperate, EconomyQuality.Average, new QualityRange()))
        };

        RegisterRegionCommand command = new(
            new RegionTag(tag),
            name,
            areas,
            settlements ?? new List<SettlementId>());

        await _registerHandler.HandleAsync(command, CancellationToken.None);
    }

    /// <summary>
    /// Simple in-memory event bus for testing that captures published events.
    /// </summary>
    private class TestEventBus : IEventBus
    {
        private readonly List<IDomainEvent> _events;

        public TestEventBus(List<IDomainEvent> events)
        {
            _events = events;
        }

        public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
            where TEvent : IDomainEvent
        {
            _events.Add(@event);
            return Task.CompletedTask;
        }

        public void Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler) where TEvent : IDomainEvent
        {
            // Not needed for these tests
        }
    }

    #endregion
}

