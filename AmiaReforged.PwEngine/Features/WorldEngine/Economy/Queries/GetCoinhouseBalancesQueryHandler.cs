using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities.Economy.Treasuries;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.DTOs;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Queries;

/// <summary>
/// Handles GetCoinhouseBalancesQuery - retrieves all balances for a persona across all coinhouses.
/// Returns empty list if persona has no accounts.
/// </summary>
[ServiceBinding(typeof(IQueryHandler<GetCoinhouseBalancesQuery, IReadOnlyList<BalanceDto>>))]
public class GetCoinhouseBalancesQueryHandler : IQueryHandler<GetCoinhouseBalancesQuery, IReadOnlyList<BalanceDto>>
{
    private readonly ICoinhouseRepository _coinhouses;

    public GetCoinhouseBalancesQueryHandler(ICoinhouseRepository coinhouses)
    {
        _coinhouses = coinhouses;
    }

    public Task<IReadOnlyList<BalanceDto>> HandleAsync(
        GetCoinhouseBalancesQuery query,
        CancellationToken cancellationToken = default)
    {
        Guid accountId = ExtractAccountId(query.PersonaId);
        CoinHouseAccount? account = _coinhouses.GetAccountFor(accountId);

        if (account == null)
        {
            return Task.FromResult<IReadOnlyList<BalanceDto>>(Array.Empty<BalanceDto>());
        }

        // For now, return single balance (will be enhanced when we have multi-coinhouse support)
        BalanceDto balance = BalanceDto.Create(
            query.PersonaId,
            account.CoinHouse!.CoinhouseTag,
            account.Balance,
            account.LastAccessedAt);

        return Task.FromResult<IReadOnlyList<BalanceDto>>(new[] { balance });
    }

    private static Guid ExtractAccountId(PersonaId personaId)
    {
        string[] parts = personaId.ToString().Split(':');
        if (parts.Length != 2)
        {
            throw new ArgumentException($"Invalid PersonaId format: {personaId}");
        }

        if (Guid.TryParse(parts[1], out Guid guid))
        {
            return guid;
        }

        return Guid.NewGuid(); // TODO: Deterministic Guid generation
    }
}

