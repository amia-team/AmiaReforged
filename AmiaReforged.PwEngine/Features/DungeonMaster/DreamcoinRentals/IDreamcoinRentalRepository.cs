using AmiaReforged.PwEngine.Database.Entities.Admin;

namespace AmiaReforged.PwEngine.Features.DungeonMaster.DreamcoinRentals;

/// <summary>
/// Repository interface for managing dreamcoin rentals.
/// </summary>
public interface IDreamcoinRentalRepository
{
    /// <summary>
    /// Gets a rental by its ID.
    /// </summary>
    Task<DreamcoinRental?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all rentals for a specific player CD Key.
    /// </summary>
    Task<List<DreamcoinRental>> GetByPlayerCdKeyAsync(string cdKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all active rentals.
    /// </summary>
    Task<List<DreamcoinRental>> GetAllActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all rentals (active and inactive).
    /// </summary>
    Task<List<DreamcoinRental>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all rentals that are due for payment processing.
    /// </summary>
    Task<List<DreamcoinRental>> GetRentalsDueForPaymentAsync(DateTime asOfDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all delinquent rentals.
    /// </summary>
    Task<List<DreamcoinRental>> GetDelinquentRentalsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Adds a new rental.
    /// </summary>
    Task AddAsync(DreamcoinRental rental, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing rental.
    /// </summary>
    Task UpdateAsync(DreamcoinRental rental, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a rental by its ID.
    /// </summary>
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a rental as delinquent.
    /// </summary>
    Task MarkDelinquentAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks a rental as paid and updates the next due date.
    /// </summary>
    Task MarkPaidAsync(int id, DateTime paymentDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deactivates a rental.
    /// </summary>
    Task DeactivateAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reactivates a rental.
    /// </summary>
    Task ReactivateAsync(int id, CancellationToken cancellationToken = default);
}
