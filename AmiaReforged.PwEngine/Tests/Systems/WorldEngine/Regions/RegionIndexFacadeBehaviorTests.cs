using AmiaReforged.PwEngine.Features.WorldEngine.Regions;
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
        Assert.That(index.TryGetRegionTagForSettlement(999, out string? tag), Is.False);
        Assert.That(tag, Is.Null);
    }

    [Test]
    public void GetSettlementsForRegion_Deduplicates_And_Is_Stable_Copy()
    {
        InMemoryRegionRepository repo = new();
        // Intentionally include duplicates in input; repository doesn't dedupe
        repo.Add(new RegionDefinition { Tag = "r1", Name = "Region One", Areas = new(), Settlements = new(){5,5,6,5,7}});
        RegionIndex index = new(repo);

        List<int> first = index.GetSettlementsForRegion("r1").ToList();
        CollectionAssert.AreEqual(new[]{5,6,7}, first);

        // Mutate the underlying repo after snapshot
        repo.Update(new RegionDefinition { Tag = "r1", Name = "Region One", Areas = new(), Settlements = new(){7,6,5,4} });

        List<int> second = index.GetSettlementsForRegion("r1").ToList();
        CollectionAssert.AreEqual(new[]{7,6,5,4}.Distinct().ToArray(), second);
        // Ensure 'first' remains unchanged
        CollectionAssert.AreEqual(new[]{5,6,7}, first);
    }

    [Test]
    public void All_Returns_Snapshot_That_Does_Not_Mutate_On_Reload()
    {
        InMemoryRegionRepository repo = new();
        repo.Add(new RegionDefinition { Tag = "r1", Name = "Region One", Areas = new(), Settlements = new(){1} });
        repo.Add(new RegionDefinition { Tag = "r2", Name = "Region Two", Areas = new(), Settlements = new(){2} });

        RegionIndex index = new(repo);
        IReadOnlyList<RegionDefinition> snapshot = index.All();
        Assert.That(snapshot.Count, Is.EqualTo(2));
        Assert.That(snapshot.Any(r => r.Tag == "r1" && r.Settlements.SequenceEqual(new[]{1})), Is.True);
        Assert.That(snapshot.Any(r => r.Tag == "r2" && r.Settlements.SequenceEqual(new[]{2})), Is.True);

        // Simulate reload by clearing and re-adding different data
        repo.Clear();
        repo.Add(new RegionDefinition { Tag = "r3", Name = "Region Three", Areas = new(), Settlements = new(){3} });

        // Original snapshot must remain unchanged
        Assert.That(snapshot.Count, Is.EqualTo(2));
        Assert.That(snapshot.Any(r => r.Tag == "r3"), Is.False);
    }
}

