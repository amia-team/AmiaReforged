using AmiaReforged.PwEngine.Database.Entities.Economy;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Transactions;

/// <summary>
/// Repository for managing transaction persistence.
/// Handles CRUD operations for gold transfers between personas.
/// </summary>
public interface ITransactionRepository
{
    /// <summary>
    /// Records a new transaction between two personas.
    /// </summary>
    /// <param name="transaction">The transaction to record</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The recorded transaction with generated ID</returns>
    Task<Transaction> RecordTransactionAsync(Transaction transaction, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a transaction by its ID.
    /// </summary>
    /// <param name="transactionId">The transaction ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The transaction, or null if not found</returns>
    Task<Transaction?> GetByIdAsync(long transactionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets transaction history for a persona.
    /// Returns both incoming and outgoing transactions.
    /// </summary>
    /// <param name="personaId">The persona to query</param>
    /// <param name="pageSize">Number of transactions per page</param>
    /// <param name="page">Page number (0-based)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of transactions ordered by timestamp (newest first)</returns>
    Task<IEnumerable<Transaction>> GetHistoryAsync(
        PersonaId personaId,
        int pageSize = 50,
        int page = 0,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets outgoing transactions (where persona is the sender).
    /// </summary>
    /// <param name="fromPersonaId">The sending persona</param>
    /// <param name="pageSize">Number of transactions per page</param>
    /// <param name="page">Page number (0-based)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of outgoing transactions ordered by timestamp (newest first)</returns>
    Task<IEnumerable<Transaction>> GetOutgoingAsync(
        PersonaId fromPersonaId,
        int pageSize = 50,
        int page = 0,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets incoming transactions (where persona is the receiver).
    /// </summary>
    /// <param name="toPersonaId">The receiving persona</param>
    /// <param name="pageSize">Number of transactions per page</param>
    /// <param name="page">Page number (0-based)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of incoming transactions ordered by timestamp (newest first)</returns>
    Task<IEnumerable<Transaction>> GetIncomingAsync(
        PersonaId toPersonaId,
        int pageSize = 50,
        int page = 0,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets transactions between two specific personas.
    /// </summary>
    /// <param name="personaId1">First persona</param>
    /// <param name="personaId2">Second persona</param>
    /// <param name="pageSize">Number of transactions per page</param>
    /// <param name="page">Page number (0-based)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of transactions between the two personas ordered by timestamp (newest first)</returns>
    Task<IEnumerable<Transaction>> GetBetweenPersonasAsync(
        PersonaId personaId1,
        PersonaId personaId2,
        int pageSize = 50,
        int page = 0,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total amount sent by a persona.
    /// </summary>
    /// <param name="fromPersonaId">The sending persona</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Total amount sent</returns>
    Task<int> GetTotalSentAsync(PersonaId fromPersonaId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the total amount received by a persona.
    /// </summary>
    /// <param name="toPersonaId">The receiving persona</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Total amount received</returns>
    Task<int> GetTotalReceivedAsync(PersonaId toPersonaId, CancellationToken cancellationToken = default);
}

