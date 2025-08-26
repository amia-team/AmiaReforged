using AmiaReforged.PwEngine.Systems.WorldEngine.Harvesting;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.Harvesting;

[TestFixture]
public class ToolHarvestModifierTests
{
    private IHarvestingModifier _sut = null!;

    [SetUp]
    public void SetUp()
    {
        _sut = new ToolHarvestModifier();
    }

}
