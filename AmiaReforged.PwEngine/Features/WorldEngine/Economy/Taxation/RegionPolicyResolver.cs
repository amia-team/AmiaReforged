using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities.Economy.Treasuries;
using AmiaReforged.PwEngine.Features.WorldEngine.Regions;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Taxation;

/// <summary>
/// Helper to resolve the region tag for a given coinhouse (by tag or settlement) for policy lookup.
/// </summary>
[ServiceBinding(typeof(RegionPolicyResolver))]
public class RegionPolicyResolver(ICoinhouseRepository coinhouses, RegionIndex regionIndex)
{
    public bool TryGetRegionTagForCoinhouseTag(string coinhouseTag, out string? regionTag)
    {
        regionTag = null;
        if (string.IsNullOrWhiteSpace(coinhouseTag)) return false;

        try
        {
            CoinHouse? ch = coinhouses.GetByTag(coinhouseTag);
            if (ch is null) return false;

            try
            {
                return regionIndex.TryGetRegionTagForSettlement(ch.Settlement, out regionTag);
            }
            catch
            {
                regionTag = null;
                return false;
            }
        }
        catch
        {
            regionTag = null;
            return false;
        }
    }

    public bool TryGetRegionTagForSettlement(int settlementId, out string? regionTag)
    {
        regionTag = null;
        try
        {
            return regionIndex.TryGetRegionTagForSettlement(settlementId, out regionTag);
        }
        catch
        {
            regionTag = null;
            return false;
        }
    }
}
