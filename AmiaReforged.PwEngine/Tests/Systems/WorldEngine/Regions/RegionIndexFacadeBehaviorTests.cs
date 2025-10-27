using AmiaReforged.PwEngine.Features.WorldEngine.Regions;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using NUnit.Framework;

namespace AmiaReforged.PwEngine.Tests.Systems.WorldEngine.Regions;

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
        // Intentionally include duplicates in input; repository doesn't dedupe
        var settlements = new List<SettlementId>
        {
            SettlementId.Parse(5),
            SettlementId.Parse(5),
            SettlementId.Parse(6),
            SettlementId.Parse(5),
            SettlementId.Parse(7)
        };
        repo.Add(new RegionDefinition { Tag = new RegionTag("r1"), Name = "Region One", Areas = new(), Settlements = settlements});
        RegionIndex index = new(repo);

        List<int> first = index.GetSettlementsForRegion(new RegionTag("r1")).Select(s => s.Value).ToList();
        CollectionAssert.AreEqual(new[]{5,6,7}, first);

        // Mutate the underlying repo after snapshot
        var newSettlements = new List<SettlementId>
        {
            SettlementId.Parse(7),
            SettlementId.Parse(6),
            SettlementId.Parse(5),
            SettlementId.Parse(4)
        };
        repo.Update(new RegionDefinition { Tag = new RegionTag("r1"), Name = "Region One", Areas = new(), Settlements = newSettlements });

        List<int> second = index.GetSettlementsForRegion(new RegionTag("r1")).Select(s => s.Value).ToList();
        CollectionAssert.AreEqual(new[]{7,6,5,4}.Distinct().ToArray(), second);
        // Ensure 'first' remains unchanged
        CollectionAssert.AreEqual(new[]{5,6,7}, first);
    }

    [Test]
    public void All_Returns_Snapshot_That_Does_Not_Mutate_On_Reload()
    {
        InMemoryRegionRepository repo = new();
        repo.Add(new RegionDefinition { Tag = new RegionTag("r1"), Name = "Region One", Areas = new(), Settlements = new(){SettlementId.Parse(1)} });
        repo.Add(new RegionDefinition { Tag = new RegionTag("r2"), Name = "Region Two", Areas = new(), Settlements = new(){SettlementId.Parse(2)} });

        RegionIndex index = new(repo);
        IReadOnlyList<RegionDefinition> snapshot = index.All();
        Assert.That(snapshot.Count, Is.EqualTo(2));
        Assert.That(snapshot.Any(r => r.Tag.Value == "r1" && r.Settlements.Select(s => s.Value).SequenceEqual(new[]{1})), Is.True);
        Assert.That(snapshot.Any(r => r.Tag.Value == "r2" && r.Settlements.Select(s => s.Value).SequenceEqual(new[]{2})), Is.True);

        // Simulate reload by clearing and re-adding different data
        repo.Clear();
        repo.Add(new RegionDefinition { Tag = new RegionTag("r3"), Name = "Region Three", Areas = new(), Settlements = new(){SettlementId.Parse(3)} });

        // Original snapshot must remain unchanged
        Assert.That(snapshot.Count, Is.EqualTo(2));
        Assert.That(snapshot.Any(r => r.Tag.Value == "r3"), Is.False);
    }
}

