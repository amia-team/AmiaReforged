using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Properties;
using AmiaReforged.PwEngine.Database.Entities.Economy.Treasuries;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Regions;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
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
    internal static readonly string[] PropertyIdVariableNames =
    {
        "rentable_property_id",
        "property_id",
        "house_property_id"
    };

    private readonly RegionIndex _regions;
    private readonly ICoinhouseRepository _coinhouses;

    public PropertyMetadataResolver(RegionIndex regions, ICoinhouseRepository coinhouses)
    {
        _regions = regions;
        _coinhouses = coinhouses;
    }

    public PropertyId? TryResolveExplicitPropertyId(NwArea area)
    {
        foreach (string variableName in PropertyIdVariableNames)
        {
            if (TryParsePropertyId(area.GetObjectVariable<LocalVariableString>(variableName), out PropertyId propertyId))
            {
                return propertyId;
            }
        }

        return null;
    }

    public static bool IsHouseArea(NwArea area)
    {
        return area.GetObjectVariable<LocalVariableInt>("is_house").Value > 0;
    }

    public PropertyAreaMetadata Capture(NwArea area, PropertyId? explicitPropertyId)
    {
        string? areaTag = area.Tag;
        if (string.IsNullOrWhiteSpace(areaTag))
        {
            throw new InvalidOperationException("House areas must be tagged to participate in housing.");
        }

        string areaResRef = area.ResRef ?? throw new InvalidOperationException("House areas must have a valid resref.");

        string internalName = ResolveInternalName(area);
        PropertyCategory category = ResolvePropertyCategory(area);
        SettlementTag settlement = ResolveSettlementTag(area);
        GoldAmount monthlyRent = ResolveMonthlyRent(area);
        CoinhouseTag? coinhouseTag = ResolveCoinhouseTag(area);
        bool allowsCoinhouse = ResolveBoolean(area, "allows_coinhouse_rental", coinhouseTag is not null);
        bool allowsDirect = ResolveBoolean(area, "allows_direct_rental", true);
        GoldAmount? purchasePrice = ResolveOptionalGold(area, "purchase_price");
        GoldAmount? ownershipTax = ResolveOptionalGold(area, "monthly_ownership_tax");
        int evictionGraceDays = ResolveEvictionGraceDays(area);
        PersonaId? defaultOwner = ResolveDefaultOwner(area);

        return new PropertyAreaMetadata(
            areaTag,
            areaResRef,
            internalName,
            category,
            settlement,
            monthlyRent,
            allowsCoinhouse,
            allowsDirect,
            coinhouseTag,
            purchasePrice,
            ownershipTax,
            evictionGraceDays,
            defaultOwner,
            explicitPropertyId);
    }

    internal static bool TryParsePropertyId(LocalVariableString variable, out PropertyId propertyId)
    {
        propertyId = default;

        if (!variable.HasValue)
        {
            return false;
        }

        string? value = variable.Value;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        if (!Guid.TryParse(value, out Guid parsed) || parsed == Guid.Empty)
        {
            Log.Warn("Invalid property id '{Value}' encountered in local variable '{VarName}'.", value, variable.Name);
            return false;
        }

        propertyId = PropertyId.Parse(parsed);
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

        RegionDefinition? fallbackRegion = null;
        AreaDefinition? fallbackArea = null;
        PlaceOfInterest? fallbackPoi = null;

        IReadOnlyList<RegionDefinition> regions = _regions.All();
        foreach (RegionDefinition region in regions)
        {
            foreach (AreaDefinition area in region.Areas)
            {
                PlaceOfInterest? matchingPoi = area.PlacesOfInterest?
                    .FirstOrDefault(p =>
                        string.Equals(p.ResRef, areaResRef, StringComparison.OrdinalIgnoreCase) ||
                        (!string.IsNullOrWhiteSpace(areaTag) &&
                         string.Equals(p.Tag, areaTag, StringComparison.OrdinalIgnoreCase)));

                if (matchingPoi is not null)
                {
                    regionDefinition = region;
                    areaDefinition = area;
                    pointOfInterest = matchingPoi;
                    return true;
                }

                bool areaMatches = string.Equals(area.ResRef.Value, areaResRef, StringComparison.OrdinalIgnoreCase);
                if (!areaMatches)
                {
                    continue;
                }

                fallbackRegion ??= region;
                fallbackArea ??= area;

                if (fallbackPoi is null && area.PlacesOfInterest is { Count: > 0 })
                {
                    fallbackPoi = area.PlacesOfInterest.FirstOrDefault(p => p.Type == PoiType.House)
                                  ?? area.PlacesOfInterest.FirstOrDefault(p => p.Type != PoiType.Undefined)
                                  ?? area.PlacesOfInterest.First();
                }
            }
        }

        if (fallbackRegion is not null)
        {
            regionDefinition = fallbackRegion;
            areaDefinition = fallbackArea;
            pointOfInterest = fallbackPoi;
            return true;
        }

        return false;
    }

    private static string ResolveInternalName(NwArea area)
    {
        LocalVariableString internalNameVar = area.GetObjectVariable<LocalVariableString>("property_internal_name");
        if (internalNameVar.HasValue && !string.IsNullOrWhiteSpace(internalNameVar.Value))
        {
            return internalNameVar.Value.Trim();
        }

        return area.Tag ?? throw new InvalidOperationException("House areas must have a valid tag.");
    }

    private static PropertyCategory ResolvePropertyCategory(NwArea area)
    {
        LocalVariableString categoryVar = area.GetObjectVariable<LocalVariableString>("property_category");

        if (categoryVar.HasValue && !string.IsNullOrWhiteSpace(categoryVar.Value) &&
            Enum.TryParse<PropertyCategory>(categoryVar.Value, true, out PropertyCategory parsed))
        {
            return parsed;
        }

        return PropertyCategory.Residential;
    }

    private SettlementTag ResolveSettlementTag(NwArea area)
    {
        LocalVariableString settlementVar = area.GetObjectVariable<LocalVariableString>("settlement_tag");
        if (settlementVar.HasValue && !string.IsNullOrWhiteSpace(settlementVar.Value))
        {
            return new SettlementTag(settlementVar.Value.Trim());
        }

        string? areaResRef = area.ResRef;
        string? areaTag = area.Tag;

        if (!string.IsNullOrWhiteSpace(areaResRef))
        {
            if (_regions.TryGetSettlementForPointOfInterest(areaResRef, out SettlementId poiSettlement))
            {
                string? settlementFromPoi = ResolveSettlementTagFromSettlement(poiSettlement, areaResRef, areaTag);
                if (!string.IsNullOrWhiteSpace(settlementFromPoi))
                {
                    return new SettlementTag(settlementFromPoi);
                }
            }

            try
            {
                AreaTag areaResRefTag = new(areaResRef);
                if (_regions.TryGetSettlementForArea(areaResRefTag, out SettlementId settlementForArea))
                {
                    string? settlementFromArea =
                        ResolveSettlementTagFromSettlement(settlementForArea, areaResRef, areaTag);
                    if (!string.IsNullOrWhiteSpace(settlementFromArea))
                    {
                        return new SettlementTag(settlementFromArea);
                    }
                }
            }
            catch (ArgumentException ex)
            {
                Log.Warn(ex, "Invalid area resref '{ResRef}' while resolving settlement tag for area {AreaTag}.",
                    areaResRef, areaTag ?? "<untagged>");
            }

            try
            {
                if (TryGetHousingAreaContext(areaResRef, areaTag, out RegionDefinition? region,
                        out AreaDefinition? definition, out PlaceOfInterest? poi))
                {
                    string? settlementFromPoi = ExtractSettlementTagFromPoi(poi);
                    if (!string.IsNullOrWhiteSpace(settlementFromPoi))
                    {
                        return new SettlementTag(settlementFromPoi);
                    }

                    if (definition?.LinkedSettlement is { } linkedSettlement)
                    {
                        string? settlementFromLinked =
                            ResolveSettlementTagFromSettlement(linkedSettlement, areaResRef, areaTag);
                        if (!string.IsNullOrWhiteSpace(settlementFromLinked))
                        {
                            return new SettlementTag(settlementFromLinked);
                        }
                    }

                    string? settlementFromDefinition = ResolveSettlementTagFromAreaDefinition(definition);
                    if (!string.IsNullOrWhiteSpace(settlementFromDefinition))
                    {
                        return new SettlementTag(settlementFromDefinition);
                    }

                    string? settlementFromRegion = ResolveSettlementTagFromRegion(region);
                    if (!string.IsNullOrWhiteSpace(settlementFromRegion))
                    {
                        return new SettlementTag(settlementFromRegion);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Warn(ex, "Failed to derive settlement metadata for housing area {AreaTag} ({AreaResRef}).",
                    areaTag ?? "<untagged>", areaResRef);
            }
        }

        LocalVariableInt legacySettlement = area.GetObjectVariable<LocalVariableInt>("settlement");
        if (legacySettlement.HasValue && legacySettlement.Value > 0)
        {
            return new SettlementTag($"legacy:{legacySettlement.Value}");
        }

        string fallbackToken = !string.IsNullOrWhiteSpace(areaTag)
            ? areaTag
            : !string.IsNullOrWhiteSpace(areaResRef)
                ? areaResRef
                : "unassigned";

        return new SettlementTag($"legacy:{fallbackToken}");
    }

    private GoldAmount ResolveMonthlyRent(NwArea area)
    {
        LocalVariableInt rentVar = area.GetObjectVariable<LocalVariableInt>("monthly_rent");
        int rent = rentVar.HasValue ? rentVar.Value : 0;

        if (!rentVar.HasValue || rent <= 0)
        {
            LocalVariableInt legacyRent = area.GetObjectVariable<LocalVariableInt>("rent");
            if (legacyRent.HasValue && legacyRent.Value > 0)
            {
                rent = legacyRent.Value;
            }
        }

        rent = Math.Max(0, rent);
        return GoldAmount.Parse(rent);
    }

    private static bool ResolveBoolean(NwArea area, string variableName, bool defaultValue)
    {
        LocalVariableInt variable = area.GetObjectVariable<LocalVariableInt>(variableName);
        return variable.HasValue ? variable.Value > 0 : defaultValue;
    }

    private CoinhouseTag? ResolveCoinhouseTag(NwArea area)
    {
        string? areaResRef = area.ResRef;
        string? areaTag = area.Tag;
        if (string.IsNullOrWhiteSpace(areaResRef))
        {
            return null;
        }

        try
        {
            if (TryResolveSettlementId(areaResRef, areaTag, out SettlementId settlementId))
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
        }
        catch (Exception ex)
        {
            Log.Warn(ex, "Failed to derive coinhouse tag for housing area {AreaTag} ({AreaResRef}).",
                areaTag ?? "<untagged>", areaResRef);
        }

        return null;
    }

    private string? ResolveCoinhouseTagFromSettlement(SettlementId settlementId)
    {
        if (settlementId.Value <= 0)
        {
            return null;
        }

        try
        {
            IReadOnlyList<PlaceOfInterest> pois = _regions.GetPointsOfInterestForSettlement(settlementId);
            PlaceOfInterest? bankPoi = pois.FirstOrDefault(p => p.Type == PoiType.Bank)
                                       ?? pois.FirstOrDefault(p => p.Type == PoiType.Guild)
                                       ?? pois.FirstOrDefault(p => p.Type == PoiType.Temple)
                                       ?? pois.FirstOrDefault();

            if (bankPoi is null)
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(bankPoi.Tag))
            {
                return bankPoi.Tag.Trim();
            }

            if (!string.IsNullOrWhiteSpace(bankPoi.ResRef))
            {
                return bankPoi.ResRef.Trim();
            }
        }
        catch (Exception ex)
        {
            Log.Warn(ex, "Failed to resolve coinhouse tag from settlement {SettlementId}.", settlementId.Value);
        }

        return null;
    }

    private bool TryResolveSettlementId(string areaResRef, string? areaTag, out SettlementId settlementId)
    {
        settlementId = default;

        if (_regions.TryGetSettlementForPointOfInterest(areaResRef, out settlementId))
        {
            return settlementId.Value > 0;
        }

        try
        {
            AreaTag areaResRefTag = new(areaResRef);
            if (_regions.TryGetSettlementForArea(areaResRefTag, out settlementId) && settlementId.Value > 0)
            {
                return true;
            }
        }
        catch (ArgumentException ex)
        {
            Log.Debug(ex, "Invalid area resref '{ResRef}' while resolving settlement id for housing area {AreaTag}.",
                areaResRef, areaTag ?? "<untagged>");
        }

        if (TryGetHousingAreaContext(areaResRef, areaTag, out RegionDefinition? region,
                out AreaDefinition? areaDefinition, out _))
        {
            if (areaDefinition?.LinkedSettlement is { } linked && linked.Value > 0)
            {
                settlementId = linked;
                return true;
            }

            SettlementId? regionSettlement = region?.Areas
                .Select(a => a.LinkedSettlement)
                .FirstOrDefault(id => id is { Value: > 0 });

            if (regionSettlement is { } fallback)
            {
                settlementId = fallback;
                return true;
            }
        }

        settlementId = default;
        return false;
    }

    private static GoldAmount? ResolveOptionalGold(NwArea area, string variableName)
    {
        LocalVariableInt variable = area.GetObjectVariable<LocalVariableInt>(variableName);
        if (!variable.HasValue)
        {
            return null;
        }

        int value = Math.Max(0, variable.Value);
        return value == 0 ? null : GoldAmount.Parse(value);
    }

    private static int ResolveEvictionGraceDays(NwArea area)
    {
        LocalVariableInt graceVar = area.GetObjectVariable<LocalVariableInt>("eviction_grace_days");
        if (!graceVar.HasValue || graceVar.Value <= 0)
        {
            return DefaultEvictionGraceDays;
        }

        return graceVar.Value;
    }

    private static PersonaId? ResolveDefaultOwner(NwArea area)
    {
        LocalVariableString ownerVar = area.GetObjectVariable<LocalVariableString>("default_owner_persona");
        if (!ownerVar.HasValue || string.IsNullOrWhiteSpace(ownerVar.Value))
        {
            return null;
        }

        try
        {
            return PersonaId.Parse(ownerVar.Value);
        }
        catch (Exception ex)
        {
            Log.Warn(ex, "Invalid default owner persona '{Persona}' configured for area {AreaTag}.",
                ownerVar.Value,
                area.Tag);
            return null;
        }
    }

    private string? ResolveSettlementTagFromSettlement(SettlementId settlementId, string areaResRef, string? areaTag)
    {
        if (settlementId.Value <= 0)
        {
            return null;
        }

        try
        {
            IReadOnlyList<PlaceOfInterest> pois = _regions.GetPointsOfInterestForSettlement(settlementId);
            if (pois.Count == 0)
            {
                return null;
            }

            PlaceOfInterest? matchingPoi = pois.FirstOrDefault(p =>
                string.Equals(p.ResRef, areaResRef, StringComparison.OrdinalIgnoreCase) ||
                (!string.IsNullOrWhiteSpace(areaTag) &&
                 string.Equals(p.Tag, areaTag, StringComparison.OrdinalIgnoreCase)));

            string? candidate = ExtractSettlementTagFromPoi(matchingPoi);
            if (!string.IsNullOrWhiteSpace(candidate))
            {
                return candidate;
            }

            PlaceOfInterest? settlementPoi = pois.FirstOrDefault(p => p.Type == PoiType.Bank)
                                             ?? pois.FirstOrDefault(p => p.Type == PoiType.Guild)
                                             ?? pois.FirstOrDefault(p => p.Type == PoiType.Temple)
                                             ?? pois.FirstOrDefault(p => p.Type == PoiType.Library)
                                             ?? pois.FirstOrDefault();

            candidate = ExtractSettlementTagFromPoi(settlementPoi);
            if (!string.IsNullOrWhiteSpace(candidate))
            {
                return candidate;
            }
        }
        catch (Exception ex)
        {
            Log.Warn(ex, "Failed to resolve settlement tag from points of interest for settlement {SettlementId}.",
                settlementId.Value);
        }

        return null;
    }

    private static string? ExtractSettlementTagFromPoi(PlaceOfInterest? poi)
    {
        if (poi is null)
        {
            return null;
        }

        string? candidate = NormalizeSettlementIdentifier(poi.Tag);
        if (!string.IsNullOrWhiteSpace(candidate))
        {
            return candidate;
        }

        return NormalizeSettlementIdentifier(poi.Name);
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

    private static string? ResolveSettlementTagFromAreaDefinition(AreaDefinition? areaDefinition)
    {
        if (areaDefinition?.DefinitionTags is not { Count: > 0 })
        {
            return null;
        }

        foreach (string tag in areaDefinition.DefinitionTags)
        {
            if (string.IsNullOrWhiteSpace(tag))
            {
                continue;
            }

            string trimmed = tag.Trim();
            int colonIndex = trimmed.IndexOf(':');
            if (colonIndex <= 0 || colonIndex >= trimmed.Length - 1)
            {
                continue;
            }

            string prefix = trimmed[..colonIndex];
            if (!prefix.Equals("settlement", StringComparison.OrdinalIgnoreCase) &&
                !prefix.Equals("municipality", StringComparison.OrdinalIgnoreCase) &&
                !prefix.Equals("town", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string? candidate = NormalizeSettlementIdentifier(trimmed[(colonIndex + 1)..]);
            if (!string.IsNullOrWhiteSpace(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static string? ResolveSettlementTagFromRegion(RegionDefinition? region)
    {
        if (region is null)
        {
            return null;
        }

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
    PropertyId? ExplicitPropertyId);
