using AmiaReforged.PwEngine.Features.WorldEngine.Regions;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.Regions;

[TestFixture]
public class RegionIndexTests
{
    [Test]
    public void Index_Resolves_Settlement_To_RegionTag()
    {
        InMemoryRegionRepository repo = new();
        repo.Add(new RegionDefinition { Tag = "r1", Name = "Region One", Areas = new(), Settlements = new(){1,2}});
        repo.Add(new RegionDefinition { Tag = "r2", Name = "Region Two", Areas = new(), Settlements = new(){3}});

        RegionIndex index = new(repo);
        Assert.That(index.TryGetRegionTagForSettlement(2, out string? tag), Is.True);
        Assert.That(tag, Is.EqualTo("r1"));
        Assert.That(index.TryGetRegionTagForSettlement(3, out tag), Is.True);
        Assert.That(tag, Is.EqualTo("r2"));
        Assert.That(index.TryGetRegionTagForSettlement(999, out tag), Is.False);
    }
}

