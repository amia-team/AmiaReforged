using System.Text.RegularExpressions;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Properties;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Regions;
using Anvil.API;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops;

[ServiceBinding(typeof(ShopLocationResolver))]
public sealed class ShopLocationResolver
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private static readonly Regex SettlementIdentifierPattern =
        new("^[a-z0-9_-]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly RegionIndex _regions;

    public ShopLocationResolver(RegionIndex regions)
    {
        _regions = regions;
    }

    public bool TryResolve(NpcShop shop, NwCreature shopkeeper, out ShopLocationMetadata metadata)
    {
        metadata = default!;

        if (shopkeeper is null)
        {
            return false;
        }

        NwArea? area = shopkeeper.Area;
        string? areaResRef = area?.ResRef;
        string? areaTag = area?.Tag;
        string? areaName = area?.Name;
        string? shopkeeperTag = string.IsNullOrWhiteSpace(shopkeeper.Tag) ? shop.ShopkeeperTag : shopkeeper.Tag;
        string? shopkeeperResRef = shopkeeper.ResRef;

        return TryResolve(shop.Tag, shop.DisplayName, shopkeeperTag, shopkeeperResRef, areaResRef, areaTag, areaName,
            out metadata);
    }

    public bool TryResolve(
        string shopTag,
        string shopDisplayName,
        string? shopkeeperTag,
        string? shopkeeperResRef,
        string? areaResRef,
        string? areaTag,
        string? areaName,
        out ShopLocationMetadata metadata)
    {
        metadata = default!;

        if (string.IsNullOrWhiteSpace(shopTag) && string.IsNullOrWhiteSpace(shopkeeperTag) &&
            string.IsNullOrWhiteSpace(areaResRef))
        {
            return false;
        }

        IReadOnlyList<RegionDefinition> snapshot = _regions.All();
        foreach (RegionDefinition region in snapshot)
        {
            foreach (AreaDefinition area in region.Areas)
            {
                PlaceOfInterest? poi = ResolveShopPoi(area, areaTag, areaResRef, shopTag, shopkeeperTag, shopkeeperResRef);
                if (poi is null)
                {
                    continue;
                }

                SettlementId? settlementId = ResolveSettlementId(area, poi);
                if (settlementId is not { Value: > 0 })
                {
                    Log.Warn("Shop POI '{PoiTag}' in area '{AreaResRef}' does not resolve to a settlement.",
                        poi.Tag ?? poi.ResRef ?? "<unknown>", area.ResRef.Value);
                    continue;
                }

                string resolvedAreaResRef = ResolveAreaResRef(areaResRef, area, poi);
                string? resolvedAreaTag = ResolveAreaTag(areaTag, poi);
                string? resolvedAreaName = ResolveAreaName(areaName, poi);
                string displayName = ResolveDisplayName(shopDisplayName, poi);
                SettlementTag settlementTag = BuildSettlementTag(region, settlementId.Value, poi);

                metadata = new ShopLocationMetadata(
                    shopTag,
                    displayName,
                    shopkeeperTag,
                    shopkeeperResRef,
                    resolvedAreaResRef,
                    resolvedAreaTag,
                    resolvedAreaName,
                    settlementId.Value,
                    settlementTag,
                    region.Tag,
                    poi.Tag,
                    poi.ResRef,
                    poi.Name,
                    poi.Description);

                return true;
            }
        }

        return false;
    }

    private PlaceOfInterest? ResolveShopPoi(
        AreaDefinition areaDefinition,
        string? areaTag,
        string? areaResRef,
        string shopTag,
        string? shopkeeperTag,
        string? shopkeeperResRef)
    {
        if (areaDefinition.PlacesOfInterest is not { Count: > 0 })
        {
            return null;
        }

        List<PlaceOfInterest> shops = areaDefinition.PlacesOfInterest
            .Where(static p => p.Type == PoiType.Shop)
            .ToList();

        if (shops.Count == 0)
        {
            return null;
        }

        PlaceOfInterest? match = shops.FirstOrDefault(p =>
            MatchesTag(p, shopTag) ||
            MatchesTag(p, shopkeeperTag) ||
            MatchesTag(p, areaTag));

        match ??= shops.FirstOrDefault(p =>
            MatchesResRef(p, shopkeeperResRef) ||
            MatchesResRef(p, areaResRef));

        if (match is null && MatchesArea(areaDefinition, areaResRef))
        {
            match = shops.FirstOrDefault();
        }

        return match;
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
            Log.Warn(ex, "Invalid area resref '{ResRef}' when resolving settlement for shop POI.",
                areaDefinition.ResRef.Value);
        }

        return null;
    }

    private static string ResolveAreaResRef(string? areaResRef, AreaDefinition areaDefinition, PlaceOfInterest poi)
    {
        if (!string.IsNullOrWhiteSpace(areaResRef))
        {
            return areaResRef.Trim();
        }

        if (!string.IsNullOrWhiteSpace(poi.ResRef))
        {
            return poi.ResRef.Trim();
        }

        return areaDefinition.ResRef.Value;
    }

    private static string? ResolveAreaTag(string? areaTag, PlaceOfInterest poi)
    {
        if (!string.IsNullOrWhiteSpace(areaTag))
        {
            return areaTag.Trim();
        }

        if (!string.IsNullOrWhiteSpace(poi.Tag))
        {
            return poi.Tag.Trim();
        }

        return null;
    }

    private static string? ResolveAreaName(string? areaName, PlaceOfInterest poi)
    {
        if (!string.IsNullOrWhiteSpace(areaName))
        {
            return areaName.Trim();
        }

        if (!string.IsNullOrWhiteSpace(poi.Name))
        {
            return poi.Name.Trim();
        }

        return null;
    }

    private static string ResolveDisplayName(string shopDisplayName, PlaceOfInterest poi)
    {
        if (!string.IsNullOrWhiteSpace(poi.Name))
        {
            return poi.Name.Trim();
        }

        return shopDisplayName;
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

    private static bool MatchesTag(PlaceOfInterest poi, string? tag)
    {
        return !string.IsNullOrWhiteSpace(tag) &&
               !string.IsNullOrWhiteSpace(poi.Tag) &&
               string.Equals(poi.Tag, tag, StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesResRef(PlaceOfInterest poi, string? resRef)
    {
        return !string.IsNullOrWhiteSpace(resRef) &&
               !string.IsNullOrWhiteSpace(poi.ResRef) &&
               string.Equals(poi.ResRef, resRef, StringComparison.OrdinalIgnoreCase);
    }

    private static bool MatchesArea(AreaDefinition areaDefinition, string? areaResRef)
    {
        return !string.IsNullOrWhiteSpace(areaResRef) &&
               string.Equals(areaDefinition.ResRef.Value, areaResRef, StringComparison.OrdinalIgnoreCase);
    }
}

public sealed record ShopLocationMetadata(
    string ShopTag,
    string ShopDisplayName,
    string? ShopkeeperTag,
    string? ShopkeeperResRef,
    string AreaResRef,
    string? AreaTag,
    string? AreaName,
    SettlementId SettlementId,
    SettlementTag Settlement,
    RegionTag RegionTag,
    string? PoiTag,
    string? PoiResRef,
    string? PoiName,
    string? PoiDescription);
