using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.CharacterData;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Runtime;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting.Application;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.ItemData;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes.ResourceNodeData;
using AmiaReforged.PwEngine.Features.WorldEngine.Tests.Systems.WorldEngine.Helpers;
using Anvil.API;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting.Tests.Harvesting;

/// <summary>
/// BDD-style tests for Harvesting CQRS implementation.
/// Tests demonstrate the command/query pattern with event publishing.
/// </summary>
[TestFixture]
public class HarvestingCqrsTests
{
    private IResourceNodeInstanceRepository _nodeRepository = null!;
    private IResourceNodeDefinitionRepository _definitionRepository = null!;
    private ICharacterRepository _characterRepository = null!;
    private IEventBus _eventBus = null!;
    private ICharacterKnowledgeRepository _knowledgeRepository = null!;

    private RegisterNodeCommandHandler _registerNodeHandler = null!;
    private HarvestResourceCommandHandler _harvestHandler = null!;
    private DestroyNodeCommandHandler _destroyNodeHandler = null!;
    private ClearAreaNodesCommandHandler _clearAreaHandler = null!;
    private GetNodesForAreaQueryHandler _getNodesForAreaHandler = null!;
    private GetNodeByIdQueryHandler _getNodeByIdHandler = null!;
    private GetNodeStateQueryHandler _getNodeStateHandler = null!;

    private List<IDomainEvent> _publishedEvents = null!;

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
        _publishedEvents = new List<IDomainEvent>();
        _eventBus = new TestEventBus(_publishedEvents);

        // Set up handlers
        _registerNodeHandler = new RegisterNodeCommandHandler(_nodeRepository, _definitionRepository, _eventBus);
        _harvestHandler = new HarvestResourceCommandHandler(_nodeRepository, _characterRepository, _eventBus);
        _destroyNodeHandler = new DestroyNodeCommandHandler(_nodeRepository, _eventBus);
        _clearAreaHandler = new ClearAreaNodesCommandHandler(_nodeRepository, _eventBus);
        _getNodesForAreaHandler = new GetNodesForAreaQueryHandler(_nodeRepository);
        _getNodeByIdHandler = new GetNodeByIdQueryHandler(_nodeRepository);
        _getNodeStateHandler = new GetNodeStateQueryHandler(_nodeRepository);

        // Add test resource definition
        ResourceNodeDefinition definition = new ResourceNodeDefinition(
            1,
            ResourceType.Ore,
            TestResourceTag,
            new HarvestContext(JobSystemItemType.ToolPick),
            [new HarvestOutput(TestItemTag, 1)],
            1); // baseHarvestRounds as 6th positional parameter

        ((InMemoryResourceNodeDefinitionRepository)_definitionRepository).Create(definition);
    }

    #region Register Node Tests

    [Test]
    public async Task RegisterNode_WithValidData_ShouldSucceed()
    {
        // Given
        RegisterNodeCommand command = new RegisterNodeCommand(
            null,
            TestResourceTag,
            TestArea,
            10.0f,
            20.0f,
            0.0f,
            0.0f,
            IPQuality.Average,
            5);

        // When
        CommandResult result = await _registerNodeHandler.HandleAsync(command, CancellationToken.None);

        // Then
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data, Is.Not.Null);
        Guid nodeInstanceId = (Guid)result.Data!["nodeInstanceId"];

        // And the node should be in the repository
        List<ResourceNodeInstance> nodes = _nodeRepository.GetInstancesByArea(TestArea);
        Assert.That(nodes, Has.Count.EqualTo(1));
        Assert.That(nodes[0].Id, Is.EqualTo(nodeInstanceId));

        // And an event should be published
        Assert.That(_publishedEvents, Has.Count.EqualTo(1));
        NodeRegisteredEvent? evt = _publishedEvents[0] as NodeRegisteredEvent;
        Assert.That(evt, Is.Not.Null);
        Assert.That(evt!.NodeInstanceId, Is.EqualTo(nodeInstanceId));
        Assert.That(evt.ResourceTag, Is.EqualTo(TestResourceTag));
    }

    [Test]
    public async Task RegisterNode_WithInvalidResource_ShouldFail()
    {
        // Given
        RegisterNodeCommand command = new RegisterNodeCommand(
            null,
            "nonexistent_resource",
            TestArea,
            10.0f,
            20.0f,
            0.0f,
            0.0f,
            IPQuality.Average,
            5);

        // When
        CommandResult result = await _registerNodeHandler.HandleAsync(command, CancellationToken.None);

        // Then
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("not found"));
        Assert.That(_publishedEvents, Is.Empty);
    }

    #endregion

    #region Query Tests

    [Test]
    public async Task GetNodesForArea_WithMultipleNodes_ShouldReturnAll()
    {
        // Given - register three nodes in same area
        await _registerNodeHandler.HandleAsync(
            new RegisterNodeCommand(null, TestResourceTag, TestArea, 1, 1, 0, 0, IPQuality.Average, 5),
            CancellationToken.None);
        await _registerNodeHandler.HandleAsync(
            new RegisterNodeCommand(null, TestResourceTag, TestArea, 2, 2, 0, 0, IPQuality.BelowAverage, 5),
            CancellationToken.None);
        await _registerNodeHandler.HandleAsync(
            new RegisterNodeCommand(null, TestResourceTag, TestArea, 3, 3, 0, 0, IPQuality.AboveAverage, 5),
            CancellationToken.None);

        // When
        GetNodesForAreaQuery query = new GetNodesForAreaQuery(TestArea);
        List<ResourceNodeInstance> nodes = await _getNodesForAreaHandler.HandleAsync(query, CancellationToken.None);

        // Then
        Assert.That(nodes, Has.Count.EqualTo(3));
    }

    [Test]
    public async Task GetNodeById_WithExistingNode_ShouldReturnNode()
    {
        // Given
        CommandResult registerResult = await _registerNodeHandler.HandleAsync(
            new RegisterNodeCommand(null, TestResourceTag, TestArea, 1, 1, 0, 0, IPQuality.Average, 5),
            CancellationToken.None);
        Guid nodeInstanceId = (Guid)registerResult.Data!["nodeInstanceId"];

        // When
        GetNodeByIdQuery query = new GetNodeByIdQuery(nodeInstanceId);
        ResourceNodeInstance? node = await _getNodeByIdHandler.HandleAsync(query, CancellationToken.None);

        // Then
        Assert.That(node, Is.Not.Null);
        Assert.That(node!.Id, Is.EqualTo(nodeInstanceId));
    }

    [Test]
    public async Task GetNodeState_WithExistingNode_ShouldReturnState()
    {
        // Given
        CommandResult registerResult = await _registerNodeHandler.HandleAsync(
            new RegisterNodeCommand(null, TestResourceTag, TestArea, 1, 1, 0, 0, IPQuality.Average, 5),
            CancellationToken.None);
        Guid nodeInstanceId = (Guid)registerResult.Data!["nodeInstanceId"];

        // When
        GetNodeStateQuery query = new GetNodeStateQuery(nodeInstanceId);
        NodeStateDto? state = await _getNodeStateHandler.HandleAsync(query, CancellationToken.None);

        // Then
        Assert.That(state, Is.Not.Null);
        Assert.That(state!.NodeInstanceId, Is.EqualTo(nodeInstanceId));
        Assert.That(state.ResourceTag, Is.EqualTo(TestResourceTag));
        Assert.That(state.RemainingUses, Is.EqualTo(5));
        Assert.That(state.Quality, Is.EqualTo(IPQuality.Average));
    }

    #endregion

    #region Harvest Tests

    [Test]
    public async Task HarvestResource_WithValidConditions_ShouldSucceed()
    {
        // Given - a registered node
        CommandResult registerResult = await _registerNodeHandler.HandleAsync(
            new RegisterNodeCommand(null, TestResourceTag, TestArea, 1, 1, 0, 0, IPQuality.Average, 5),
            CancellationToken.None);
        Guid nodeInstanceId = (Guid)registerResult.Data!["nodeInstanceId"];

        // And a character with the right tool
        ICharacter character = CreateCharacterWithTool(JobSystemItemType.ToolPick);
        _characterRepository.Add(character);

        _publishedEvents.Clear(); // Clear registration event

        // When
        HarvestResourceCommand harvestCommand = new HarvestResourceCommand(character.GetId().Value, nodeInstanceId);
        CommandResult harvestResult = await _harvestHandler.HandleAsync(harvestCommand, CancellationToken.None);

        // Then
        Assert.That(harvestResult.Success, Is.True);
        Assert.That(harvestResult.Data!["status"], Is.EqualTo("Completed"));

        // And the node should have one less use
        NodeStateDto? state = await _getNodeStateHandler.HandleAsync(
            new GetNodeStateQuery(nodeInstanceId),
            CancellationToken.None);
        Assert.That(state!.RemainingUses, Is.EqualTo(4));

        // And a harvest event should be published
        ResourceHarvestedEvent? harvestEvent = _publishedEvents.OfType<ResourceHarvestedEvent>().FirstOrDefault();
        Assert.That(harvestEvent, Is.Not.Null);
        Assert.That(harvestEvent!.Items, Has.Length.EqualTo(1));
        Assert.That(harvestEvent.Items[0].ItemTag, Is.EqualTo(TestItemTag));
    }

    [Test]
    public async Task HarvestResource_WithoutRequiredTool_ShouldFail()
    {
        // Given - a node that requires a pick
        CommandResult registerResult = await _registerNodeHandler.HandleAsync(
            new RegisterNodeCommand(null, TestResourceTag, TestArea, 1, 1, 0, 0, IPQuality.Average, 5),
            CancellationToken.None);
        Guid nodeInstanceId = (Guid)registerResult.Data!["nodeInstanceId"];

        // And a character with the wrong tool
        ICharacter character = CreateCharacterWithTool(JobSystemItemType.ToolHammer);
        _characterRepository.Add(character);

        _publishedEvents.Clear();

        // When
        HarvestResourceCommand harvestCommand = new HarvestResourceCommand(character.GetId().Value, nodeInstanceId);
        CommandResult harvestResult = await _harvestHandler.HandleAsync(harvestCommand, CancellationToken.None);

        // Then
        Assert.That(harvestResult.Success, Is.False);
        Assert.That(harvestResult.ErrorMessage, Does.Contain("tool"));

        // And no harvest event should be published
        Assert.That(_publishedEvents.OfType<ResourceHarvestedEvent>().ToList(), Is.Empty);
    }

    [Test]
    public async Task HarvestResource_MultipleRounds_ShouldShowProgress()
    {
        // Given - a resource that takes 3 rounds to harvest
        ResourceNodeDefinition definition = new ResourceNodeDefinition(
            2,
            ResourceType.Ore,
            "slow_ore",
            new HarvestContext(JobSystemItemType.ToolPick),
            [new HarvestOutput(TestItemTag, 1)],
            Uses: 10,              // 6th parameter
            BaseHarvestRounds: 3); // 7th parameter
        ((InMemoryResourceNodeDefinitionRepository)_definitionRepository).Create(definition);

        CommandResult registerResult = await _registerNodeHandler.HandleAsync(
            new RegisterNodeCommand(null, "slow_ore", TestArea, 1, 1, 0, 0, IPQuality.Average, 5),
            CancellationToken.None);
        Guid nodeInstanceId = (Guid)registerResult.Data!["nodeInstanceId"];

        ICharacter character = CreateCharacterWithTool(JobSystemItemType.ToolPick);
        _characterRepository.Add(character);

        _publishedEvents.Clear();

        // When - first harvest attempt
        HarvestResourceCommand command = new HarvestResourceCommand(character.GetId().Value, nodeInstanceId);
        CommandResult result1 = await _harvestHandler.HandleAsync(command, CancellationToken.None);

        // Then
        Assert.That(result1.Success, Is.True);
        Assert.That(result1.Data!["status"], Is.EqualTo("InProgress"));
        Assert.That(_publishedEvents.OfType<ResourceHarvestedEvent>().ToList(), Is.Empty);

        // When - second harvest attempt
        CommandResult result2 = await _harvestHandler.HandleAsync(command, CancellationToken.None);

        // Then
        Assert.That(result2.Success, Is.True);
        Assert.That(result2.Data!["status"], Is.EqualTo("InProgress"));

        // When - third harvest attempt
        CommandResult result3 = await _harvestHandler.HandleAsync(command, CancellationToken.None);

        // Then - harvest completes
        Assert.That(result3.Success, Is.True);
        Assert.That(result3.Data!["status"], Is.EqualTo("Completed"));
        Assert.That(_publishedEvents.OfType<ResourceHarvestedEvent>().ToList(), Has.Count.EqualTo(1));
    }

    [Test]
    public async Task HarvestResource_UntilDepleted_ShouldPublishDepletedEvent()
    {
        // Given - a node with only 1 use
        CommandResult registerResult = await _registerNodeHandler.HandleAsync(
            new RegisterNodeCommand(null, TestResourceTag, TestArea, 1, 1, 0, 0, IPQuality.Average, 1),
            CancellationToken.None);
        Guid nodeInstanceId = (Guid)registerResult.Data!["nodeInstanceId"];

        ICharacter character = CreateCharacterWithTool(JobSystemItemType.ToolPick);
        _characterRepository.Add(character);

        _publishedEvents.Clear();

        // When - harvesting the last use
        HarvestResourceCommand command = new HarvestResourceCommand(character.GetId().Value, nodeInstanceId);
        CommandResult result = await _harvestHandler.HandleAsync(command, CancellationToken.None);

        // Then
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data!["status"], Is.EqualTo("NodeDepleted"));

        // And both harvest and depleted events should be published
        Assert.That(_publishedEvents.OfType<ResourceHarvestedEvent>().ToList(), Has.Count.EqualTo(1));
        Assert.That(_publishedEvents.OfType<NodeDepletedEvent>().ToList(), Has.Count.EqualTo(1));

        // And the node should be removed from the repository
        ResourceNodeInstance? node = await _getNodeByIdHandler.HandleAsync(
            new GetNodeByIdQuery(nodeInstanceId),
            CancellationToken.None);
        Assert.That(node, Is.Null);
    }

    #endregion

    #region Destroy Node Tests

    [Test]
    public async Task DestroyNode_WithExistingNode_ShouldRemoveNode()
    {
        // Given
        CommandResult registerResult = await _registerNodeHandler.HandleAsync(
            new RegisterNodeCommand(null, TestResourceTag, TestArea, 1, 1, 0, 0, IPQuality.Average, 5),
            CancellationToken.None);
        Guid nodeInstanceId = (Guid)registerResult.Data!["nodeInstanceId"];

        _publishedEvents.Clear();

        // When
        DestroyNodeCommand command = new DestroyNodeCommand(nodeInstanceId);
        CommandResult result = await _destroyNodeHandler.HandleAsync(command, CancellationToken.None);

        // Then
        Assert.That(result.Success, Is.True);

        // And the node should be removed
        ResourceNodeInstance? node = await _getNodeByIdHandler.HandleAsync(
            new GetNodeByIdQuery(nodeInstanceId),
            CancellationToken.None);
        Assert.That(node, Is.Null);

        // And a depleted event should be published
        Assert.That(_publishedEvents.OfType<NodeDepletedEvent>().ToList(), Has.Count.EqualTo(1));
    }

    [Test]
    public async Task DestroyNode_WithNonexistentNode_ShouldFail()
    {
        // Given
        Guid nonexistentId = Guid.NewGuid();

        // When
        DestroyNodeCommand command = new DestroyNodeCommand(nonexistentId);
        CommandResult result = await _destroyNodeHandler.HandleAsync(command, CancellationToken.None);

        // Then
        Assert.That(result.Success, Is.False);
        Assert.That(result.ErrorMessage, Does.Contain("not found"));
    }

    #endregion

    #region Clear Area Tests

    [Test]
    public async Task ClearAreaNodes_WithMultipleNodes_ShouldRemoveAll()
    {
        // Given - three nodes in the same area
        await _registerNodeHandler.HandleAsync(
            new RegisterNodeCommand(null, TestResourceTag, TestArea, 1, 1, 0, 0, IPQuality.Average, 5),
            CancellationToken.None);
        await _registerNodeHandler.HandleAsync(
            new RegisterNodeCommand(null, TestResourceTag, TestArea, 2, 2, 0, 0, IPQuality.Average, 5),
            CancellationToken.None);
        await _registerNodeHandler.HandleAsync(
            new RegisterNodeCommand(null, TestResourceTag, TestArea, 3, 3, 0, 0, IPQuality.Average, 5),
            CancellationToken.None);

        _publishedEvents.Clear();

        // When
        ClearAreaNodesCommand command = new ClearAreaNodesCommand(TestArea);
        CommandResult result = await _clearAreaHandler.HandleAsync(command, CancellationToken.None);

        // Then
        Assert.That(result.Success, Is.True);
        Assert.That((int)result.Data!["nodesCleared"], Is.EqualTo(3));

        // And all nodes should be removed
        List<ResourceNodeInstance> nodes = await _getNodesForAreaHandler.HandleAsync(
            new GetNodesForAreaQuery(TestArea),
            CancellationToken.None);
        Assert.That(nodes, Is.Empty);

        // And a cleared event should be published
        NodesClearedEvent? clearedEvent = _publishedEvents.OfType<NodesClearedEvent>().FirstOrDefault();
        Assert.That(clearedEvent, Is.Not.Null);
        Assert.That(clearedEvent!.NodesCleared, Is.EqualTo(3));
    }

    #endregion

    #region Helper Methods

    private ICharacter CreateCharacterWithTool(JobSystemItemType toolType)
    {
        CharacterId characterId = CharacterId.From(Guid.NewGuid());
        TestCharacter character = new TestCharacter(
            new Dictionary<EquipmentSlots, ItemSnapshot>
            {
                [EquipmentSlots.RightHand] = new ItemSnapshot(
                    "test_tool",
                    "Test Tool",
                    "A tool for testing",
                    IPQuality.Average,
                    [MaterialEnum.Iron],
                    toolType,
                    0,
                    null)
            },
            new List<SkillData>(),
            characterId,
            _knowledgeRepository,
            null, // No membership service needed for these tests
            null);

        return character;
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

