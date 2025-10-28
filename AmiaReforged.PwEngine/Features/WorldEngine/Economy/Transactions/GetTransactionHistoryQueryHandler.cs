using AmiaReforged.PwEngine.Database.Entities.Economy;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Transactions;

/// <summary>
/// Handles GetTransactionHistoryQuery execution.
/// Retrieves transaction history for a persona from the repository.
/// </summary>
[ServiceBinding(typeof(IQueryHandler<GetTransactionHistoryQuery, IEnumerable<Transaction>>))]
public class GetTransactionHistoryQueryHandler : IQueryHandler<GetTransactionHistoryQuery, IEnumerable<Transaction>>
{
    private readonly ITransactionRepository _repository;

    public GetTransactionHistoryQueryHandler(ITransactionRepository repository)
    {
        _repository = repository;
    }

    public async Task<IEnumerable<Transaction>> HandleAsync(
        GetTransactionHistoryQuery query,
        CancellationToken cancellationToken = default)
    {
        // Validate query
        (bool isValid, string? errorMessage) = query.Validate();
        if (!isValid)
        {
            throw new ArgumentException(errorMessage, nameof(query));
        }

        // Execute query
        return await _repository.GetHistoryAsync(
            query.PersonaId,
            query.PageSize,
            query.Page,
            cancellationToken);
    }
}

