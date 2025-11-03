using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Properties;
using AmiaReforged.PwEngine.Database.Entities.Economy.Treasuries;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Regions;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using Anvil.API;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.Player.Housing;

[ServiceBinding(typeof(PropertyMetadataResolver))]
public sealed class PropertyMetadataResolver
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private const int DefaultEvictionGraceDays = 2;

    private static readonly Regex SettlementIdentifierPattern =
        new("^[a-z0-9_-]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly RegionIndex _regions;
    private readonly ICoinhouseRepository _coinhouses;

    public PropertyMetadataResolver(RegionIndex regions, ICoinhouseRepository coinhouses)
    {
        _regions = regions;
        _coinhouses = coinhouses;
    }

    public bool TryCapture(NwArea area, out PropertyAreaMetadata metadata)
    {
        metadata = default!;

        if (!TryResolveContext(area, out RegionDefinition? region, out AreaDefinition? areaDefinition,
                out PlaceOfInterest? poi, out SettlementId settlementId))
        {
            return false;
        }

        string areaTag = ResolveAreaTag(area, poi!);
        string areaResRef = ResolveAreaResRef(area, areaDefinition!, poi!);
        string displayName = ResolveInternalName(area, poi!, areaDefinition!);
        PropertyCategory category = ResolvePropertyCategory(poi!);
        SettlementTag settlement = BuildSettlementTag(region!, settlementId, poi!);
        CoinhouseTag? coinhouseTag = ResolveCoinhouseTag(settlementId);

        metadata = new PropertyAreaMetadata(
            areaTag,
            areaResRef,
            displayName,
            category,
            settlement,
            MonthlyRent: GoldAmount.Zero,
            AllowsCoinhouseRental: coinhouseTag is not null,
            AllowsDirectRental: true,
            SettlementCoinhouseTag: coinhouseTag,
            PurchasePrice: null,
            MonthlyOwnershipTax: null,
            EvictionGraceDays: DefaultEvictionGraceDays,
            DefaultOwner: null,
            RegionTag: region!.Tag,
            PoiTag: poi!.Tag,
            PoiResRef: poi!.ResRef,
            Description: poi!.Description);

        return true;
    }

    public bool TryGetHousingAreaContext(
        string areaResRef,
        string? areaTag,
        out RegionDefinition? regionDefinition,
        out AreaDefinition? areaDefinition,
        out PlaceOfInterest? pointOfInterest)
    {
        regionDefinition = null;
        areaDefinition = null;
        pointOfInterest = null;

        if (string.IsNullOrWhiteSpace(areaResRef))
        {
            return false;
        }

        IReadOnlyList<RegionDefinition> snapshot = _regions.All();
        foreach (RegionDefinition region in snapshot)
        {
            foreach (AreaDefinition area in region.Areas)
            {
                if (!string.Equals(area.ResRef.Value, areaResRef, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                PlaceOfInterest? housePoi = ResolveHousePoi(area, areaTag, areaResRef);
                if (housePoi is null)
                {
                    continue;
                }

                regionDefinition = region;
                areaDefinition = area;
                pointOfInterest = housePoi;
                return true;
            }
        }

        return false;
    }

    private bool TryResolveContext(
        NwArea area,
        out RegionDefinition? region,
        out AreaDefinition? areaDefinition,
        out PlaceOfInterest? poi,
        out SettlementId settlementId)
    {
        region = null;
        areaDefinition = null;
        poi = null;
        settlementId = default;

        string? areaResRef = area.ResRef;
        string? areaTag = area.Tag;

        if (string.IsNullOrWhiteSpace(areaResRef) && string.IsNullOrWhiteSpace(areaTag))
        {
            return false;
        }

        IReadOnlyList<RegionDefinition> snapshot = _regions.All();
        foreach (RegionDefinition candidateRegion in snapshot)
        {
            foreach (AreaDefinition candidateArea in candidateRegion.Areas)
            {
                PlaceOfInterest? housePoi = ResolveHousePoi(candidateArea, areaTag, areaResRef);
                if (housePoi is null)
                {
                    continue;
                }

                SettlementId? resolvedSettlement = ResolveSettlementId(candidateArea, housePoi);
                if (resolvedSettlement is not { Value: > 0 })
                {
                    continue;
                }

                region = candidateRegion;
                areaDefinition = candidateArea;
                poi = housePoi;
                settlementId = resolvedSettlement.Value;
                return true;
            }
        }

        return false;
    }

    private static PlaceOfInterest? ResolveHousePoi(
        AreaDefinition areaDefinition,
        string? areaTag,
        string? areaResRef)
    {
        if (areaDefinition.PlacesOfInterest is not { Count: > 0 })
        {
            return null;
        }

        return areaDefinition.PlacesOfInterest.FirstOrDefault(p =>
                   p.Type == PoiType.House &&
                   (MatchesTag(p, areaTag) || MatchesResRef(p, areaResRef) || MatchesArea(areaDefinition, areaResRef)))
               ?? areaDefinition.PlacesOfInterest.FirstOrDefault(p =>
                   p.Type == PoiType.House && MatchesArea(areaDefinition, areaResRef));

        static bool MatchesTag(PlaceOfInterest poi, string? tag)
        {
            return !string.IsNullOrWhiteSpace(tag) &&
                   !string.IsNullOrWhiteSpace(poi.Tag) &&
                   string.Equals(poi.Tag, tag, StringComparison.OrdinalIgnoreCase);
        }

        static bool MatchesResRef(PlaceOfInterest poi, string? resRef)
        {
            return !string.IsNullOrWhiteSpace(resRef) &&
                   !string.IsNullOrWhiteSpace(poi.ResRef) &&
                   string.Equals(poi.ResRef, resRef, StringComparison.OrdinalIgnoreCase);
        }

        static bool MatchesArea(AreaDefinition area, string? resRef)
        {
            return !string.IsNullOrWhiteSpace(resRef) &&
                   string.Equals(area.ResRef.Value, resRef, StringComparison.OrdinalIgnoreCase);
        }
    }

    private SettlementId? ResolveSettlementId(AreaDefinition areaDefinition, PlaceOfInterest poi)
    {
        if (areaDefinition.LinkedSettlement is { Value: > 0 } direct)
        {
            return direct;
        }

        if (!string.IsNullOrWhiteSpace(poi.ResRef) &&
            _regions.TryGetSettlementForPointOfInterest(poi.ResRef, out SettlementId poiSettlement) &&
            poiSettlement.Value > 0)
        {
            return poiSettlement;
        }

        try
        {
            if (_regions.TryGetSettlementForArea(areaDefinition.ResRef, out SettlementId areaSettlement) &&
                areaSettlement.Value > 0)
            {
                return areaSettlement;
            }
        }
        catch (ArgumentException ex)
        {
            Log.Warn(ex, "Invalid area resref '{ResRef}' when resolving settlement for housing area.",
                areaDefinition.ResRef.Value);
        }

        return null;
    }

    private static string ResolveAreaTag(NwArea area, PlaceOfInterest poi)
    {
        if (!string.IsNullOrWhiteSpace(poi.Tag))
        {
            return poi.Tag.Trim();
        }

        if (!string.IsNullOrWhiteSpace(area.Tag))
        {
            return area.Tag.Trim();
        }

        throw new InvalidOperationException("Housing areas must have a tag or matching POI tag.");
    }

    private static string ResolveAreaResRef(NwArea area, AreaDefinition areaDefinition, PlaceOfInterest poi)
    {
        if (!string.IsNullOrWhiteSpace(area.ResRef))
        {
            return area.ResRef;
        }

        if (!string.IsNullOrWhiteSpace(poi.ResRef))
        {
            return poi.ResRef.Trim();
        }

        return areaDefinition.ResRef.Value;
    }

    private static string ResolveInternalName(NwArea area, PlaceOfInterest poi, AreaDefinition areaDefinition)
    {
        if (!string.IsNullOrWhiteSpace(poi.Name))
        {
            return poi.Name.Trim();
        }

        if (!string.IsNullOrWhiteSpace(area.Name))
        {
            return area.Name.Trim();
        }

        if (!string.IsNullOrWhiteSpace(area.Tag))
        {
            return area.Tag.Trim();
        }

        return areaDefinition.ResRef.Value;
    }

    private static PropertyCategory ResolvePropertyCategory(PlaceOfInterest poi)
    {
        return poi.Type switch
        {
            PoiType.Shop => PropertyCategory.Commercial,
            PoiType.Guild or PoiType.Temple or PoiType.Library => PropertyCategory.GuildHall,
            PoiType.Warehouse => PropertyCategory.Industrial,
            _ => PropertyCategory.Residential
        };
    }

    private SettlementTag BuildSettlementTag(RegionDefinition region, SettlementId settlementId, PlaceOfInterest poi)
    {
        string? candidate = NormalizeSettlementIdentifier(poi.Tag)
                            ?? NormalizeSettlementIdentifier(poi.Name)
                            ?? ResolveSettlementTagFromRegion(region);

        if (string.IsNullOrWhiteSpace(candidate))
        {
            candidate = $"settlement-{settlementId.Value}";
        }

        return new SettlementTag(candidate);
    }

    private CoinhouseTag? ResolveCoinhouseTag(SettlementId settlementId)
    {
        if (settlementId.Value <= 0)
        {
            return null;
        }

        try
        {
            CoinHouse? coinhouse = _coinhouses.GetSettlementCoinhouse(settlementId);
            if (coinhouse is not null)
            {
                return coinhouse.CoinhouseTag;
            }

            string? derivedTag = ResolveCoinhouseTagFromSettlement(settlementId);
            if (!string.IsNullOrWhiteSpace(derivedTag))
            {
                return new CoinhouseTag(derivedTag);
            }
        }
        catch (Exception ex)
        {
            Log.Warn(ex, "Failed to derive coinhouse tag for settlement {SettlementId}.", settlementId.Value);
        }

        return null;
    }

    private string? ResolveCoinhouseTagFromSettlement(SettlementId settlementId)
    {
        try
        {
            IReadOnlyList<PlaceOfInterest> pois = _regions.GetPointsOfInterestForSettlement(settlementId);
            PlaceOfInterest? candidate = pois.FirstOrDefault(p => p.Type == PoiType.Bank)
                                           ?? pois.FirstOrDefault(p => p.Type == PoiType.Guild)
                                           ?? pois.FirstOrDefault(p => p.Type == PoiType.Temple)
                                           ?? pois.FirstOrDefault();

            if (candidate is null)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(candidate.Tag))
            {
                return candidate.Tag.Trim();
            }

            if (!string.IsNullOrWhiteSpace(candidate.ResRef))
            {
                return candidate.ResRef.Trim();
            }
        }
        catch (Exception ex)
        {
            Log.Warn(ex, "Failed to resolve coinhouse tag from settlement {SettlementId}.", settlementId.Value);
        }

        return null;
    }

    private static string? NormalizeSettlementIdentifier(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string trimmed = value.Trim();

        if (trimmed.StartsWith("settlement:", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed["settlement:".Length..];
        }

        int lastColon = trimmed.LastIndexOf(':');
        if (lastColon >= 0 && lastColon < trimmed.Length - 1)
        {
            trimmed = trimmed[(lastColon + 1)..];
        }

        if (trimmed.EndsWith("_coinhouse", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed[..^"_coinhouse".Length];
        }
        else if (trimmed.EndsWith("_bank", StringComparison.OrdinalIgnoreCase))
        {
            trimmed = trimmed[..^"_bank".Length];
        }

        trimmed = trimmed.Trim('_', '-');
        if (trimmed.Length == 0)
        {
            return null;
        }

        if (!SettlementIdentifierPattern.IsMatch(trimmed))
        {
            return null;
        }

        return trimmed.ToLowerInvariant();
    }

    private static string? ResolveSettlementTagFromRegion(RegionDefinition region)
    {
        string? candidate = NormalizeSettlementIdentifier(region.Tag.Value);
        if (!string.IsNullOrWhiteSpace(candidate))
        {
            return candidate;
        }

        return NormalizeSettlementIdentifier(region.Name);
    }
}

public sealed record PropertyAreaMetadata(
    string AreaTag,
    string AreaResRef,
    string InternalName,
    PropertyCategory Category,
    SettlementTag Settlement,
    GoldAmount MonthlyRent,
    bool AllowsCoinhouseRental,
    bool AllowsDirectRental,
    CoinhouseTag? SettlementCoinhouseTag,
    GoldAmount? PurchasePrice,
    GoldAmount? MonthlyOwnershipTax,
    int EvictionGraceDays,
    PersonaId? DefaultOwner,
    RegionTag RegionTag,
    string? PoiTag,
    string? PoiResRef,
    string? Description);
