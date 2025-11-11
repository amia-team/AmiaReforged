using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Features.WorldEngine.Regions;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Accounts;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Taxation;

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
            CoinhouseTag tag = new CoinhouseTag(coinhouseTag);
            CoinhouseDto? coinhouse = coinhouses
                .GetByTagAsync(tag)
                .GetAwaiter()
                .GetResult();
            if (coinhouse is null) return false;

            try
            {
                SettlementId settlementId = SettlementId.Parse(coinhouse.Settlement);
                bool result = regionIndex.TryGetRegionTagForSettlement(settlementId, out RegionTag? regionTagValue);
                regionTag = regionTagValue?.Value;
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
            SettlementId settlementIdValue = SettlementId.Parse(settlementId);
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
