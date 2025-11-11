using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Regions;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Properties.Queries;

/// <summary>
/// Query handler to locate rentable properties by their POI ResRef.
/// Coordinates between Region domain (POI lookup) and Property domain (rental state).
/// </summary>
[ServiceBinding(typeof(GetPropertyByPoiQueryHandler))]
public sealed class GetPropertyByPoiQueryHandler
{
    private readonly RegionIndex _regionIndex;
    private readonly IRentablePropertyRepository _propertyRepository;

    public GetPropertyByPoiQueryHandler(
        RegionIndex regionIndex,
        IRentablePropertyRepository propertyRepository)
    {
        _regionIndex = regionIndex;
        _propertyRepository = propertyRepository;
    }

    public async Task<RentablePropertySnapshot?> HandleAsync(
        GetPropertyByPoiQuery query,
        CancellationToken cancellationToken = default)
    {
        // Step 1: Resolve the POI ResRef to a Settlement
        if (!_regionIndex.TryGetSettlementForPointOfInterest(query.PoiResRef, out SettlementId settlementId))
        {
            return null;
        }

        // Step 2: Find all POIs for that settlement
        IReadOnlyList<PlaceOfInterest> pois = _regionIndex.GetPointsOfInterestForSettlement(settlementId);
        PlaceOfInterest? targetPoi = pois.FirstOrDefault(p =>
            string.Equals(p.ResRef, query.PoiResRef, StringComparison.OrdinalIgnoreCase));

        if (targetPoi is null)
        {
            return null;
        }

        // Step 3: Use the POI Name as the InternalName to look up the property
        // InternalName convention: Properties are identified by their POI Name (domain-side)
        RentablePropertySnapshot? property = await _propertyRepository.GetSnapshotByInternalNameAsync(
            targetPoi.Name,
            cancellationToken);

        return property;
    }
}
