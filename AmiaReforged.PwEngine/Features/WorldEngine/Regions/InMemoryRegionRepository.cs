using System.Collections.Generic;
using System.Linq;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Regions;

[ServiceBinding(typeof(IRegionRepository))]
public class InMemoryRegionRepository : IRegionRepository
{
    private readonly Dictionary<string, RegionDefinition> _regions = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<int, string> _settlementToRegionTag = new();
    private readonly Dictionary<string, int> _areaToSettlement = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<int, List<AreaDefinition>> _settlementToAreas = new();
    private readonly Dictionary<int, List<PlaceOfInterest>> _settlementToPois = new();
    private readonly Dictionary<string, int> _poiToSettlement = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, HashSet<int>> _regionToSettlements = new(StringComparer.OrdinalIgnoreCase);

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

    public void Clear()
    {
        _regions.Clear();
        _settlementToRegionTag.Clear();
        _areaToSettlement.Clear();
        _settlementToAreas.Clear();
        _settlementToPois.Clear();
        _poiToSettlement.Clear();
        _regionToSettlements.Clear();
    }

    private void RebuildIndexes()
    {
        _settlementToRegionTag.Clear();
        _areaToSettlement.Clear();
        _settlementToAreas.Clear();
        _settlementToPois.Clear();
        _poiToSettlement.Clear();
        _regionToSettlements.Clear();

        foreach ((string regionKey, RegionDefinition region) in _regions)
        {
            HashSet<int> regionSettlements = new();

            foreach (AreaDefinition area in region.Areas)
            {
                if (!string.IsNullOrWhiteSpace(area.ResRef.Value))
                {
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
                                }
                            }
                        }
                    }
                    else if (area.PlacesOfInterest is { Count: > 0 })
                    {
                        // POIs without linked settlements are ignored for settlement lookups but remain discoverable via area data.
                        foreach (PlaceOfInterest poi in area.PlacesOfInterest)
                        {
                            if (!string.IsNullOrWhiteSpace(poi.ResRef))
                            {
                                _poiToSettlement[poi.ResRef] = 0;
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

