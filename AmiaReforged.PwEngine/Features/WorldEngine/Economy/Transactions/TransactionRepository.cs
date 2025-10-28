using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities.Economy;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Transactions;

/// <summary>
/// EF Core implementation of transaction repository.
/// Manages persistence of gold transfers between personas.
/// </summary>
[ServiceBinding(typeof(ITransactionRepository))]
public class TransactionRepository : ITransactionRepository
{
    private readonly PwEngineContext _context;

    public TransactionRepository(PwEngineContext context)
    {
        _context = context;
    }

    public async Task<Transaction> RecordTransactionAsync(Transaction transaction, CancellationToken cancellationToken = default)
    {
        _context.Transactions.Add(transaction);
        await _context.SaveChangesAsync(cancellationToken);
        return transaction;
    }

    public async Task<Transaction?> GetByIdAsync(long transactionId, CancellationToken cancellationToken = default)
    {
        return await _context.Transactions
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == transactionId, cancellationToken);
    }

    public async Task<IEnumerable<Transaction>> GetHistoryAsync(
        PersonaId personaId,
        int pageSize = 50,
        int page = 0,
        CancellationToken cancellationToken = default)
    {
        string personaIdStr = personaId.ToString();

        return await _context.Transactions
            .AsNoTracking()
            .Where(t => t.FromPersonaId == personaIdStr || t.ToPersonaId == personaIdStr)
            .OrderByDescending(t => t.Timestamp)
            .Skip(page * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Transaction>> GetOutgoingAsync(
        PersonaId fromPersonaId,
        int pageSize = 50,
        int page = 0,
        CancellationToken cancellationToken = default)
    {
        string personaIdStr = fromPersonaId.ToString();

        return await _context.Transactions
            .AsNoTracking()
            .Where(t => t.FromPersonaId == personaIdStr)
            .OrderByDescending(t => t.Timestamp)
            .Skip(page * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Transaction>> GetIncomingAsync(
        PersonaId toPersonaId,
        int pageSize = 50,
        int page = 0,
        CancellationToken cancellationToken = default)
    {
        string personaIdStr = toPersonaId.ToString();

        return await _context.Transactions
            .AsNoTracking()
            .Where(t => t.ToPersonaId == personaIdStr)
            .OrderByDescending(t => t.Timestamp)
            .Skip(page * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<Transaction>> GetBetweenPersonasAsync(
        PersonaId personaId1,
        PersonaId personaId2,
        int pageSize = 50,
        int page = 0,
        CancellationToken cancellationToken = default)
    {
        string persona1Str = personaId1.ToString();
        string persona2Str = personaId2.ToString();

        return await _context.Transactions
            .AsNoTracking()
            .Where(t =>
                (t.FromPersonaId == persona1Str && t.ToPersonaId == persona2Str) ||
                (t.FromPersonaId == persona2Str && t.ToPersonaId == persona1Str))
            .OrderByDescending(t => t.Timestamp)
            .Skip(page * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetTotalSentAsync(PersonaId fromPersonaId, CancellationToken cancellationToken = default)
    {
        string personaIdStr = fromPersonaId.ToString();

        return await _context.Transactions
            .AsNoTracking()
            .Where(t => t.FromPersonaId == personaIdStr)
            .SumAsync(t => t.Amount, cancellationToken);
    }

    public async Task<int> GetTotalReceivedAsync(PersonaId toPersonaId, CancellationToken cancellationToken = default)
    {
        string personaIdStr = toPersonaId.ToString();

        return await _context.Transactions
            .AsNoTracking()
            .Where(t => t.ToPersonaId == personaIdStr)
            .SumAsync(t => t.Amount, cancellationToken);
    }
}

