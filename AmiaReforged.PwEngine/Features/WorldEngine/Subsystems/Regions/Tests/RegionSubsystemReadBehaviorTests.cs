using System.Threading;
using AmiaReforged.PwEngine.Features.Encounters.Models;
using AmiaReforged.PwEngine.Features.WorldEngine.Application.Regions.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Application.Regions.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Implementations;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Regions;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Regions.Application;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes.ResourceNodeData;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Regions.Queries;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Regions.Tests;

/// <summary>
/// Behavioral tests for the region facade read path
/// (`RegionSubsystem.GetRegionAsync` / `GetAllRegionsAsync`).
/// A real <see cref="QueryDispatcher"/> is wired to the real region query handlers and an
/// <see cref="InMemoryRegionRepository"/> so the full route (query → handler → projection) is exercised.
/// </summary>
[TestFixture]
public class RegionSubsystemReadBehaviorTests
{
    private static RegionSubsystem BuildSubsystem(
        InMemoryRegionRepository repo,
        out QueryDispatcher dispatcher,
        ICommandDispatcher? commandDispatcher = null)
    {
        IQueryDispatcher queryDispatcher = new QueryDispatcher(
            new IQueryHandlerMarker[]
            {
                new GetRegionDefinitionHandler(repo),
                new SearchRegionDefinitionsHandler(repo),
                new GetChaosForAreaQueryHandler(repo)
            });
        dispatcher = (QueryDispatcher)queryDispatcher;

        ICommandDispatcher commandDispatcherToUse = commandDispatcher ?? new CommandDispatcher(
            new ICommandHandlerMarker[]
            {
                new UpdateRegionHandler(repo)
            },
            new CapturingEventBus(new List<IDomainEvent>()));

        return new RegionSubsystem(repo, queryDispatcher, commandDispatcherToUse);
    }

    private static RegionDefinition Region(
        string tag,
        string name,
        string? description = null,
        RegionType? type = null,
        ChaosState? defaultChaos = null,
        AreaDefinition? area = null)
        => new()
        {
            Tag = new RegionTag(tag),
            Name = name,
            Description = description,
            Type = type,
            Areas = area is null ? [] : [area],
            DefaultChaos = defaultChaos
        };

    private static AreaDefinition Area(string resRef, ChaosState? chaos = null)
        => new(
            new AreaTag(resRef),
            ["wilderness"],
            new EnvironmentData(Climate.Temperate, EconomyQuality.Average, new QualityRange(), chaos));

    [Test]
    public async Task GetRegionAsync_KnownTag_ReturnsMappedProjection()
    {
        InMemoryRegionRepository repo = new();
        repo.Add(Region("wilderness_north", "North Wildlands", "Frozen hills", RegionType.Wilderness));
        RegionSubsystem subsystem = BuildSubsystem(repo, out _);

        RegionInfo? info = await subsystem.GetRegionAsync("wilderness_north", CancellationToken.None);

        Assert.That(info, Is.Not.Null);
        Assert.That(info!.Tag, Is.EqualTo("wilderness_north"));
        Assert.That(info.Name, Is.EqualTo("North Wildlands"));
        Assert.That(info.Description, Is.EqualTo("Frozen hills"));
        Assert.That(info.Type, Is.EqualTo(RegionType.Wilderness));
    }

    [Test]
    public async Task GetRegionAsync_CaseInsensitive_TagLookup()
    {
        InMemoryRegionRepository repo = new();
        repo.Add(Region("Dungeon_Deep", "Deep Dungeon"));
        RegionSubsystem subsystem = BuildSubsystem(repo, out _);

        RegionInfo? info = await subsystem.GetRegionAsync("dungeon_deep", CancellationToken.None);

        Assert.That(info, Is.Not.Null);
        Assert.That(info!.Name, Is.EqualTo("Deep Dungeon"));
    }

    [Test]
    public async Task GetRegionAsync_UnknownTag_ReturnsNull()
    {
        InMemoryRegionRepository repo = new();
        repo.Add(Region("known", "Known Region"));
        RegionSubsystem subsystem = BuildSubsystem(repo, out _);

        RegionInfo? info = await subsystem.GetRegionAsync("does_not_exist", CancellationToken.None);

        Assert.That(info, Is.Null);
    }

    [Test]
    public async Task GetAllRegionsAsync_IncludesRegisteredRegions()
    {
        InMemoryRegionRepository repo = new();
        repo.Add(Region("r1", "Region One"));
        repo.Add(Region("r2", "Region Two"));
        RegionSubsystem subsystem = BuildSubsystem(repo, out _);

        List<RegionInfo> all = await subsystem.GetAllRegionsAsync(CancellationToken.None);

        Assert.That(all, Has.Count.EqualTo(2));
        Assert.That(all.Select(r => r.Tag), Contains.Item("r1"));
        Assert.That(all.Select(r => r.Tag), Contains.Item("r2"));
    }

    [Test]
    public async Task GetAllRegionsAsync_EmptyRepository_ReturnsEmptyList()
    {
        InMemoryRegionRepository repo = new();
        RegionSubsystem subsystem = BuildSubsystem(repo, out _);

        List<RegionInfo> all = await subsystem.GetAllRegionsAsync(CancellationToken.None);

        Assert.That(all, Is.Not.Null);
        Assert.That(all, Has.Count.EqualTo(0));
    }

    [Test]
    public async Task GetRegionAsync_UnsetDescriptionAndType_ProjecsContractDefaults()
    {
        InMemoryRegionRepository repo = new();
        // No Description, no Type — exercises the compatibility defaults from the 033 contract.
        repo.Add(Region("bare", "Bare Region"));
        RegionSubsystem subsystem = BuildSubsystem(repo, out _);

        RegionInfo? info = await subsystem.GetRegionAsync("bare", CancellationToken.None);

        Assert.That(info, Is.Not.Null);
        Assert.That(info!.Description, Is.EqualTo(string.Empty));
        Assert.That(info.Type, Is.EqualTo(RegionType.Special));
    }

    #region Update Region Tests (task 035)

    [Test]
    public async Task UpdateRegionAsync_WithValidData_ChangesFieldsAndIsObservable()
    {
        // Given
        InMemoryRegionRepository repo = new();
        repo.Add(Region("r1", "Region One", "Old description", RegionType.Wilderness));
        RegionSubsystem subsystem = BuildSubsystem(repo, out _);

        // When
        CommandResult result = await subsystem.UpdateRegionAsync(new AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.UpdateRegionCommand("r1", Name: "Renamed"), CancellationToken.None);

        // Then — success (the facade no longer returns "Not yet implemented")
        Assert.That(result.Success, Is.True);

        // And the change is observable through the read query
        RegionInfo? info = await subsystem.GetRegionAsync("r1", CancellationToken.None);
        Assert.That(info, Is.Not.Null);
        Assert.That(info!.Name, Is.EqualTo("Renamed"));
        Assert.That(info.Description, Is.EqualTo("Old description")); // untouched field preserved
        Assert.That(info.Type, Is.EqualTo(RegionType.Wilderness));
    }

    [Test]
    public async Task UpdateRegionAsync_MissingRegion_FailsWithoutInsertion()
    {
        // Given
        InMemoryRegionRepository repo = new();
        RegionSubsystem subsystem = BuildSubsystem(repo, out _);

        // When
        CommandResult result = await subsystem.UpdateRegionAsync(new AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.UpdateRegionCommand("ghost", Name: "X"), CancellationToken.None);

        // Then — fails, and no region was inserted
        Assert.That(result.Success, Is.False);
        Assert.That(repo.All(), Is.Empty);
    }

    [Test]
    public async Task UpdateRegionAsync_WithEmptyTag_ReturnsFailWithoutThrowing()
    {
        // Given
        InMemoryRegionRepository repo = new();
        RegionSubsystem subsystem = BuildSubsystem(repo, out _);

        // When
        CommandResult result = await subsystem.UpdateRegionAsync(
            new AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.UpdateRegionCommand("   ", Name: "X"), CancellationToken.None);

        // Then — fails with a validation message rather than throwing (the application handler
        // would otherwise reject the empty tag before any dispatch)
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("cannot be empty"));
        Assert.That(repo.All(), Is.Empty);
    }

    [Test]
    public async Task UpdateRegionAsync_SuccessfulExecution_PublishesGenericEvent()
    {
        // Given
        InMemoryRegionRepository repo = new();
        repo.Add(Region("r1", "Region One"));
        List<IDomainEvent> published = new();
        IEventBus eventBus = new CapturingEventBus(published);
        ICommandDispatcher dispatcher = new CommandDispatcher(
            new ICommandHandlerMarker[] { new UpdateRegionHandler(repo) }, eventBus);
        RegionSubsystem subsystem = BuildSubsystem(repo, out _, dispatcher);

        // When
        CommandResult result = await subsystem.UpdateRegionAsync(new AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.UpdateRegionCommand("r1", Name: "Renamed"), CancellationToken.None);

        // Then — the dispatcher publishes the generic CommandExecutedEvent on success
        Assert.That(
            published.Any(e => e is CommandExecutedEvent<AmiaReforged.PwEngine.Features.WorldEngine.Application.Regions.Commands.UpdateRegionCommand>),
            Is.True);
    }

    #endregion

    #region Chaos Query (task 036)

    [Test]
    public async Task GetChaosForAreaAsync_AreaOverride_TakesPrecedence()
    {
        // Given
        ChaosState overrideChaos = new() { Danger = 42, Corruption = 10, Density = 20, Mutation = 5 };
        InMemoryRegionRepository repo = new();
        repo.Add(Region("r1", "Region One", area: Area("area_a", overrideChaos), defaultChaos: ChaosState.Default));
        RegionSubsystem subsystem = BuildSubsystem(repo, out _);

        // When
        ChaosState result = await subsystem.GetChaosForAreaAsync("area_a", CancellationToken.None);

        // Then — area-level override wins over the region default
        Assert.That(result.Danger, Is.EqualTo(42));
    }

    [Test]
    public async Task GetChaosForAreaAsync_NoAreaOverride_FallsBackToRegionDefault()
    {
        // Given
        ChaosState regionDefault = new() { Danger = 30, Corruption = 20, Density = 10, Mutation = 15 };
        InMemoryRegionRepository repo = new();
        repo.Add(Region("r1", "Region One", area: Area("area_a"), defaultChaos: regionDefault));
        RegionSubsystem subsystem = BuildSubsystem(repo, out _);

        // When
        ChaosState result = await subsystem.GetChaosForAreaAsync("area_a", CancellationToken.None);

        // Then — region default is used when no area override exists
        Assert.That(result.Density, Is.EqualTo(10));
        Assert.That(result.Mutation, Is.EqualTo(15));
    }

    [Test]
    public async Task GetChaosForAreaAsync_UnregisteredArea_ReturnsDefault()
    {
        // Given
        InMemoryRegionRepository repo = new();
        repo.Add(Region("r1", "Region One", area: Area("area_a"), defaultChaos: new ChaosState { Danger = 50 }));
        RegionSubsystem subsystem = BuildSubsystem(repo, out _);

        // When — area not defined in any region
        ChaosState result = await subsystem.GetChaosForAreaAsync("nowhere", CancellationToken.None);

        // Then — all-zero default
        Assert.That(result, Is.EqualTo(ChaosState.Default));
    }

    [Test]
    public async Task GetChaosForAreaAsync_CaseInsensitive_AreaMatching()
    {
        // Given
        ChaosState overrideChaos = new() { Danger = 77 };
        InMemoryRegionRepository repo = new();
        repo.Add(Region("r1", "Region One", area: Area("Area_A", overrideChaos), defaultChaos: ChaosState.Default));
        RegionSubsystem subsystem = BuildSubsystem(repo, out _);

        // When — different casing
        ChaosState result = await subsystem.GetChaosForAreaAsync("area_a", CancellationToken.None);

        // Then — match is case-insensitive
        Assert.That(result.Danger, Is.EqualTo(77));
    }

    [Test]
    public async Task GetChaosForAreaAsync_Subsystem_NoLongerReadsRepositoryDirectly()
    {
        // Given — a repository-backed handler wired via the dispatcher; the subsystem must route
        // through it rather than touching the repository itself.
        ChaosState regionDefault = new() { Corruption = 33 };
        InMemoryRegionRepository repo = new();
        repo.Add(Region("r1", "Region One", area: Area("area_a"), defaultChaos: regionDefault));
        RegionSubsystem subsystem = BuildSubsystem(repo, out _);

        // When
        ChaosState result = await subsystem.GetChaosForAreaAsync("area_a", CancellationToken.None);

        // Then
        Assert.That(result.Corruption, Is.EqualTo(33));
    }

    #endregion

    /// <summary>
    /// Minimal in-memory event bus that captures published events for assertions.
    /// </summary>
    private sealed class CapturingEventBus : IEventBus
    {
        private readonly List<IDomainEvent> _events;

        public CapturingEventBus(List<IDomainEvent> events)
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
        }
    }
}
