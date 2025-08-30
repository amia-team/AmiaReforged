using AmiaReforged.PwEngine.Systems.WorldEngine.Harvesting;
using AmiaReforged.PwEngine.Systems.WorldEngine.Industries;
using AmiaReforged.PwEngine.Systems.WorldEngine.Items;
using AmiaReforged.PwEngine.Systems.WorldEngine.ResourceNodes;
using Anvil.API;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.Harvesting;

[TestFixture]
public class HarvestTests
{
    private const string TestItemTag = "test_item";
    private HarvestingService _sut = null!;

    [SetUp]
    public void OneTimeSetUp()
    {
        IItemDefinitionRepository itemDefinitionRepository = CreateItemDefinitionRepository();
        ItemDefinition item = new ItemDefinition("test_itm", TestItemTag, "Test Item", "Testy McTest", [],
            JobSystemItemType.None, 0);
        itemDefinitionRepository.AddItemDefinition(item);
        _sut = new HarvestingService(CreateTestRepository(), itemDefinitionRepository);
    }

    [Test]
    public void Should_Harvest_Resource_Node()
    {
        ICharacter pc = CreateTestCharacter();

        pc.GetEquipment().Add(EquipmentSlots.RightHand,
            new ItemSnapshot("fake_tool", IPQuality.Average, [Material.Iron], JobSystemItemType.ToolPick, 0, null));

        ResourceNodeDefinition definition = new("test",
            new HarvestContext(JobSystemItemType.None, Material.None),
            [new HarvestOutput(TestItemTag, 1)]);

        ResourceNodeInstance instance = new()
        {
            Area = "test_area",
            Definition = definition,
            Id = 0,
            X = 1.0f,
            Y = 1.0f,
            Z = 1.0f,
            Rotation = 1.0f,
            Quality = QualityLevel.BelowAverage,
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

        // Equip a tool that does not match the node's required tool type
        pc.GetEquipment().Add(EquipmentSlots.RightHand,
            new ItemSnapshot("fake_tool", IPQuality.Average, [Material.Iron], JobSystemItemType.ToolHammer, 0, null));

        // Node requires a pick, but the character holds a hammer
        ResourceNodeDefinition definition = new("test",
            new HarvestContext(JobSystemItemType.ToolPick, Material.None),
            [new HarvestOutput(TestItemTag, 1)]);

        ResourceNodeInstance instance = new()
        {
            Area = "test_area",
            Definition = definition,
            Id = 1,
            X = 1.0f,
            Y = 1.0f,
            Z = 1.0f,
            Rotation = 1.0f,
            Quality = QualityLevel.BelowAverage,
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
        pc.GetEquipment().Add(EquipmentSlots.RightHand,
            new ItemSnapshot("fake_tool", IPQuality.Average, [Material.Iron], JobSystemItemType.ToolPick, 0, null));

        ResourceNodeDefinition definition = new("test",
            new HarvestContext(JobSystemItemType.ToolPick, Material.None),
            [new HarvestOutput(TestItemTag, 1)], 2);

        ResourceNodeInstance instance = new()
        {
            Area = "test_area",
            Definition = definition,
            Id = 1,
            X = 1.0f,
            Y = 1.0f,
            Z = 1.0f,
            Rotation = 1.0f,
            Quality = QualityLevel.BelowAverage,
            Uses = 10
        };

        _sut.RegisterNode(instance);
        HarvestResult result = instance.Harvest(pc);

        Assert.That(result, Is.EqualTo(HarvestResult.InProgress));

        result = instance.Harvest(pc);

        Assert.That(result, Is.EqualTo(HarvestResult.Finished));
    }


    private ICharacter CreateTestCharacter(Dictionary<EquipmentSlots, ItemSnapshot>? injectedEquipment = null,
        List<SkillData>? skills = null, List<ItemSnapshot>? inventory = null)
    {
        return new TestCharacter(injectedEquipment ?? new Dictionary<EquipmentSlots, ItemSnapshot>(), skills ?? [],
            Guid.NewGuid(),
            inventory: inventory);
    }

    private IResourceNodeInstanceRepository CreateTestRepository()
    {
        return new TestResourceNodeInstanceRepository();
    }

    private IItemDefinitionRepository CreateItemDefinitionRepository()
    {
        return new InMemoryItemDefinitionRepository();
    }
}

internal class InMemoryItemDefinitionRepository : IItemDefinitionRepository
{
    private readonly Dictionary<string, ItemDefinition> _itemDefinitions = new();

    public void AddItemDefinition(ItemDefinition definition)
    {
        _itemDefinitions.TryAdd(definition.ItemTag, definition);
    }

    public ItemDefinition? GetByTag(string harvestOutputItemDefinitionTag)
    {
        return _itemDefinitions.GetValueOrDefault(harvestOutputItemDefinitionTag);
    }
}

internal class TestResourceNodeInstanceRepository : IResourceNodeInstanceRepository
{
    private readonly List<ResourceNodeInstance> _resourceNodeInstances = [];


    public void AddNodeInstance(ResourceNodeInstance instance)
    {
        _resourceNodeInstances.Add(instance);
    }

    public void RemoveNodeInstance(ResourceNodeInstance instance)
    {
        _resourceNodeInstances.Remove(instance);
    }

    public void Update(ResourceNodeInstance dataNodeInstance)
    {
        _resourceNodeInstances.Remove(dataNodeInstance);
        _resourceNodeInstances.Add(dataNodeInstance);
    }

    public List<ResourceNodeInstance> GetInstances()
    {
        return _resourceNodeInstances;
    }

    public List<ResourceNodeInstance> GetInstancesByArea(string resRef)
    {
        return _resourceNodeInstances.Where(r => r.Area == resRef).ToList();
    }
}
