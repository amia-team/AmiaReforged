using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Properties;
using Anvil.API;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Features.Player.Housing;

[ServiceBinding(typeof(PropertyDefinitionSynchronizer))]
public sealed class PropertyDefinitionSynchronizer
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private const string PropertyIdScope = "player-housing-area";

    private readonly IRentablePropertyRepository _properties;
    private readonly PropertyMetadataResolver _metadataResolver;

    public PropertyDefinitionSynchronizer(
        IRentablePropertyRepository properties,
        PropertyMetadataResolver metadataResolver)
    {
        _properties = properties;
        _metadataResolver = metadataResolver;
    }

    public async Task SynchronizeModuleHousingAsync()
    {
        List<PropertyAreaMetadata> metadataList = new();

        foreach (NwArea area in NwModule.Instance.Areas)
        {
            try
            {
                if (_metadataResolver.TryCapture(area, out PropertyAreaMetadata metadata))
                {
                    metadataList.Add(metadata);
                }
            }
            catch (Exception ex)
            {
                Log.Warn(ex, "Failed to capture housing metadata for area {AreaTag}.", area.Tag ?? "<untagged>");
            }
        }

        foreach (PropertyAreaMetadata metadata in metadataList)
        {
            try
            {
                PropertyId propertyId = ResolvePropertyId(metadata);
                await EnsureSnapshotAsync(propertyId, metadata).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Failed to provision housing record for area {AreaTag}.", metadata.AreaTag);
            }
        }
    }

    public PropertyId ResolvePropertyId(PropertyAreaMetadata metadata) => DerivePropertyId(metadata.AreaTag);

    public async Task<RentablePropertySnapshot?> EnsureSnapshotAsync(
        PropertyId propertyId,
        PropertyAreaMetadata metadata)
    {
        RentablePropertySnapshot? existing = await _properties.GetSnapshotAsync(propertyId).ConfigureAwait(false);
        if (existing is not null)
        {
            return existing;
        }

        RentablePropertyDefinition definition = BuildDefinition(propertyId, metadata);
        PropertyOccupancyStatus occupancy = metadata.DefaultOwner is null
            ? PropertyOccupancyStatus.Vacant
            : PropertyOccupancyStatus.Owned;

        RentablePropertySnapshot seed = new(
            definition,
            occupancy,
            CurrentTenant: null,
            CurrentOwner: metadata.DefaultOwner,
            Residents: Array.Empty<PersonaId>(),
            ActiveRental: null);

        await _properties.PersistRentalAsync(seed).ConfigureAwait(false);

        Log.Info(
            "Provisioned housing record for area {AreaTag} with property id {PropertyId}.",
            metadata.AreaTag,
            propertyId);

        return seed;
    }

    private static RentablePropertyDefinition BuildDefinition(
        PropertyId propertyId,
        PropertyAreaMetadata metadata)
    {
        RentablePropertyDefinition definition = new(
            propertyId,
            metadata.InternalName,
            metadata.Settlement,
            metadata.Category,
            metadata.MonthlyRent,
            metadata.AllowsCoinhouseRental,
            metadata.AllowsDirectRental,
            metadata.SettlementCoinhouseTag,
            metadata.PurchasePrice,
            metadata.MonthlyOwnershipTax,
            metadata.EvictionGraceDays)
        {
            DefaultOwner = metadata.DefaultOwner
        };

        return definition;
    }

    private static PropertyId DerivePropertyId(string areaTag)
    {
        if (string.IsNullOrWhiteSpace(areaTag))
        {
            throw new ArgumentException("Area tag cannot be empty when deriving property id.", nameof(areaTag));
        }

        Guid derived = DeterministicGuidFactory.Create(PropertyIdScope, areaTag);
        return PropertyId.Parse(derived);
    }
}
