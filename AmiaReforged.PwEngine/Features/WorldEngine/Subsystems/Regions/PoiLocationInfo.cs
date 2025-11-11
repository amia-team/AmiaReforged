using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Regions;

/// <summary>
/// Value object that represents the complete location context of a Point of Interest.
/// Encapsulates POI data along with its containing Settlement, Region, and Area.
/// This composite allows single O(1) query instead of multiple queries and filtering.
/// </summary>
/// <remarks>
/// DDD Pattern: Value Object - Immutable, equality by value, no identity
/// Performance: Eliminates anti-pattern of "get settlement, get all POIs, filter by known ResRef"
/// </remarks>
public sealed record PoiLocationInfo(
    PlaceOfInterest Poi,
    SettlementId? SettlementId,
    RegionTag RegionTag,
    AreaTag AreaTag)
{
    /// <summary>
    /// Indicates whether this POI is linked to a settlement (has economic/social context).
    /// </summary>
    public bool HasSettlement => SettlementId is { Value: > 0 };

    /// <summary>
    /// Provides a human-readable description of the POI's location.
    /// </summary>
    public string LocationDescription =>
        HasSettlement
            ? $"{Poi.Name} in {AreaTag.Value} (Settlement: {SettlementId!.Value}, Region: {RegionTag.Value})"
            : $"{Poi.Name} in {AreaTag.Value} (Region: {RegionTag.Value})";
}

