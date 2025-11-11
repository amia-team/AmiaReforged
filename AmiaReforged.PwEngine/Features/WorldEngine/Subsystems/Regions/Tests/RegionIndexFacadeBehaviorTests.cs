using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.ResourceNodes.ResourceNodeData;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Regions.Tests;

[TestFixture]
public class RegionIndexFacadeBehaviorTests
{
    [Test]
    public void Unknown_Settlement_Returns_False_And_Null_Tag()
    {
        InMemoryRegionRepository repo = new();
        RegionIndex index = new(repo);
        Assert.That(index.TryGetRegionTagForSettlement(SettlementId.Parse(999), out RegionTag? tag), Is.False);
        Assert.That(tag, Is.Null);
    }

    [Test]
    public void GetSettlementsForRegion_Deduplicates_And_Is_Stable_Copy()
    {
        InMemoryRegionRepository repo = new();
        repo.Add(new RegionDefinition
        {
            Tag = new RegionTag("r1"),
            Name = "Region One",
            Areas =
            [
                CreateArea("a1", 5),
                CreateArea("a2", 5),
                CreateArea("a3", 6),
                CreateArea("a4", 5),
                CreateArea("a5", 7)
            ]
        });
        RegionIndex index = new(repo);

        List<int> first = index.GetSettlementsForRegion(new RegionTag("r1")).Select(s => s.Value).ToList();
        CollectionAssert.AreEqual(new[]{5,6,7}, first);

        // Mutate the underlying repo after snapshot
        repo.Update(new RegionDefinition
        {
            Tag = new RegionTag("r1"),
            Name = "Region One",
            Areas =
            [
                CreateArea("a10", 7),
                CreateArea("a11", 6),
                CreateArea("a12", 5),
                CreateArea("a13", 4)
            ]
        });

        List<int> second = index.GetSettlementsForRegion(new RegionTag("r1")).Select(s => s.Value).ToList();
        CollectionAssert.AreEqual(new[]{7,6,5,4}.Distinct().ToArray(), second);
        // Ensure 'first' remains unchanged
        CollectionAssert.AreEqual(new[]{5,6,7}, first);
    }

    [Test]
    public void All_Returns_Snapshot_That_Does_Not_Mutate_On_Reload()
    {
        InMemoryRegionRepository repo = new();
        repo.Add(new RegionDefinition
        {
            Tag = new RegionTag("r1"),
            Name = "Region One",
            Areas = [CreateArea("r1-a1", 1)]
        });
        repo.Add(new RegionDefinition
        {
            Tag = new RegionTag("r2"),
            Name = "Region Two",
            Areas = [CreateArea("r2-a1", 2)]
        });

        RegionIndex index = new(repo);
        IReadOnlyList<RegionDefinition> snapshot = index.All();
        Assert.That(snapshot.Count, Is.EqualTo(2));
        Assert.That(snapshot.Any(r => r.Tag.Value == "r1" && r.Areas.Any(a => a.LinkedSettlement?.Value == 1)), Is.True);
        Assert.That(snapshot.Any(r => r.Tag.Value == "r2" && r.Areas.Any(a => a.LinkedSettlement?.Value == 2)), Is.True);

        // Simulate reload by clearing and re-adding different data
        repo.Clear();
        repo.Add(new RegionDefinition
        {
            Tag = new RegionTag("r3"),
            Name = "Region Three",
            Areas = [CreateArea("r3-a1", 3)]
        });

        // Original snapshot must remain unchanged
        Assert.That(snapshot.Count, Is.EqualTo(2));
        Assert.That(snapshot.Any(r => r.Tag.Value == "r3"), Is.False);
    }

    [Test]
    public void TryGetSettlementForArea_ReturnsTrue_WhenAreaLinked()
    {
        InMemoryRegionRepository repo = new();
        repo.Add(new RegionDefinition
        {
            Tag = new RegionTag("r1"),
            Name = "Region One",
            Areas = [CreateArea("area_linked", 12)]
        });

        RegionIndex index = new(repo);
        bool found = index.TryGetSettlementForArea(new AreaTag("area_linked"), out SettlementId settlement);

        Assert.That(found, Is.True);
        Assert.That(settlement.Value, Is.EqualTo(12));
    }

    [Test]
    public void PointsOfInterest_AreQueryable_By_Settlement()
    {
        PlaceOfInterest bank = new("bank_interior", "bank_tag", "Bank", PoiType.Bank);
        InMemoryRegionRepository repo = new();
        repo.Add(new RegionDefinition
        {
            Tag = new RegionTag("r1"),
            Name = "Region One",
            Areas = [CreateArea("area_bank", 99, new List<PlaceOfInterest> { bank })]
        });

        RegionIndex index = new(repo);

        IReadOnlyList<PlaceOfInterest> pois = index.GetPointsOfInterestForSettlement(SettlementId.Parse(99));
        Assert.That(pois, Has.Count.EqualTo(1));
        Assert.That(pois[0].ResRef, Is.EqualTo("bank_interior"));
        Assert.That(index.TryGetSettlementForPointOfInterest("bank_interior", out SettlementId settlement), Is.True);
        Assert.That(settlement.Value, Is.EqualTo(99));
    }

    private static AreaDefinition CreateArea(string resRef, int settlementId, List<PlaceOfInterest>? pois = null)
    {
        return new AreaDefinition(
            new AreaTag(resRef),
            new List<string>(),
            new EnvironmentData(Climate.Temperate, EconomyQuality.Average, new QualityRange()),
            pois,
            SettlementId.Parse(settlementId));
    }
}

