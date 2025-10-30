using System.Collections.Generic;
using System.Linq;
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
                Areas = CloneAreas(r.Areas)
            });
        }
        return snapshot;
    }

    public bool TryGetSettlementForArea(AreaTag areaTag, out SettlementId settlementId)
    {
        return regions.TryGetSettlementForArea(areaTag, out settlementId);
    }

    public IReadOnlyList<AreaDefinition> GetAreasForSettlement(SettlementId settlementId)
    {
        return CloneAreas(regions.GetAreasForSettlement(settlementId));
    }

    public IReadOnlyList<PlaceOfInterest> GetPointsOfInterestForSettlement(SettlementId settlementId)
    {
        return ClonePois(regions.GetPointsOfInterest(settlementId));
    }

    public bool TryGetSettlementForPointOfInterest(string poiResRef, out SettlementId settlementId)
    {
        return regions.TryGetSettlementForPointOfInterest(poiResRef, out settlementId);
    }

    private static List<AreaDefinition> CloneAreas(IEnumerable<AreaDefinition> areas)
    {
        List<AreaDefinition> copy = new();
        foreach (AreaDefinition area in areas)
        {
            List<string> tags = new(area.DefinitionTags);
            List<PlaceOfInterest>? pois = area.PlacesOfInterest is { Count: > 0 }
                ? area.PlacesOfInterest.Select(ClonePoi).ToList()
                : null;

            copy.Add(new AreaDefinition(area.ResRef, tags, area.Environment, pois, area.LinkedSettlement));
        }

        return copy;
    }

    private static List<PlaceOfInterest> ClonePois(IEnumerable<PlaceOfInterest> pois)
    {
        return pois.Select(ClonePoi).ToList();
    }

    private static PlaceOfInterest ClonePoi(PlaceOfInterest poi) => poi with { };
}
