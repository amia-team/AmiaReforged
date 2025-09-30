using AmiaReforged.PwEngine.Systems.WorldEngine;
using AmiaReforged.PwEngine.Systems.WorldEngine.Characters;
using AmiaReforged.PwEngine.Systems.WorldEngine.Harvesting;
using AmiaReforged.PwEngine.Systems.WorldEngine.Industries;
using AmiaReforged.PwEngine.Systems.WorldEngine.Items;
using AmiaReforged.PwEngine.Systems.WorldEngine.Items.ItemData;
using AmiaReforged.PwEngine.Systems.WorldEngine.KnowledgeSubsystem;
using AmiaReforged.PwEngine.Systems.WorldEngine.ResourceNodes;
using AmiaReforged.PwEngine.Systems.WorldEngine.ResourceNodes.ResourceNodeData;
using AmiaReforged.PwEngine.Tests.Systems.WorldEngine.Helpers;
using Anvil.API;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.Harvesting;

[TestFixture]
public class HarvestTests
{
    private const string TestItemTag = "test_item";
    private const string HarvestTime = "mod_time";
    private const string HarvestQuality = "mod_quality";
    private const string HarvestYield = "mod_yield";
    private HarvestingService _sut = null!;
    private IIndustryMembershipService _membershipService = null!;
    private ICharacterKnowledgeRepository _characterKnowledgeRepository = null!;
    private IIndustryRepository _industries = null!;
    private IIndustryMembershipRepository _memberships = null!;
    private readonly RuntimeCharacterRepository _characters = new();

    [SetUp]
    public void OneTimeSetUp()
    {
        IItemDefinitionRepository itemDefinitionRepository = CreateItemDefinitionRepository();
        ItemDefinition item = new("test_itm", TestItemTag, "Test Item", "Testy McTest", [],
            JobSystemItemType.None, 0, new AppearanceData(0, null, null));
        itemDefinitionRepository.AddItemDefinition(item);
        _sut = new HarvestingService(CreateTestRepository(), itemDefinitionRepository);

        // IMPORTANT: Initialize the knowledge repository BEFORE creating the membership service
        _characterKnowledgeRepository = InMemoryCharacterKnowledgeRepository.Create();

        _industries = InMemoryIndustryRepository.Create();
        _memberships = InMemoryIndustryMembershipRepository.Create();

        _membershipService = new IndustryMembershipService(
            _memberships,
            _industries,
            _characters,
            _characterKnowledgeRepository);

        Industry i = new()
        {
            Tag = "new",
            Name = "industry",
            Knowledge =
            [
                new Knowledge
                {
                    Tag = HarvestYield,
                    Name = "yield",
                    Description = "more items",
                    Level = ProficiencyLevel.Novice,
                    HarvestEffects =
                    [
                        new KnowledgeHarvestEffect("test", HarvestStep.ItemYield, 1.0f, EffectOperation.Additive)
                    ],
                    PointCost = 0
                },
                new Knowledge()
                {
                    Tag = HarvestQuality,
                    Name = "quality",
                    Description = "improves quality",
                    Level = ProficiencyLevel.Novice,
                    HarvestEffects =
                    [
                        new KnowledgeHarvestEffect("test", HarvestStep.Quality, 1.0f, EffectOperation.Additive)
                    ],
                    PointCost = 0
                },
                new Knowledge()
                {
                    Tag = HarvestTime,
                    Name = "time",
                    Description = "reduces time to collection",
                    Level = ProficiencyLevel.Novice,
                    HarvestEffects =
                    [
                        new KnowledgeHarvestEffect("test", HarvestStep.HarvestStepRate, 1.0f, EffectOperation.Additive)
                    ],
                    PointCost = 0
                },
            ],
        };

        _industries.Add(i);
    }


    [Test]
    public void Should_Harvest_Resource_Node()
    {
        ICharacter pc = CreateTestCharacter();
        _characters.Add(pc);
        pc.GetEquipment().Add(EquipmentSlots.RightHand,
            new ItemSnapshot("fake_tool", "Test Item", "Test", IPQuality.Average, [MaterialEnum.Iron],
                JobSystemItemType.ToolPick, 0, null));

        ResourceNodeDefinition definition = new(0, ResourceType.Undefined, "test",
            new HarvestContext(JobSystemItemType.None),
            [new HarvestOutput(TestItemTag, 1)]);

        ResourceNodeInstance instance = new()
        {
            Area = "test_area",
            Definition = definition,
            X = 1.0f,
            Y = 1.0f,
            Z = 1.0f,
            Rotation = 1.0f,
            Quality = IPQuality.BelowAverage,
            Uses = 10
        };

        _sut.RegisterNode(instance);
        HarvestResult result = instance.Harvest(pc);

        Assert.That(result, Is.EqualTo(HarvestResult.Finished));
        Assert.That(pc.GetInventory().Any(i => i.Tag == TestItemTag), Is.True);
    }

    [Test]
    public void Fails_Harvest_Without_Correct_Tool()
    {
        ICharacter pc = CreateTestCharacter();
        _characters.Add(pc);

        // Equip a tool that does not match the node's required tool type
        pc.GetEquipment().Add(EquipmentSlots.RightHand,
            new ItemSnapshot("fake_tool", "Test Item", "Test", IPQuality.Average, [MaterialEnum.Iron],
                JobSystemItemType.ToolHammer, 0, null));

        // Node requires a pick, but the character holds a hammer
        ResourceNodeDefinition definition = new(0, ResourceType.Ore, "test",
            new HarvestContext(JobSystemItemType.ToolPick),
            [new HarvestOutput(TestItemTag, 1)]);

        ResourceNodeInstance instance = new()
        {
            Area = "test_area",
            Definition = definition,
            X = 1.0f,
            Y = 1.0f,
            Z = 1.0f,
            Rotation = 1.0f,
            Quality = IPQuality.BelowAverage,
            Uses = 10
        };

        _sut.RegisterNode(instance);
        HarvestResult result = instance.Harvest(pc);

        Assert.That(result, Is.EqualTo(HarvestResult.NoTool));
        Assert.That(pc.GetInventory().Any(i => i.Tag == TestItemTag), Is.False);
    }

    [Test]
    public void Some_Nodes_Should_Take_Multiple_Attempts()
    {
        ICharacter pc = CreateTestCharacter();
        _characters.Add(pc);

        pc.GetEquipment().Add(EquipmentSlots.RightHand,
            new ItemSnapshot("fake_tool", "Test Item", "Test", IPQuality.Average, [MaterialEnum.Iron],
                JobSystemItemType.ToolPick, 0, null));

        ResourceNodeDefinition definition = new(0, ResourceType.Ore, "test",
            new HarvestContext(JobSystemItemType.ToolPick),
            [new HarvestOutput(TestItemTag, 1)], 10, 2);

        ResourceNodeInstance instance = new()
        {
            Area = "test_area",
            Definition = definition,
            X = 1.0f,
            Y = 1.0f,
            Z = 1.0f,
            Rotation = 1.0f,
            Quality = IPQuality.BelowAverage,
            Uses = 10
        };

        _sut.RegisterNode(instance);
        HarvestResult result = instance.Harvest(pc);

        Assert.That(result, Is.EqualTo(HarvestResult.InProgress));

        result = instance.Harvest(pc);

        Assert.That(result, Is.EqualTo(HarvestResult.Finished));
    }

    [Test]
    public void Knowledge_Can_Modify_Yield()
    {
        ICharacter pc = CreateTestCharacter();
        _characters.Add(pc);

        pc.GetEquipment().Add(EquipmentSlots.RightHand,
            new ItemSnapshot("fake_tool", "Test Item", "Test", IPQuality.Average, [MaterialEnum.Iron],
                JobSystemItemType.ToolPick, 0, null));

        pc.JoinIndustry("new");

        pc.Learn(HarvestYield);

        ResourceNodeDefinition definition = new(0, ResourceType.Ore, "test",
            new HarvestContext(JobSystemItemType.ToolPick),
            [new HarvestOutput(TestItemTag, 1)], 10, 2);

        ResourceNodeInstance instance = new()
        {
            Area = "test_area",
            Definition = definition,
            X = 1.0f,
            Y = 1.0f,
            Z = 1.0f,
            Rotation = 1.0f,
            Quality = IPQuality.BelowAverage,
            Uses = 10
        };

        _sut.RegisterNode(instance);

        HarvestResult result = instance.Harvest(pc);

        Assert.That(result, Is.EqualTo(HarvestResult.InProgress));

        result = instance.Harvest(pc);

        Assert.That(result, Is.EqualTo(HarvestResult.Finished));
        Assert.That(pc.GetInventory().Count(i => i.Tag == TestItemTag), Is.EqualTo(2));
    }

    [Test]
    public void Knowledge_Can_Modify_Quality()
    {
        ICharacter pc = CreateTestCharacter();
        _characters.Add(pc);

        pc.GetEquipment().Add(EquipmentSlots.RightHand,
            new ItemSnapshot("fake_tool", "Test Item", "Test", IPQuality.Average, [MaterialEnum.Iron],
                JobSystemItemType.ToolPick, 0, null));

        pc.JoinIndustry("new");

        pc.Learn(HarvestQuality);

        ResourceNodeDefinition definition = new(0, ResourceType.Ore, "test",
            new HarvestContext(JobSystemItemType.ToolPick),
            [new HarvestOutput(TestItemTag, 1)], 1);

        ResourceNodeInstance instance = new()
        {
            Area = "test_area",
            Definition = definition,
            X = 1.0f,
            Y = 1.0f,
            Z = 1.0f,
            Rotation = 1.0f,
            Quality = IPQuality.Average,
            Uses = 10
        };

        _sut.RegisterNode(instance);

        HarvestResult result = instance.Harvest(pc);

        Assert.That(result, Is.EqualTo(HarvestResult.Finished));

        ItemSnapshot? snapshot = pc.GetInventory().FirstOrDefault(i => i.Tag == TestItemTag);
        Assert.That(snapshot, Is.Not.Null);
        Assert.That(snapshot!.Quality, Is.EqualTo(IPQuality.AboveAverage));
    }

    [Test]
    public void Knowledge_Can_Modify_Time_To_Harvest()
    {
        ICharacter pc = CreateTestCharacter();
        _characters.Add(pc);

        pc.GetEquipment().Add(EquipmentSlots.RightHand,
            new ItemSnapshot("fake_tool", "Test Item", "Test", IPQuality.Average, [MaterialEnum.Iron],
                JobSystemItemType.ToolPick, 0, null));

        pc.JoinIndustry("new");

        pc.Learn(HarvestTime);

        ResourceNodeDefinition definition = new(0, ResourceType.Ore, "test",
            new HarvestContext(JobSystemItemType.ToolPick),
            [new HarvestOutput(TestItemTag, 1)], 2);

        ResourceNodeInstance instance = new()
        {
            Area = "test_area",
            Definition = definition,
            X = 1.0f,
            Y = 1.0f,
            Z = 1.0f,
            Rotation = 1.0f,
            Quality = IPQuality.Average,
            Uses = 10
        };

        _sut.RegisterNode(instance);

        HarvestResult result = instance.Harvest(pc);

        Assert.That(result, Is.EqualTo(HarvestResult.Finished));
    }


    private ICharacter CreateTestCharacter(Dictionary<EquipmentSlots, ItemSnapshot>? injectedEquipment = null,
        List<SkillData>? skills = null, List<ItemSnapshot>? inventory = null)
    {
        return new TestCharacter(injectedEquipment ?? new Dictionary<EquipmentSlots, ItemSnapshot>(), skills ?? [],
            Guid.NewGuid(), _characterKnowledgeRepository, _membershipService,
            inventory: inventory);
    }

    private IResourceNodeInstanceRepository CreateTestRepository()
    {
        return new InMemoryResourceNodeInstanceRepository();
    }

    private IItemDefinitionRepository CreateItemDefinitionRepository()
    {
        return new InMemoryItemDefinitionRepository();
    }
}
