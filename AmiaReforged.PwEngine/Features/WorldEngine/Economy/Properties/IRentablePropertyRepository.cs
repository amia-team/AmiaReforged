namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Properties;

/// <summary>
/// Abstraction over persistence for rentable property metadata and state.
/// </summary>
public interface IRentablePropertyRepository
{
    Task<RentablePropertySnapshot?> GetSnapshotAsync(PropertyId id, CancellationToken cancellationToken = default);

    Task PersistRentalAsync(RentablePropertySnapshot snapshot, CancellationToken cancellationToken = default);
}
