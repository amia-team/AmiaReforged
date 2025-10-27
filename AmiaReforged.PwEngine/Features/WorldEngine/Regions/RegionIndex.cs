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
        => regions.GetSettlements(regionTag);

    public IEnumerable<RegionDefinition> All() => regions.All();
}

