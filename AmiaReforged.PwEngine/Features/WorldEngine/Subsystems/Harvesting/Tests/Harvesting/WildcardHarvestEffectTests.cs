using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Tests.Helpers;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.CharacterData;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Characters.Runtime;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting.Application;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting.Commands;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting.Events;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Industries.KnowledgeSubsystem;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Items.ItemData;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes.ResourceNodeData;
using Anvil.API;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Harvesting.Tests.Harvesting;

/// <summary>
/// BDD-style tests verifying that wildcard <see cref="NodeTagPattern"/> values in
/// <see cref="KnowledgeHarvestEffect"/> correctly modify harvest outputs through the
/// full CQRS pipeline.
/// </summary>
[TestFixture]
public class WildcardHarvestEffectTests
{
    private IResourceNodeInstanceRepository _nodeRepository = null!;
    private IResourceNodeDefinitionRepository _definitionRepository = null!;
    private ICharacterRepository _characterRepository = null!;
    private ICharacterKnowledgeRepository _knowledgeRepository = null!;
    private IEventBus _eventBus = null!;

    private RegisterNodeCommandHandler _registerNodeHandler = null!;
    private HarvestResourceCommandHandler _harvestHandler = null!;

    private List<IDomainEvent> _publishedEvents = null!;

    private const string TestArea = "test_area";

    // Two ore definitions with related tags
    private const string CopperOreTag = "ore_vein_copper_native";
    private const string HematiteOreTag = "ore_vein_hematite";
    private const string CopperItemTag = "copper_ore_native";
    private const string HematiteItemTag = "hematite_ore";

    // A tree definition (different resource type)
    private const string TreeOakTag = "tree_oak";
    private const string TreeItemTag = "oak_wood";

    [SetUp]
    public void SetUp()
    {
        _nodeRepository = new InMemoryResourceNodeInstanceRepository();
        _definitionRepository = new InMemoryResourceNodeDefinitionRepository();
        _characterRepository = new RuntimeCharacterRepository();
        _knowledgeRepository = InMemoryCharacterKnowledgeRepository.Create();
        _publishedEvents = new List<IDomainEvent>();
        _eventBus = new TestEventBus(_publishedEvents);

        _registerNodeHandler = new RegisterNodeCommandHandler(_nodeRepository, _definitionRepository, _eventBus);
        _harvestHandler = new HarvestResourceCommandHandler(_nodeRepository, _characterRepository, _eventBus);

        // --- Resource definitions ---
        ((InMemoryResourceNodeDefinitionRepository)_definitionRepository).Create(
            new ResourceNodeDefinition(1, ResourceType.Ore, CopperOreTag,
                new HarvestContext(ItemForm.ToolPick),
                [new HarvestOutput(CopperItemTag, 1)],
                Uses: 50, BaseHarvestRounds: 0));

        ((InMemoryResourceNodeDefinitionRepository)_definitionRepository).Create(
            new ResourceNodeDefinition(2, ResourceType.Ore, HematiteOreTag,
                new HarvestContext(ItemForm.ToolPick),
                [new HarvestOutput(HematiteItemTag, 1)],
                Uses: 50, BaseHarvestRounds: 0));

        ((InMemoryResourceNodeDefinitionRepository)_definitionRepository).Create(
            new ResourceNodeDefinition(3, ResourceType.Tree, TreeOakTag,
                new HarvestContext(ItemForm.ToolAxe),
                [new HarvestOutput(TreeItemTag, 1)],
                Uses: 50, BaseHarvestRounds: 0));
    }

    #region Exact Match (backward compatibility)

    [Test]
    public async Task ExactMatch_OnlyAppliesToSpecificNode()
    {
        // Given knowledge with an exact-tag harvest effect for copper ore
        Knowledge oreKnowledge = CreateKnowledge("exact_copper",
            new KnowledgeHarvestEffect(CopperOreTag, HarvestStep.ItemYield, 2, EffectOperation.Additive));

        TestCharacter character = CreateCharacterWithKnowledge(ItemForm.ToolPick, oreKnowledge);
        _characterRepository.Add(character);

        // When harvesting copper ore
        HarvestedItem[] copperItems = await HarvestNode(CopperOreTag, character);

        // Then the yield should be increased by 2 (1 base + 2 bonus = 3)
        Assert.That(copperItems, Has.Length.EqualTo(1));
        Assert.That(copperItems[0].Quantity, Is.EqualTo(3));

        // When harvesting hematite ore (same tool type, different tag)
        HarvestedItem[] hematiteItems = await HarvestNode(HematiteOreTag, character);

        // Then the hematite yield should be unmodified (base = 1)
        Assert.That(hematiteItems, Has.Length.EqualTo(1));
        Assert.That(hematiteItems[0].Quantity, Is.EqualTo(1));
    }

    #endregion

    #region Glob Wildcard Matching

    [Test]
    public async Task GlobWildcard_OreVeinStar_MatchesAllOreVeins()
    {
        // Given knowledge with a wildcard harvest effect for all ore veins
        Knowledge oreKnowledge = CreateKnowledge("all_ore_veins",
            new KnowledgeHarvestEffect("ore_vein_*", HarvestStep.ItemYield, 1, EffectOperation.Additive));

        TestCharacter character = CreateCharacterWithKnowledge(ItemForm.ToolPick, oreKnowledge);
        _characterRepository.Add(character);

        // When harvesting copper ore
        HarvestedItem[] copperItems = await HarvestNode(CopperOreTag, character);

        // Then yield is increased (1 base + 1 bonus = 2)
        Assert.That(copperItems[0].Quantity, Is.EqualTo(2));

        // When harvesting hematite ore
        HarvestedItem[] hematiteItems = await HarvestNode(HematiteOreTag, character);

        // Then yield is also increased
        Assert.That(hematiteItems[0].Quantity, Is.EqualTo(2));
    }

    [Test]
    public async Task GlobWildcard_DoesNotMatchDifferentPrefix()
    {
        // Given knowledge with a wildcard for ore veins
        Knowledge oreKnowledge = CreateKnowledge("ore_wildcard",
            new KnowledgeHarvestEffect("ore_vein_*", HarvestStep.ItemYield, 5, EffectOperation.Additive));

        // Character needs an axe for trees
        TestCharacter character = CreateCharacterWithKnowledge(ItemForm.ToolAxe, oreKnowledge);
        _characterRepository.Add(character);

        // When harvesting a tree
        HarvestedItem[] treeItems = await HarvestNode(TreeOakTag, character);

        // Then tree yield is unaffected (base = 1)
        Assert.That(treeItems[0].Quantity, Is.EqualTo(1));
    }

    [Test]
    public async Task GlobWildcard_MiddleStar_MatchesPartialSegments()
    {
        // Given knowledge with a wildcard in the middle: ore_*_copper_*
        Knowledge oreKnowledge = CreateKnowledge("copper_variants",
            new KnowledgeHarvestEffect("ore_*_copper_*", HarvestStep.ItemYield, 3, EffectOperation.Additive));

        TestCharacter character = CreateCharacterWithKnowledge(ItemForm.ToolPick, oreKnowledge);
        _characterRepository.Add(character);

        // When harvesting copper ore (tag: ore_vein_copper_native — matches ore_*_copper_*)
        HarvestedItem[] copperItems = await HarvestNode(CopperOreTag, character);

        // Then yield is increased (1 base + 3 bonus = 4)
        Assert.That(copperItems[0].Quantity, Is.EqualTo(4));

        // When harvesting hematite (ore_vein_hematite — does NOT match ore_*_copper_*)
        HarvestedItem[] hematiteItems = await HarvestNode(HematiteOreTag, character);

        // Then yield is unaffected
        Assert.That(hematiteItems[0].Quantity, Is.EqualTo(1));
    }

    #endregion

    #region Type Pattern Matching

    [Test]
    public async Task TypePattern_MatchesAllNodesOfThatType()
    {
        // Given knowledge with a type-based pattern for all ores
        Knowledge oreKnowledge = CreateKnowledge("all_ores_type",
            new KnowledgeHarvestEffect("type:ore", HarvestStep.ItemYield, 1, EffectOperation.Additive));

        TestCharacter character = CreateCharacterWithKnowledge(ItemForm.ToolPick, oreKnowledge);
        _characterRepository.Add(character);

        // When harvesting copper ore
        HarvestedItem[] copperItems = await HarvestNode(CopperOreTag, character);

        // Then yield is increased
        Assert.That(copperItems[0].Quantity, Is.EqualTo(2));

        // When harvesting hematite ore
        HarvestedItem[] hematiteItems = await HarvestNode(HematiteOreTag, character);

        // Then yield is also increased
        Assert.That(hematiteItems[0].Quantity, Is.EqualTo(2));
    }

    [Test]
    public async Task TypePattern_DoesNotMatchDifferentResourceType()
    {
        // Given knowledge affecting only ore types
        Knowledge oreKnowledge = CreateKnowledge("ore_type_only",
            new KnowledgeHarvestEffect("type:ore", HarvestStep.ItemYield, 5, EffectOperation.Additive));

        TestCharacter character = CreateCharacterWithKnowledge(ItemForm.ToolAxe, oreKnowledge);
        _characterRepository.Add(character);

        // When harvesting a tree
        HarvestedItem[] treeItems = await HarvestNode(TreeOakTag, character);

        // Then tree yield is unaffected
        Assert.That(treeItems[0].Quantity, Is.EqualTo(1));
    }

    #endregion

    #region Combined / Stacking

    [Test]
    public async Task MultipleWildcardEffects_Stack()
    {
        // Given knowledge with BOTH a type-wide effect and a specific wildcard
        Knowledge broadKnowledge = CreateKnowledge("broad_ore",
            new KnowledgeHarvestEffect("type:ore", HarvestStep.ItemYield, 1, EffectOperation.Additive));

        Knowledge specificKnowledge = CreateKnowledge("specific_copper",
            new KnowledgeHarvestEffect("ore_vein_copper_*", HarvestStep.ItemYield, 2, EffectOperation.Additive));

        TestCharacter character = CreateCharacterWithKnowledge(ItemForm.ToolPick, broadKnowledge, specificKnowledge);
        _characterRepository.Add(character);

        // When harvesting copper (matches both patterns)
        HarvestedItem[] copperItems = await HarvestNode(CopperOreTag, character);

        // Then yield is increased by both effects (1 base + 1 broad + 2 specific = 4)
        Assert.That(copperItems[0].Quantity, Is.EqualTo(4));

        // When harvesting hematite (matches only the type pattern)
        HarvestedItem[] hematiteItems = await HarvestNode(HematiteOreTag, character);

        // Then yield is increased by only the type effect (1 base + 1 broad = 2)
        Assert.That(hematiteItems[0].Quantity, Is.EqualTo(2));
    }

    #endregion

    #region HarvestStepRate with Wildcards

    [Test]
    public async Task GlobWildcard_HarvestStepRate_SpeedsUpAllMatchingNodes()
    {
        // Given - a slow ore that takes 3 rounds to harvest
        const string slowOreTag = "ore_vein_slow_test";
        const string slowOreItemTag = "slow_ore_item";

        ((InMemoryResourceNodeDefinitionRepository)_definitionRepository).Create(
            new ResourceNodeDefinition(10, ResourceType.Ore, slowOreTag,
                new HarvestContext(ItemForm.ToolPick),
                [new HarvestOutput(slowOreItemTag, 1)],
                Uses: 50, BaseHarvestRounds: 3));

        // And knowledge that speeds up all ore veins by +2 steps per round
        Knowledge speedKnowledge = CreateKnowledge("fast_miner",
            new KnowledgeHarvestEffect("ore_vein_*", HarvestStep.HarvestStepRate, 2, EffectOperation.Additive));

        TestCharacter character = CreateCharacterWithKnowledge(ItemForm.ToolPick, speedKnowledge);
        _characterRepository.Add(character);

        // Register the slow ore node
        CommandResult registerResult = await _registerNodeHandler.HandleAsync(
            new RegisterNodeCommand(null, slowOreTag, TestArea, 1, 1, 0, 0, IPQuality.Average, 5),
            CancellationToken.None);
        Guid nodeId = (Guid)registerResult.Data!["nodeInstanceId"];

        _publishedEvents.Clear();

        // When - harvesting in a single round (1 base progress + 2 bonus = 3, which meets BaseHarvestRounds=3)
        HarvestResourceCommand command = new(character.GetId().Value, nodeId);
        CommandResult result = await _harvestHandler.HandleAsync(command, CancellationToken.None);

        // Then - harvest should complete in a single round instead of 3
        Assert.That(result.Success, Is.True);
        Assert.That(result.Data!["status"], Is.EqualTo("Completed"));
    }

    #endregion

    #region Helper Methods

    private Knowledge CreateKnowledge(string tag, params KnowledgeHarvestEffect[] effects)
    {
        return new Knowledge
        {
            Tag = tag,
            Name = tag,
            Description = $"Test knowledge: {tag}",
            Level = ProficiencyLevel.Novice,
            PointCost = 0,
            HarvestEffects = effects.ToList()
        };
    }

    private TestCharacter CreateCharacterWithKnowledge(ItemForm toolType, params Knowledge[] knowledgeArticles)
    {
        CharacterId characterId = CharacterId.From(Guid.NewGuid());

        // Inject knowledge directly into the repository
        foreach (Knowledge k in knowledgeArticles)
        {
            _knowledgeRepository.Add(new CharacterKnowledge
            {
                Id = Guid.NewGuid(),
                IndustryTag = "test_industry",
                Definition = k,
                CharacterId = characterId.Value
            });
        }

        return new TestCharacter(
            new Dictionary<EquipmentSlots, ItemSnapshot>
            {
                [EquipmentSlots.RightHand] = new ItemSnapshot(
                    "test_tool", "Test Tool", "A tool for testing",
                    IPQuality.Average, [MaterialEnum.Iron], toolType, 0, null)
            },
            new List<SkillData>(),
            characterId,
            _knowledgeRepository,
            null!, // No membership service needed
            null);
    }

    private async Task<HarvestedItem[]> HarvestNode(string definitionTag, TestCharacter character)
    {
        // Register a fresh node
        CommandResult registerResult = await _registerNodeHandler.HandleAsync(
            new RegisterNodeCommand(null, definitionTag, TestArea, 1, 1, 0, 0, IPQuality.Average, 5),
            CancellationToken.None);
        Guid nodeId = (Guid)registerResult.Data!["nodeInstanceId"];

        _publishedEvents.Clear();

        // Harvest it
        HarvestResourceCommand command = new(character.GetId().Value, nodeId);
        await _harvestHandler.HandleAsync(command, CancellationToken.None);

        // Return the harvested items from the event
        ResourceHarvestedEvent? harvestEvent = _publishedEvents.OfType<ResourceHarvestedEvent>().FirstOrDefault();
        return harvestEvent?.Items ?? [];
    }

    private class TestEventBus : IEventBus
    {
        private readonly List<IDomainEvent> _events;
        public TestEventBus(List<IDomainEvent> events) => _events = events;

        public Task PublishAsync<TEvent>(TEvent @event, CancellationToken cancellationToken = default)
            where TEvent : IDomainEvent
        {
            _events.Add(@event);
            return Task.CompletedTask;
        }

        public void Subscribe<TEvent>(Func<TEvent, CancellationToken, Task> handler) where TEvent : IDomainEvent { }
    }

    #endregion
}
