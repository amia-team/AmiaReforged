using AmiaReforged.PwEngine.Systems.Economy.Entities;
using AmiaReforged.PwEngine.Systems.JobSystem.Entities;
using Anvil.API;

namespace AmiaReforged.PwEngineTests.Systems.Economy;

[TestFixture]
public class PersistentResourceTests
{
    // Tests that seem trivial at first, but ensure that this domain is properly defined.
    [Test]
    public void Should_Define_Quantity()
    {
        var resource = new PersistentResource
        {
            Quantity = 100,
            ItemType = BaseItemType.MiscSmall,
            Materials =
            [
                Material.FromType(MaterialEnum.Adamantine)
            ]
        };
        Assert.That(resource.Quantity, Is.EqualTo(100));
    }
}