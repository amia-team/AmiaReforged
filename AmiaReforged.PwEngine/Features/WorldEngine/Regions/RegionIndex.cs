using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Regions;

/// <summary>
/// Facade read-model for region lookups, so callers don't need the full repository.
/// </summary>
[ServiceBinding(typeof(RegionIndex))]
public class RegionIndex(IRegionRepository regions)
{
    public bool TryGetRegionTagForSettlement(int settlementId, out string? regionTag)
    {
        regionTag = null;
        if (regions.TryGetRegionBySettlement(settlementId, out RegionDefinition? region) && region is not null)
        {
            regionTag = region.Tag;
            return true;
        }
        return false;
    }

    public IReadOnlyCollection<int> GetSettlementsForRegion(string regionTag)
    {
        // Return a distinct, stable-order copy so callers don't observe internal mutations
        var items = regions.GetSettlements(regionTag);
        List<int> result = new();
        HashSet<int> seen = new();
        foreach (var s in items)
        {
            if (seen.Add(s)) result.Add(s);
        }
        return result;
    }

    public IReadOnlyList<RegionDefinition> All()
    {
        // Return a snapshot (copy) to avoid external observers seeing future mutations
        var all = regions.All();
        var snapshot = new List<RegionDefinition>(all.Count);
        foreach (var r in all)
        {
            snapshot.Add(new RegionDefinition
            {
                Tag = r.Tag,
                Name = r.Name,
                Areas = new List<AreaDefinition>(r.Areas),
                Settlements = new List<int>(r.Settlements)
            });
        }
        return snapshot;
    }
}
