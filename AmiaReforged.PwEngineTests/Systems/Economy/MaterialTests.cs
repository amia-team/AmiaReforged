using AmiaReforged.PwEngine.Systems.Economy.Entities;
using AmiaReforged.PwEngine.Systems.JobSystem.Entities;

namespace AmiaReforged.PwEngineTests.Systems.Economy;

[TestFixture]
public class MaterialTests
{
    
    [Test]
    public void Should_Define_Enum_Mapping()
    {
        var material = Material.FromType(MaterialEnum.Adamantine);
        
        Assert.That(material.Type, Is.EqualTo(MaterialEnum.Adamantine));
    }

    [Test]
    public void Should_Define_Cost_Modifier()
    {
        var material = Material.FromType(MaterialEnum.Adamantine);
        
        Assert.That(material.CostModifier, Is.EqualTo(Material.AdamantineCostModifier));
    }
}