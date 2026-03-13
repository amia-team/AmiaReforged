using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Tests.Helpers;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.CharacterData;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Runtime;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting.Application;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.KnowledgeSubsystem;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions.Handlers;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.ItemData;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes.ResourceNodeData;
using Anvil.API;
using FluentAssertions;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Interactions.Tests;

/// <summary>
/// Integration tests verifying that <see cref="HarvestInteractionHandler"/> preserves
/// identical behavior to the original <c>HarvestResourceCommandHandler</c> when
/// executed through the Interaction Framework dispatcher.
/// </summary>
[TestFixture]
public class HarvestViaInteractionFrameworkTests
{
    private IResourceNodeInstanceRepository _nodeRepository = null!;
    private IResourceNodeDefinitionRepository _definitionRepository = null!;
    private ICharacterRepository _characterRepository = null!;
    private ICharacterKnowledgeRepository _knowledgeRepository = null!;
    private IInteractionSessionManager _sessionManager = null!;
    private PerformInteractionCommandHandler _commandHandler = null!;
    private RegisterNodeCommandHandler _registerNodeHandler = null!;

    private List<IDomainEvent> _publishedEvents = null!;
    private IEventBus _eventBus = null!;

    private const string TestArea = "test_mine";
    private const string CopperOreTag = "ore_vein_copper";
    private const string CopperItemTag = "copper_ore";

    [SetUp]
    public void SetUp()
    {
        _nodeRepository = new InMemoryResourceNodeInstanceRepository();
        _definitionRepository = new InMemoryResourceNodeDefinitionRepository();
        _characterRepository = new RuntimeCharacterRepository();
        _knowledgeRepository = InMemoryCharacterKnowledgeRepository.Create();
        _sessionManager = new InteractionSessionManager();
        _publishedEvents = [];
        _eventBus = new TestEventBus(_publishedEvents);

        // Register resource definitions
        ((InMemoryResourceNodeDefinitionRepository)_definitionRepository).Create(
            new ResourceNodeDefinition(1, ResourceType.Ore, CopperOreTag,
                new HarvestContext(JobSystemItemType.ToolPick),
                [new HarvestOutput(CopperItemTag, 1)],
                Uses: 50, BaseHarvestRounds: 3));

        // Build the handler pipeline
        _registerNodeHandler = new RegisterNodeCommandHandler(
            _nodeRepository, _definitionRepository, _eventBus);

        HarvestInteractionHandler harvestHandler = new(_nodeRepository, _eventBus);
        IInteractionHandlerRegistry registry = new InteractionHandlerRegistry(
            new[] { (IInteractionHandler)harvestHandler });

        _commandHandler = new PerformInteractionCommandHandler(
            _sessionManager, _characterRepository, registry,
            new InMemoryInteractionDefinitionRepository(), _eventBus);
    }

    #region Tool Checks

    [Test]
    public async Task Harvest_without_required_tool_fails_precondition()
    {
        // Given a node requiring a pick and a character with no tool
        Guid nodeId = await RegisterNode(CopperOreTag);
        TestCharacter character = CreateCharacter(JobSystemItemType.None); // Wrong tool
        _characterRepository.Add(character);

        // When attempting to harvest
        CommandResult result = await _commandHandler.HandleAsync(
            new PerformInteractionCommand(character.GetId(), "harvesting", nodeId));

        // Then it should fail
        result.Success.Should().BeFalse();
        result.ErrorMessage.Should().Contain("tool");
    }

    [Test]
    public async Task Harvest_with_correct_tool_starts_successfully()
    {
        // Given a node and a character with the correct tool
        Guid nodeId = await RegisterNode(CopperOreTag);
        TestCharacter character = CreateCharacter(JobSystemItemType.ToolPick);
        _characterRepository.Add(character);

        // When starting the harvest
        CommandResult result = await _commandHandler.HandleAsync(
            new PerformInteractionCommand(character.GetId(), "harvesting", nodeId));

        // Then it should succeed and be in progress
        result.Success.Should().BeTrue();
        result.Data!["status"].Should().Be("InProgress");
    }

    #endregion

    #region Multi-Round Harvesting

    [Test]
    public async Task Harvest_completes_after_required_rounds()
    {
        // Given a node requiring 3 rounds and a character with a pick
        Guid nodeId = await RegisterNode(CopperOreTag);
        TestCharacter character = CreateCharacter(JobSystemItemType.ToolPick);
        _characterRepository.Add(character);
        PerformInteractionCommand command = new(character.GetId(), "harvesting", nodeId);

        // When harvesting for 3 rounds
        CommandResult r1 = await _commandHandler.HandleAsync(command);
        r1.Data!["status"].Should().Be("InProgress");

        CommandResult r2 = await _commandHandler.HandleAsync(command);
        r2.Data!["status"].Should().Be("InProgress");

        CommandResult r3 = await _commandHandler.HandleAsync(command);

        // Then the harvest should complete
        r3.Data!["status"].Should().Be("Completed");
    }

    [Test]
    public async Task Completed_harvest_publishes_resource_harvested_event()
    {
        // Given a node we'll harvest to completion
        Guid nodeId = await RegisterNode(CopperOreTag);
        TestCharacter character = CreateCharacter(JobSystemItemType.ToolPick);
        _characterRepository.Add(character);
        PerformInteractionCommand command = new(character.GetId(), "harvesting", nodeId);

        // When harvesting to completion
        _publishedEvents.Clear();
        for (int i = 0; i < 3; i++)
            await _commandHandler.HandleAsync(command);

        // Then a ResourceHarvestedEvent should be published
        _publishedEvents.OfType<ResourceHarvestedEvent>().Should().HaveCount(1);
        ResourceHarvestedEvent harvestEvent = _publishedEvents.OfType<ResourceHarvestedEvent>().First();
        harvestEvent.ResourceTag.Should().Be(CopperOreTag);
        harvestEvent.Items.Should().HaveCountGreaterThan(0);
    }

    #endregion

    #region Knowledge Modifiers

    [Test]
    public async Task Knowledge_rate_modifier_reduces_harvest_rounds()
    {
        // Given a 3-round node and knowledge granting +1 progress per tick
        Guid nodeId = await RegisterNode(CopperOreTag);
        Knowledge rateKnowledge = CreateKnowledge("fast_mining",
            new KnowledgeHarvestEffect(CopperOreTag, HarvestStep.HarvestStepRate, 1, EffectOperation.Additive));
        TestCharacter character = CreateCharacterWithKnowledge(
            JobSystemItemType.ToolPick, rateKnowledge);
        _characterRepository.Add(character);

        PerformInteractionCommand command = new(character.GetId(), "harvesting", nodeId);

        // When performing 2 ticks (each adds 1+1=2 progress, so 2 ticks = 4 progress >= 3)
        CommandResult r1 = await _commandHandler.HandleAsync(command);
        r1.Data!["status"].Should().Be("InProgress");

        _publishedEvents.Clear();
        CommandResult r2 = await _commandHandler.HandleAsync(command);

        // Then it should complete in just 2 rounds
        r2.Data!["status"].Should().Be("Completed");
        _publishedEvents.OfType<ResourceHarvestedEvent>().Should().HaveCount(1);
    }

    [Test]
    public async Task Knowledge_yield_modifier_increases_output_quantity()
    {
        // Given knowledge granting +2 yield on copper ore
        Guid nodeId = await RegisterNode(CopperOreTag);
        Knowledge yieldKnowledge = CreateKnowledge("abundant_mining",
            new KnowledgeHarvestEffect(CopperOreTag, HarvestStep.ItemYield, 2, EffectOperation.Additive));
        TestCharacter character = CreateCharacterWithKnowledge(
            JobSystemItemType.ToolPick, yieldKnowledge);
        _characterRepository.Add(character);

        PerformInteractionCommand command = new(character.GetId(), "harvesting", nodeId);

        // When harvesting to completion
        _publishedEvents.Clear();
        for (int i = 0; i < 3; i++)
            await _commandHandler.HandleAsync(command);

        // Then the yield should be 1 (base) + 2 (bonus) = 3
        ResourceHarvestedEvent harvestEvent = _publishedEvents.OfType<ResourceHarvestedEvent>().First();
        harvestEvent.Items.First().Quantity.Should().Be(3);
    }

    #endregion

    #region Session Lifecycle

    [Test]
    public async Task Session_cleared_after_harvest_completion()
    {
        // Given a completed harvest
        Guid nodeId = await RegisterNode(CopperOreTag);
        TestCharacter character = CreateCharacter(JobSystemItemType.ToolPick);
        _characterRepository.Add(character);

        for (int i = 0; i < 3; i++)
            await _commandHandler.HandleAsync(
                new PerformInteractionCommand(character.GetId(), "harvesting", nodeId));

        // Then no active session should remain
        _sessionManager.HasActiveSession(character.GetId()).Should().BeFalse();
    }

    [Test]
    public async Task Interaction_framework_events_published_alongside_harvest_events()
    {
        // Given a single-round harvest (BaseHarvestRounds = 0)
        ((InMemoryResourceNodeDefinitionRepository)_definitionRepository).Create(
            new ResourceNodeDefinition(2, ResourceType.Ore, "quick_ore",
                new HarvestContext(JobSystemItemType.ToolPick),
                [new HarvestOutput("quick_item", 1)],
                Uses: 50, BaseHarvestRounds: 0));
        Guid nodeId = await RegisterNode("quick_ore");
        TestCharacter character = CreateCharacter(JobSystemItemType.ToolPick);
        _characterRepository.Add(character);

        _publishedEvents.Clear();

        // When harvesting (completes immediately since BaseHarvestRounds = 0)
        await _commandHandler.HandleAsync(
            new PerformInteractionCommand(character.GetId(), "harvesting", nodeId));

        // Then both framework and domain events should be published
        _publishedEvents.OfType<InteractionStartedEvent>().Should().HaveCount(1);
        _publishedEvents.OfType<InteractionCompletedEvent>().Should().HaveCount(1);
        _publishedEvents.OfType<ResourceHarvestedEvent>().Should().HaveCount(1);
    }

    #endregion

    #region Helpers

    private async Task<Guid> RegisterNode(string definitionTag)
    {
        CommandResult result = await _registerNodeHandler.HandleAsync(
            new RegisterNodeCommand(null, definitionTag, TestArea, 1, 1, 0, 0, IPQuality.Average, 50));
        return (Guid)result.Data!["nodeInstanceId"];
    }

    private TestCharacter CreateCharacter(JobSystemItemType toolType)
    {
        CharacterId id = CharacterId.New();
        Dictionary<EquipmentSlots, ItemSnapshot> equipment = toolType == JobSystemItemType.None
            ? []
            : new Dictionary<EquipmentSlots, ItemSnapshot>
            {
                [EquipmentSlots.RightHand] = new ItemSnapshot(
                    "test_tool", "Test Tool", "A tool",
                    IPQuality.Average, [MaterialEnum.Iron], toolType, 0, null)
            };

        return new TestCharacter(equipment, [], id,
            _knowledgeRepository, null!);
    }

    private TestCharacter CreateCharacterWithKnowledge(
        JobSystemItemType toolType, params Knowledge[] knowledgeArticles)
    {
        CharacterId id = CharacterId.New();

        foreach (Knowledge k in knowledgeArticles)
        {
            _knowledgeRepository.Add(new CharacterKnowledge
            {
                Id = Guid.NewGuid(),
                IndustryTag = "mining",
                Definition = k,
                CharacterId = id.Value
            });
        }

        return new TestCharacter(
            new Dictionary<EquipmentSlots, ItemSnapshot>
            {
                [EquipmentSlots.RightHand] = new ItemSnapshot(
                    "test_tool", "Test Tool", "A tool",
                    IPQuality.Average, [MaterialEnum.Iron], toolType, 0, null)
            },
            [], id, _knowledgeRepository, null!);
    }

    private static Knowledge CreateKnowledge(string tag, params KnowledgeHarvestEffect[] effects)
    {
        return new Knowledge
        {
            Tag = tag,
            Name = $"Test Knowledge: {tag}",
            Description = "Test knowledge for harvest testing",
            Level = ProficiencyLevel.Novice,
            HarvestEffects = [..effects]
        };
    }

    private class TestEventBus(List<IDomainEvent> events) : IEventBus
    {
        public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
            where TEvent : IDomainEvent
        {
            events.Add(@event);
            return Task.CompletedTask;
        }

        public void Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler)
            where TEvent : IDomainEvent { }
    }

    #endregion
}
