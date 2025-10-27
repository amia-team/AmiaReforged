namespace AmiaReforged.PwEngine.Features.WorldEngine.Regions;

public interface IRegionRepository
{
    void Add(RegionDefinition definition);
    void Update(RegionDefinition definition);
    bool Exists(string tag);
    List<RegionDefinition> All();

    // New queries to support settlement-region mapping
    bool TryGetRegionBySettlement(int settlementId, out RegionDefinition? region);
    IReadOnlyCollection<int> GetSettlements(string regionTag);

    // Clear all regions and indexes (used on reload)
    void Clear();
}
