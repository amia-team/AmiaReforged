using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Regions;

[ServiceBinding(typeof(IRegionRepository))]
public class InMemoryRegionRepository : IRegionRepository
{
    // Region aggregates and core indexes
    private readonly Dictionary<string, RegionDefinition> _regions = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<int, string> _settlementToRegionTag = new();
    private readonly Dictionary<string, int> _areaToSettlement = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<int, List<AreaDefinition>> _settlementToAreas = new();
    private readonly Dictionary<int, List<PlaceOfInterest>> _settlementToPois = new();
    private readonly Dictionary<string, int> _poiToSettlement = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, HashSet<int>> _regionToSettlements = new(StringComparer.OrdinalIgnoreCase);

    // Area-to-region index for O(1) area registration lookups
    private readonly Dictionary<string, string> _areaToRegionTag = new(StringComparer.OrdinalIgnoreCase);

    // Optimized POI indexes for O(1) direct lookups
    private readonly Dictionary<string, PlaceOfInterest> _poiByResRef = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<string>> _poiByTag = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<PoiType, List<string>> _poiByType = new();
    private readonly Dictionary<string, List<string>> _poiByArea = new(StringComparer.OrdinalIgnoreCase);

    // Composite location cache for single-query resolution
    private readonly Dictionary<string, PoiLocationInfo> _poiLocationCache = new(StringComparer.OrdinalIgnoreCase);

    public void Add(RegionDefinition definition)
    {
        _regions[definition.Tag] = definition;
        RebuildIndexes();
    }

    public void Update(RegionDefinition definition)
    {
        _regions[definition.Tag] = definition;
        RebuildIndexes();
    }

    public bool Exists(RegionTag tag)
    {
        return _regions.ContainsKey(tag);
    }

    public List<RegionDefinition> All()
    {
        return _regions.Values.ToList();
    }

    public bool TryGetRegionBySettlement(SettlementId settlementId, out RegionDefinition? region)
    {
        region = null;
        if (_settlementToRegionTag.TryGetValue(settlementId, out string? tag) && _regions.TryGetValue(tag, out RegionDefinition? reg))
        {
            region = reg;
            return true;
        }

        return false;
    }

    public IReadOnlyCollection<SettlementId> GetSettlements(RegionTag regionTag)
    {
        if (!_regionToSettlements.TryGetValue(regionTag, out HashSet<int>? settlementIds) || settlementIds.Count == 0)
        {
            return Array.Empty<SettlementId>();
        }

        List<SettlementId> result = new(settlementIds.Count);
        foreach (int id in settlementIds)
        {
            result.Add(SettlementId.Parse(id));
        }

        return result;
    }

    public bool TryGetSettlementForArea(AreaTag areaTag, out SettlementId settlementId)
    {
        settlementId = default;
        if (_areaToSettlement.TryGetValue(areaTag.Value, out int id) && id > 0)
        {
            settlementId = SettlementId.Parse(id);
            return true;
        }

        return false;
    }

    public IReadOnlyList<AreaDefinition> GetAreasForSettlement(SettlementId settlementId)
    {
        if (!_settlementToAreas.TryGetValue(settlementId.Value, out List<AreaDefinition>? areas) || areas.Count == 0)
        {
            return Array.Empty<AreaDefinition>();
        }

        return CloneAreas(areas);
    }

    public IReadOnlyList<PlaceOfInterest> GetPointsOfInterest(SettlementId settlementId)
    {
        if (!_settlementToPois.TryGetValue(settlementId.Value, out List<PlaceOfInterest>? pois) || pois.Count == 0)
        {
            return Array.Empty<PlaceOfInterest>();
        }

        return ClonePois(pois);
    }

    public bool TryGetSettlementForPointOfInterest(string poiResRef, out SettlementId settlementId)
    {
        settlementId = default;
        if (_poiToSettlement.TryGetValue(poiResRef, out int id) && id > 0)
        {
            settlementId = SettlementId.Parse(id);
            return true;
        }

        return false;
    }

    public bool TryGetPointOfInterestByResRef(string poiResRef, out PlaceOfInterest poi)
    {
        poi = default;
        return _poiByResRef.TryGetValue(poiResRef, out poi!);
    }

    public IReadOnlyList<PlaceOfInterest> GetPointsOfInterestByTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag) || !_poiByTag.TryGetValue(tag, out List<string>? poiResRefs))
        {
            return Array.Empty<PlaceOfInterest>();
        }

        List<PlaceOfInterest> result = new(poiResRefs.Count);
        foreach (string resRef in poiResRefs)
        {
            if (_poiByResRef.TryGetValue(resRef, out PlaceOfInterest poi))
            {
                result.Add(poi);
            }
        }

        return result;
    }

    public IReadOnlyList<PlaceOfInterest> GetPointsOfInterestByType(PoiType type)
    {
        if (type == PoiType.Undefined || !_poiByType.TryGetValue(type, out List<string>? poiResRefs))
        {
            return Array.Empty<PlaceOfInterest>();
        }

        List<PlaceOfInterest> result = new(poiResRefs.Count);
        foreach (string resRef in poiResRefs)
        {
            if (_poiByResRef.TryGetValue(resRef, out PlaceOfInterest poi))
            {
                result.Add(poi);
            }
        }

        return result;
    }

    public IReadOnlyList<PlaceOfInterest> GetPointsOfInterestForArea(AreaTag areaTag)
    {
        if (!_poiByArea.TryGetValue(areaTag.Value, out List<string>? poiResRefs))
        {
            return Array.Empty<PlaceOfInterest>();
        }

        List<PlaceOfInterest> result = new(poiResRefs.Count);
        foreach (string resRef in poiResRefs)
        {
            if (_poiByResRef.TryGetValue(resRef, out PlaceOfInterest poi))
            {
                result.Add(poi);
            }
        }

        return result;
    }

    public PoiLocationInfo? GetPoiLocationInfo(string poiResRef)
    {
        return _poiLocationCache.TryGetValue(poiResRef, out PoiLocationInfo? info) ? info : null;
    }

    public bool IsAreaRegistered(string areaResRef)
    {
        return _areaToRegionTag.ContainsKey(areaResRef);
    }

    public bool TryGetRegionForArea(string areaResRef, out RegionDefinition? region)
    {
        region = null;
        if (_areaToRegionTag.TryGetValue(areaResRef, out string? regionTag)
            && _regions.TryGetValue(regionTag, out RegionDefinition? def))
        {
            region = def;
            return true;
        }

        return false;
    }

    public void Clear()
    {
        _regions.Clear();
        _settlementToRegionTag.Clear();
        _areaToSettlement.Clear();
        _areaToRegionTag.Clear();
        _settlementToAreas.Clear();
        _settlementToPois.Clear();
        _poiToSettlement.Clear();
        _regionToSettlements.Clear();

        // Clear optimized indexes
        _poiByResRef.Clear();
        _poiByTag.Clear();
        _poiByType.Clear();
        _poiByArea.Clear();
        _poiLocationCache.Clear();
    }

    private void RebuildIndexes()
    {
        _settlementToRegionTag.Clear();
        _areaToSettlement.Clear();
        _areaToRegionTag.Clear();
        _settlementToAreas.Clear();
        _settlementToPois.Clear();
        _poiToSettlement.Clear();
        _regionToSettlements.Clear();

        // Clear optimized indexes
        _poiByResRef.Clear();
        _poiByTag.Clear();
        _poiByType.Clear();
        _poiByArea.Clear();
        _poiLocationCache.Clear();

        foreach ((string regionKey, RegionDefinition region) in _regions)
        {
            HashSet<int> regionSettlements = new();

            foreach (AreaDefinition area in region.Areas)
            {
                if (!string.IsNullOrWhiteSpace(area.ResRef.Value))
                {
                    // Index every area to its parent region for O(1) registration checks
                    _areaToRegionTag[area.ResRef.Value] = regionKey;

                    if (area.LinkedSettlement is { } settlement)
                    {
                        regionSettlements.Add(settlement.Value);
                        _settlementToRegionTag[settlement.Value] = regionKey;
                        _areaToSettlement[area.ResRef.Value] = settlement.Value;

                        List<AreaDefinition> areasForSettlement = _settlementToAreas.TryGetValue(settlement.Value, out List<AreaDefinition>? existingAreas)
                            ? existingAreas
                            : (_settlementToAreas[settlement.Value] = new List<AreaDefinition>());
                        areasForSettlement.Add(area);

                        if (area.PlacesOfInterest is { Count: > 0 })
                        {
                            List<PlaceOfInterest> poisForSettlement = _settlementToPois.TryGetValue(settlement.Value, out List<PlaceOfInterest>? existingPois)
                                ? existingPois
                                : (_settlementToPois[settlement.Value] = new List<PlaceOfInterest>());

                            foreach (PlaceOfInterest poi in area.PlacesOfInterest)
                            {
                                poisForSettlement.Add(poi);
                                if (!string.IsNullOrWhiteSpace(poi.ResRef))
                                {
                                    _poiToSettlement[poi.ResRef] = settlement.Value;

                                    // Build optimized indexes
                                    IndexPoi(poi, settlement, region.Tag, area.ResRef);
                                }
                            }
                        }
                    }
                    else if (area.PlacesOfInterest is { Count: > 0 })
                    {
                        // POIs without linked settlements are still indexed for direct lookup
                        foreach (PlaceOfInterest poi in area.PlacesOfInterest)
                        {
                            if (!string.IsNullOrWhiteSpace(poi.ResRef))
                            {
                                _poiToSettlement[poi.ResRef] = 0;

                                // Index POI without settlement context
                                IndexPoi(poi, null, region.Tag, area.ResRef);
                            }
                        }
                    }
                }
            }

            if (regionSettlements.Count > 0)
            {
                _regionToSettlements[regionKey] = regionSettlements;
            }
        }
    }

    /// <summary>
    /// Indexes a single POI across all optimized lookup structures.
    /// Encapsulates the indexing logic for maintainability.
    /// </summary>
    private void IndexPoi(PlaceOfInterest poi, SettlementId? settlement, RegionTag region, AreaTag area)
    {
        // Index by ResRef (primary key for O(1) lookup)
        _poiByResRef[poi.ResRef] = poi;

        // Index by Tag (for tag-based queries)
        if (!string.IsNullOrWhiteSpace(poi.Tag))
        {
            if (!_poiByTag.TryGetValue(poi.Tag, out List<string>? tagList))
            {
                tagList = new List<string>();
                _poiByTag[poi.Tag] = tagList;
            }
            tagList.Add(poi.ResRef);
        }

        // Index by Type (for type-based queries)
        if (poi.Type != PoiType.Undefined)
        {
            if (!_poiByType.TryGetValue(poi.Type, out List<string>? typeList))
            {
                typeList = new List<string>();
                _poiByType[poi.Type] = typeList;
            }
            typeList.Add(poi.ResRef);
        }

        // Index by Area (for area-scoped queries)
        if (!_poiByArea.TryGetValue(area.Value, out List<string>? areaList))
        {
            areaList = new List<string>();
            _poiByArea[area.Value] = areaList;
        }
        areaList.Add(poi.ResRef);

        // Build composite location info for single-query resolution
        _poiLocationCache[poi.ResRef] = new PoiLocationInfo(poi, settlement, region, area);
    }

    private static IReadOnlyList<AreaDefinition> CloneAreas(IEnumerable<AreaDefinition> source)
    {
        List<AreaDefinition> copy = new();
        foreach (AreaDefinition area in source)
        {
            List<string> tags = new(area.DefinitionTags);
            List<PlaceOfInterest>? pois = area.PlacesOfInterest is { Count: > 0 }
                ? area.PlacesOfInterest.Select(ClonePoi).ToList()
                : null;

            copy.Add(new AreaDefinition(area.ResRef, tags, area.Environment, pois, area.LinkedSettlement));
        }

        return copy;
    }

    private static IReadOnlyList<PlaceOfInterest> ClonePois(IEnumerable<PlaceOfInterest> source)
    {
        return source.Select(ClonePoi).ToList();
    }

    private static PlaceOfInterest ClonePoi(PlaceOfInterest poi)
    {
        return poi with { };
    }
}

