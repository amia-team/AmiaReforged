using AmiaReforged.PwEngine.Features.WorldEngine.Regions;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.Regions;

[TestFixture]
public class RegionIndexTests
{
    [Test]
    public void Index_Resolves_Settlement_To_RegionTag()
    {
        InMemoryRegionRepository repo = new();
        repo.Add(new RegionDefinition { Tag = new RegionTag("r1"), Name = "Region One", Areas = new(), Settlements = new(){SettlementId.Parse(1), SettlementId.Parse(2)}});
        repo.Add(new RegionDefinition { Tag = new RegionTag("r2"), Name = "Region Two", Areas = new(), Settlements = new(){SettlementId.Parse(3)}});

        RegionIndex index = new(repo);
        Assert.That(index.TryGetRegionTagForSettlement(SettlementId.Parse(2), out RegionTag? tag), Is.True);
        Assert.That(tag?.Value, Is.EqualTo("r1"));
        Assert.That(index.TryGetRegionTagForSettlement(SettlementId.Parse(3), out tag), Is.True);
        Assert.That(tag?.Value, Is.EqualTo("r2"));
        Assert.That(index.TryGetRegionTagForSettlement(SettlementId.Parse(999), out tag), Is.False);
    }
}

