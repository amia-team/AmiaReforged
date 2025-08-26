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
    private HarvestingService _sut = null!;

    [SetUp]
    public void OneTimeSetUp()
    {
        _sut = new HarvestingService(CreateTestRepository());
    }

    [Test]
    public void Should_Harvest_Resource_Node()
    {
        ICharacter pc = CreateTestCharacter();
        ItemDefinition item = new ItemDefinition("test_itm", "test_item", "Test Item", "Testy McTest", [],
            JobSystemItemType.None, 0);
        ResourceNodeDefinition definition = new("test",
            [new HarvestContext(JobSystemItemType.None, Material.None)],
            [new HarvestOutput("test_item", 1)]);
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
            Quantity = 10
        };

        _sut.RegisterNode(instance);
        instance.Harvest(pc);

        Assert.That(pc.GetInventory().Any(i => i.Tag == item.ItemTag), Is.True);
    }

    private ICharacter CreateTestCharacter(Dictionary<EquipmentSlots, ItemSnapshot>? injectedEquipment = null,
        List<SkillData>? skills = null, List<ItemSnapshot>? inventory = null)
    {
        return new TestCharacter(injectedEquipment ?? new Dictionary<EquipmentSlots, ItemSnapshot>(), skills ?? [], Guid.NewGuid(),
            inventory: inventory);
    }

    private IResourceNodeInstanceRepository CreateTestRepository()
    {
        return new TestResourceNodeInstanceRepository();
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

    public List<ResourceNodeInstance> GetInstances()
    {
        return _resourceNodeInstances;
    }

    public List<ResourceNodeInstance> GetInstancesByArea(string resRef)
    {
        return _resourceNodeInstances.Where(r => r.Area == resRef).ToList();
    }
}
