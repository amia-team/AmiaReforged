using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Regions;

public interface IRegionRepository
{
    void Add(RegionDefinition definition);
    void Update(RegionDefinition definition);
    bool Exists(RegionTag tag);
    List<RegionDefinition> All();

    // Settlement-region mapping queries
    bool TryGetRegionBySettlement(SettlementId settlementId, out RegionDefinition? region);
    IReadOnlyCollection<SettlementId> GetSettlements(RegionTag regionTag);
    bool TryGetSettlementForArea(AreaTag areaTag, out SettlementId settlementId);
    IReadOnlyList<AreaDefinition> GetAreasForSettlement(SettlementId settlementId);

    // Legacy POI queries (settlement-based)
    IReadOnlyList<PlaceOfInterest> GetPointsOfInterest(SettlementId settlementId);
    bool TryGetSettlementForPointOfInterest(string poiResRef, out SettlementId settlementId);

    // Direct POI queries (optimized - O(1) or O(k))
    bool TryGetPointOfInterestByResRef(string poiResRef, out PlaceOfInterest poi);
    IReadOnlyList<PlaceOfInterest> GetPointsOfInterestByTag(string tag);
    IReadOnlyList<PlaceOfInterest> GetPointsOfInterestByType(PoiType type);
    IReadOnlyList<PlaceOfInterest> GetPointsOfInterestForArea(AreaTag areaTag);

    // Composite query - returns POI with full location context in single O(1) operation
    PoiLocationInfo? GetPoiLocationInfo(string poiResRef);

    // Area-to-region lookup
    /// <summary>
    /// Checks whether an area (by resref) is defined in any region.
    /// </summary>
    bool IsAreaRegistered(string areaResRef);

    /// <summary>
    /// Retrieves the <see cref="RegionDefinition"/> that contains the given area, if any.
    /// </summary>
    bool TryGetRegionForArea(string areaResRef, out RegionDefinition? region);

    // Delete a single region by tag, returns true if it existed
    bool Delete(RegionTag tag);

    // Clear all regions and indexes (used on reload)
    void Clear();
}
