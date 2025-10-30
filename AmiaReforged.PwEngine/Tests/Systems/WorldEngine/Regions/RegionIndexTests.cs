using System.Collections.Generic;
using AmiaReforged.PwEngine.Features.WorldEngine.Regions;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.ResourceNodes.ResourceNodeData;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.Regions;

[TestFixture]
public class RegionIndexTests
{
    [Test]
    public void Index_Resolves_Settlement_To_RegionTag()
    {
        InMemoryRegionRepository repo = new();
        repo.Add(new RegionDefinition { Tag = new RegionTag("r1"), Name = "Region One", Areas = [CreateArea("r1-a1", 1), CreateArea("r1-a2", 2)] });
        repo.Add(new RegionDefinition { Tag = new RegionTag("r2"), Name = "Region Two", Areas = [CreateArea("r2-a1", 3)] });

        RegionIndex index = new(repo);
        Assert.That(index.TryGetRegionTagForSettlement(SettlementId.Parse(2), out RegionTag? tag), Is.True);
        Assert.That(tag?.Value, Is.EqualTo("r1"));
        Assert.That(index.TryGetRegionTagForSettlement(SettlementId.Parse(3), out tag), Is.True);
        Assert.That(tag?.Value, Is.EqualTo("r2"));
        Assert.That(index.TryGetRegionTagForSettlement(SettlementId.Parse(999), out tag), Is.False);
    }

    private static AreaDefinition CreateArea(string resRef, int settlement)
    {
        return new AreaDefinition(
            new AreaTag(resRef),
            new List<string>(),
            new EnvironmentData(Climate.Temperate, EconomyQuality.Average, new QualityRange()),
            LinkedSettlement: SettlementId.Parse(settlement));
    }
}

