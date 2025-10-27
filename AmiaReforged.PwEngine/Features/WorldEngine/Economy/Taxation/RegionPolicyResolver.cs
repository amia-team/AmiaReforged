using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities.Economy.Treasuries;
using AmiaReforged.PwEngine.Features.WorldEngine.Regions;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
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
            var tag = new CoinhouseTag(coinhouseTag);
            CoinHouse? ch = coinhouses.GetByTag(tag);
            if (ch is null) return false;

            try
            {
                // ch.SettlementId returns SettlementId from the NotMapped property
                bool result = regionIndex.TryGetRegionTagForSettlement(ch.SettlementId, out RegionTag? regionTagValue);
                regionTag = regionTagValue?.Value;  // Extract string value
                return result;
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
            var settlementIdValue = SettlementId.Parse(settlementId);
            bool result = regionIndex.TryGetRegionTagForSettlement(settlementIdValue, out RegionTag? regionTagValue);
            regionTag = regionTagValue?.Value;  // Extract string value
            return result;
        }
        catch
        {
            regionTag = null;
            return false;
        }
    }
}
