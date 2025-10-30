using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Regions;

/// <summary>
/// Facade read-model for region lookups, so callers don't need the full repository.
/// </summary>
[ServiceBinding(typeof(RegionIndex))]
public class RegionIndex(IRegionRepository regions)
{
    public bool TryGetRegionTagForSettlement(SettlementId settlementId, out RegionTag? regionTag)
    {
        regionTag = null;
        if (regions.TryGetRegionBySettlement(settlementId, out RegionDefinition? region) && region is not null)
        {
            regionTag = region.Tag;
            return true;
        }
        return false;
    }

    public IReadOnlyCollection<SettlementId> GetSettlementsForRegion(RegionTag regionTag)
    {
        // Return a distinct, stable-order copy so callers don't observe internal mutations
        IReadOnlyCollection<SettlementId> items = regions.GetSettlements(regionTag);
        List<SettlementId> result = [];
        HashSet<int> seen = [];
        foreach (SettlementId s in items)
        {
            // Use underlying int value for deduplication
            if (seen.Add(s.Value)) result.Add(s);
        }
        return result;
    }

    public IReadOnlyList<RegionDefinition> All()
    {
        // Return a snapshot (copy) to avoid external observers seeing future mutations
        List<RegionDefinition> all = regions.All();
        List<RegionDefinition> snapshot = new List<RegionDefinition>(all.Count);
        foreach (RegionDefinition r in all)
        {
            snapshot.Add(new RegionDefinition
            {
                Tag = r.Tag,
                Name = r.Name,
                Areas = [..r.Areas],
                Settlements = [..r.Settlements]
            });
        }
        return snapshot;
    }
}
