using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Regions;

public interface IRegionRepository
{
    void Add(RegionDefinition definition);
    void Update(RegionDefinition definition);
    bool Exists(RegionTag tag);
    List<RegionDefinition> All();

    // New queries to support settlement-region mapping
    bool TryGetRegionBySettlement(SettlementId settlementId, out RegionDefinition? region);
    IReadOnlyCollection<SettlementId> GetSettlements(RegionTag regionTag);

    // Clear all regions and indexes (used on reload)
    void Clear();
}
