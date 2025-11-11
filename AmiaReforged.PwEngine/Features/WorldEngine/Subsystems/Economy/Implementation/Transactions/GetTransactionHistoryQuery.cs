using AmiaReforged.PwEngine.Database.Entities.Economy;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Transactions;

/// <summary>
/// Query to retrieve transaction history for a persona.
/// Returns both incoming and outgoing transactions.
/// </summary>
public sealed record GetTransactionHistoryQuery(
    PersonaId PersonaId,
    int PageSize = 50,
    int Page = 0
) : IQuery<IEnumerable<Transaction>>
{
    /// <summary>
    /// Validates the query parameters.
    /// </summary>
    public (bool IsValid, string? ErrorMessage) Validate()
    {
        if (PageSize <= 0)
            return (false, "PageSize must be greater than zero");

        if (PageSize > 1000)
            return (false, "PageSize cannot exceed 1000");

        if (Page < 0)
            return (false, "Page must be non-negative");

        return (true, null);
    }
}

