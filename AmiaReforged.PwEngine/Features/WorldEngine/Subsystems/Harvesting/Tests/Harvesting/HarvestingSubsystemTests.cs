using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Tests.Helpers;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.CharacterData;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Runtime;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting.Application;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Implementations;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions.Handlers;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions.Tests;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.ItemData;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes.ResourceNodeData;
using Anvil.API;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting.Tests.Harvesting;

/// <summary>
/// Tests for <see cref="HarvestingSubsystem"/> as a thin dispatch wrapper:
/// command/query translation, id parsing, harvest-status mapping, and the
/// documented unsupported operations. Live harvest behavior itself is covered
/// by HarvestingCqrsTests / WildcardHarvestEffectTests /
/// HarvestViaInteractionFrameworkTests — this fixture must not duplicate them.
/// </summary>
[TestFixture]
public class HarvestingSubsystemTests
{
    private IResourceNodeInstanceRepository _nodeRepository = null!;
    private IResourceNodeDefinitionRepository _definitionRepository = null!;
    private ICharacterRepository _characterRepository = null!;
    private ICharacterKnowledgeRepository _knowledgeRepository = null!;
    private IEventBus _eventBus = null!;

    private const string TestArea = "test_area";
    private const string TestResourceTag = "iron_ore";
    private const string TestItemTag = "item_iron_ore";

    [SetUp]
    public void SetUp()
    {
        _nodeRepository = new InMemoryResourceNodeInstanceRepository();
        _definitionRepository = new InMemoryResourceNodeDefinitionRepository();
        _characterRepository = new RuntimeCharacterRepository();
        _knowledgeRepository = InMemoryCharacterKnowledgeRepository.Create();
        _eventBus = new TestEventBus([]);

        ((InMemoryResourceNodeDefinitionRepository)_definitionRepository).Create(
            new ResourceNodeDefinition(
                1,
                ResourceType.Ore,
                TestResourceTag,
                new HarvestContext(ItemForm.ToolPick),
                [new HarvestOutput(TestItemTag, 1)],
                Uses: 50,
                BaseHarvestRounds: 0));
    }

    private HarvestingSubsystem CreateSubsystem(
        Func<object, CommandResult>? commandResponder = null,
        Func<object, object?>? queryResponder = null)
    {
        return new HarvestingSubsystem(
            new StubCommandDispatcher(commandResponder ?? (_ => CommandResult.Ok())),
            new StubQueryDispatcher(queryResponder ?? (_ => null)));
    }

    private HarvestingSubsystem CreateLiveSubsystem()
    {
        InteractionSessionManager sessionManager = new();
        HarvestInteractionHandler harvestHandler = new(_nodeRepository, _eventBus);
        InteractionHandlerRegistry registry = new([(IInteractionHandler)harvestHandler]);
        PerformInteractionCommandHandler performInteraction = new(
            sessionManager,
            _characterRepository,
            registry,
            new InMemoryInteractionDefinitionRepository(),
            _eventBus);

        return new HarvestingSubsystem(
            new RoutingCommandDispatcher(
                new RegisterNodeCommandHandler(_nodeRepository, _definitionRepository, _eventBus),
                performInteraction,
                new DestroyNodeCommandHandler(_nodeRepository, _eventBus)),
            new RoutingQueryDispatcher(
                new GetNodeByIdQueryHandler(_nodeRepository),
                new GetNodesForAreaQueryHandler(_nodeRepository),
                new GetNodeStateQueryHandler(_nodeRepository)));
    }

    #region Despawn

    [Test]
    public async Task Despawn_WithValidId_DispatchesDestroyNodeCommand()
    {
        Guid id = Guid.NewGuid();
        List<object> seen = [];
        HarvestingSubsystem subsystem = CreateSubsystem(
            commandResponder: cmd => { seen.Add(cmd); return CommandResult.Ok(); });

        CommandResult result = await subsystem.DespawnResourceNodeAsync(id.ToString());

        Assert.That(result.Success, Is.True);
        Assert.That(seen, Has.Count.EqualTo(1));
        DestroyNodeCommand dispatched = (DestroyNodeCommand)seen[0];
        Assert.That(dispatched.NodeInstanceId, Is.EqualTo(id));
    }

    [Test]
    public async Task Despawn_WithInvalidId_FailsWithoutDispatching()
    {
        List<object> seen = [];
        HarvestingSubsystem subsystem = CreateSubsystem(
            commandResponder: cmd => { seen.Add(cmd); return CommandResult.Ok(); });

        CommandResult result = await subsystem.DespawnResourceNodeAsync("not-a-guid");

        Assert.That(result.Success, Is.False);
        Assert.That(seen, Is.Empty);
    }

    #endregion

    #region Harvest status mapping

    [Test]
    public async Task Harvest_MapsHandlerStatuses_ToHarvestResult()
    {
        CharacterId characterId = CharacterId.From(Guid.NewGuid());
        Guid nodeId = Guid.NewGuid();

        Assert.That(
            await CreateSubsystem(commandResponder: _ => CommandResult.Ok(
                new Dictionary<string, object> { ["status"] = "InProgress" }))
                .HarvestResourceAsync(characterId, nodeId.ToString()),
            Is.EqualTo(HarvestResult.InProgress));

        Assert.That(
            await CreateSubsystem(commandResponder: _ => CommandResult.Ok(
                new Dictionary<string, object> { ["status"] = "Completed" }))
                .HarvestResourceAsync(characterId, nodeId.ToString()),
            Is.EqualTo(HarvestResult.Finished));

        Assert.That(
            await CreateSubsystem(commandResponder: _ => CommandResult.Ok(
                new Dictionary<string, object> { ["status"] = "NodeDepleted" }))
                .HarvestResourceAsync(characterId, nodeId.ToString()),
            Is.EqualTo(HarvestResult.Finished));

        Assert.That(
            await CreateSubsystem(commandResponder: _ => CommandResult.Fail("Required tool not equipped"))
                .HarvestResourceAsync(characterId, nodeId.ToString()),
            Is.EqualTo(HarvestResult.Failed));

        Assert.That(
            await CreateSubsystem()
                .HarvestResourceAsync(characterId, "not-a-guid"),
            Is.EqualTo(HarvestResult.Failed));
    }

    [Test]
    public async Task Harvest_DispatchesCommandWithCharacterAndNodeIds()
    {
        CharacterId characterId = CharacterId.From(Guid.NewGuid());
        Guid nodeId = Guid.NewGuid();
        List<object> seen = [];
        HarvestingSubsystem subsystem = CreateSubsystem(
            commandResponder: cmd =>
            {
                seen.Add(cmd);
                return CommandResult.Ok(new Dictionary<string, object> { ["status"] = "InProgress" });
            });

        await subsystem.HarvestResourceAsync(characterId, nodeId.ToString());

        PerformInteractionCommand dispatched = (PerformInteractionCommand)seen[0];
        Assert.That(dispatched.CharacterId, Is.EqualTo(characterId));
        Assert.That(dispatched.InteractionTag, Is.EqualTo("harvesting"));
        Assert.That(dispatched.TargetId, Is.EqualTo(nodeId));
    }

    #endregion

    #region Reads

    [Test]
    public async Task GetResourceNode_WithInvalidId_ReturnsNullWithoutDispatching()
    {
        List<object> seenQueries = [];
        HarvestingSubsystem subsystem = CreateSubsystem(
            queryResponder: q => { seenQueries.Add(q); return null; });

        Assert.That(await subsystem.GetResourceNodeAsync("not-a-guid"), Is.Null);
        Assert.That(seenQueries, Is.Empty);
    }

    [Test]
    public async Task GetHarvestContext_WithInvalidId_ReturnsNullWithoutDispatching()
    {
        List<object> seenQueries = [];
        HarvestingSubsystem subsystem = CreateSubsystem(
            queryResponder: q => { seenQueries.Add(q); return null; });

        Assert.That(
            await subsystem.GetHarvestContextAsync(CharacterId.From(Guid.NewGuid()), "not-a-guid"),
            Is.Null);
        Assert.That(seenQueries, Is.Empty);
    }

    [Test]
    public async Task CanHarvest_WithInvalidId_ReturnsFalseWithoutDispatching()
    {
        List<object> seenQueries = [];
        HarvestingSubsystem subsystem = CreateSubsystem(
            queryResponder: q => { seenQueries.Add(q); return null; });

        Assert.That(
            await subsystem.CanHarvestAsync(CharacterId.From(Guid.NewGuid()), "not-a-guid"),
            Is.False);
        Assert.That(seenQueries, Is.Empty);
    }

    [Test]
    public async Task CanHarvest_ReflectsRemainingUses()
    {
        Guid nodeId = Guid.NewGuid();
        CharacterId characterId = CharacterId.From(Guid.NewGuid());

        HarvestingSubsystem withUses = CreateSubsystem(queryResponder: _ =>
            new NodeStateDto(nodeId, TestResourceTag, 3, IPQuality.Average, TestArea));
        Assert.That(await withUses.CanHarvestAsync(characterId, nodeId.ToString()), Is.True);

        HarvestingSubsystem depleted = CreateSubsystem(queryResponder: _ =>
            new NodeStateDto(nodeId, TestResourceTag, 0, IPQuality.Average, TestArea));
        Assert.That(await depleted.CanHarvestAsync(characterId, nodeId.ToString()), Is.False);

        HarvestingSubsystem missing = CreateSubsystem(queryResponder: _ => null);
        Assert.That(await missing.CanHarvestAsync(characterId, nodeId.ToString()), Is.False);
    }

    #endregion

    #region Unsupported operations

    [Test]
    public async Task SpawnResourceNode_ReturnsExplicitNotSupportedFailure()
    {
        HarvestingSubsystem subsystem = CreateSubsystem();

        CommandResult result = await subsystem.SpawnResourceNodeAsync(TestResourceTag, TestArea, 1f, 2f, 3f);

        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("Not supported"));
    }

    [Test]
    public async Task HistoryAndLastHarvest_ReturnEmptyWithoutBackingStore()
    {
        HarvestingSubsystem subsystem = CreateSubsystem();
        CharacterId characterId = CharacterId.From(Guid.NewGuid());

        Assert.That(await subsystem.GetHarvestHistoryAsync(characterId), Is.Empty);
        Assert.That(await subsystem.GetLastHarvestTimeAsync(characterId, Guid.NewGuid().ToString()), Is.Null);
    }

    #endregion

    #region End-to-end through real handlers

    [Test]
    public async Task LiveSubsystem_RegisterThenGetContextAreaDespawn_RoundTrips()
    {
        HarvestingSubsystem subsystem = CreateLiveSubsystem();

        CommandResult registered = await new RegisterNodeCommandHandler(
                _nodeRepository, _definitionRepository, _eventBus)
            .HandleAsync(new RegisterNodeCommand(
                null, TestResourceTag, TestArea, 10f, 20f, 0f, 0f, IPQuality.Average, 5));
        Guid nodeId = (Guid)registered.Data!["nodeInstanceId"]!;

        SpawnedNode? node = await subsystem.GetResourceNodeAsync(nodeId.ToString());
        Assert.That(node, Is.Not.Null);
        Assert.That(node!.Instance.Id, Is.EqualTo(nodeId));
        Assert.That(node.Placeable, Is.Null); // no runtime placeable off-server

        List<SpawnedNode> areaNodes = await subsystem.GetAreaResourceNodesAsync(TestArea);
        Assert.That(areaNodes, Has.Count.EqualTo(1));

        HarvestContext? context = await subsystem.GetHarvestContextAsync(
            CharacterId.From(Guid.NewGuid()), nodeId.ToString());
        Assert.That(context, Is.Not.Null);
        Assert.That(context!.RequiredItemType, Is.EqualTo(ItemForm.ToolPick));

        CharacterId characterId = CharacterId.From(Guid.NewGuid());
        Assert.That(await subsystem.CanHarvestAsync(characterId, nodeId.ToString()), Is.True);

        CommandResult despawned = await subsystem.DespawnResourceNodeAsync(nodeId.ToString());
        Assert.That(despawned.Success, Is.True);
        Assert.That(await subsystem.GetResourceNodeAsync(nodeId.ToString()), Is.Null);
        Assert.That(await subsystem.CanHarvestAsync(characterId, nodeId.ToString()), Is.False);
    }

    [Test]
    public async Task LiveSubsystem_HarvestThroughWrapper_ProgressesToFinished()
    {
        HarvestingSubsystem subsystem = CreateLiveSubsystem();

        CommandResult registered = await new RegisterNodeCommandHandler(
                _nodeRepository, _definitionRepository, _eventBus)
            .HandleAsync(new RegisterNodeCommand(
                null, TestResourceTag, TestArea, 10f, 20f, 0f, 0f, IPQuality.Average, 5));
        Guid nodeId = (Guid)registered.Data!["nodeInstanceId"]!;

        TestCharacter character = new TestCharacter(
            new Dictionary<EquipmentSlots, ItemSnapshot>
            {
                [EquipmentSlots.RightHand] = new ItemSnapshot(
                    "test_tool", "Test Tool", "A tool for testing",
                    IPQuality.Average, [MaterialEnum.Iron], ItemForm.ToolPick, 0, null)
            },
            new List<SkillData>(),
            CharacterId.From(Guid.NewGuid()),
            _knowledgeRepository,
            null,
            null);
        _characterRepository.Add(character);

        // BaseHarvestRounds = 0, so a single harvest completes immediately (Uses 5 -> 4).
        HarvestResult result = await subsystem.HarvestResourceAsync(character.GetId(), nodeId.ToString());

        Assert.That(result, Is.EqualTo(HarvestResult.Finished));
        Assert.That(await subsystem.CanHarvestAsync(character.GetId(), nodeId.ToString()), Is.True);
    }

    #endregion

    #region Test doubles

    /// <summary>Minimal in-memory event bus capturing published events.</summary>
    private sealed class TestEventBus(List<IDomainEvent> events) : IEventBus
    {
        public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
            where TEvent : IDomainEvent
        {
            events.Add(@event);
            return Task.CompletedTask;
        }

        public void Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler) where TEvent : IDomainEvent
        {
        }
    }

    #region Dispatcher fakes

    private sealed class StubCommandDispatcher(Func<object, CommandResult> responder) : ICommandDispatcher
    {
        public Task<CommandResult> DispatchAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
            where TCommand : ICommand
            => Task.FromResult(responder(command!));

        public Task<BatchCommandResult> DispatchBatchAsync<TCommand>(
            IEnumerable<TCommand> commands, BatchExecutionOptions? options = null,
            CancellationToken cancellationToken = default) where TCommand : ICommand
            => throw new NotImplementedException();
    }

    private sealed class StubQueryDispatcher(Func<object, object?> responder) : IQueryDispatcher
    {
        public Task<TResult> DispatchAsync<TQuery, TResult>(TQuery query, CancellationToken cancellationToken = default)
            where TQuery : IQuery<TResult>
            => Task.FromResult((TResult)responder(query!)!);
    }

    private sealed class RoutingCommandDispatcher(
        RegisterNodeCommandHandler register,
        PerformInteractionCommandHandler performInteraction,
        DestroyNodeCommandHandler destroy) : ICommandDispatcher
    {
        public async Task<CommandResult> DispatchAsync<TCommand>(TCommand command, CancellationToken cancellationToken = default)
            where TCommand : ICommand
        {
            object result = command switch
            {
                RegisterNodeCommand c => await register.HandleAsync(c, cancellationToken),
                PerformInteractionCommand c => await performInteraction.HandleAsync(c, cancellationToken),
                DestroyNodeCommand c => await destroy.HandleAsync(c, cancellationToken),
                _ => throw new InvalidOperationException($"Unexpected command {command?.GetType().Name}")
            };
            return (CommandResult)result;
        }

        public Task<BatchCommandResult> DispatchBatchAsync<TCommand>(
            IEnumerable<TCommand> commands, BatchExecutionOptions? options = null,
            CancellationToken cancellationToken = default) where TCommand : ICommand
            => throw new NotImplementedException();
    }

    private sealed class RoutingQueryDispatcher(
        GetNodeByIdQueryHandler byId,
        GetNodesForAreaQueryHandler forArea,
        GetNodeStateQueryHandler state) : IQueryDispatcher
    {
        public async Task<TResult> DispatchAsync<TQuery, TResult>(TQuery query, CancellationToken cancellationToken = default)
            where TQuery : IQuery<TResult>
        {
            object? result = query switch
            {
                GetNodeByIdQuery q => await byId.HandleAsync(q, cancellationToken),
                GetNodesForAreaQuery q => await forArea.HandleAsync(q, cancellationToken),
                GetNodeStateQuery q => await state.HandleAsync(q, cancellationToken),
                _ => throw new InvalidOperationException($"Unexpected query {query?.GetType().Name}")
            };
            return (TResult)result!;
        }
    }

    #endregion // Dispatcher fakes

    #endregion // Test doubles
}
