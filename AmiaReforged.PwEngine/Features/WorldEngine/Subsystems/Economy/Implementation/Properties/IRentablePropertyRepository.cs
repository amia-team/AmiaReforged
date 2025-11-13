using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Properties;

/// <summary>
/// Abstraction over persistence for rentable property metadata and state.
/// </summary>
public interface IRentablePropertyRepository
{
    Task<RentablePropertySnapshot?> GetSnapshotAsync(PropertyId id, CancellationToken cancellationToken = default);

    Task<RentablePropertySnapshot?> GetSnapshotByInternalNameAsync(string internalName, CancellationToken cancellationToken = default);

    Task PersistRentalAsync(RentablePropertySnapshot snapshot, CancellationToken cancellationToken = default);

    Task<List<RentablePropertySnapshot>> GetAllPropertiesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all properties currently rented by the specified tenant.
    /// </summary>
    Task<List<RentablePropertySnapshot>> GetPropertiesRentedByTenantAsync(PersonaId tenantId, CancellationToken cancellationToken = default);
}
