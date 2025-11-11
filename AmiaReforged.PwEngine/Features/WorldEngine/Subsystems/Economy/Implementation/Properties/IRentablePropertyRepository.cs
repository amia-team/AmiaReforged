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
}
