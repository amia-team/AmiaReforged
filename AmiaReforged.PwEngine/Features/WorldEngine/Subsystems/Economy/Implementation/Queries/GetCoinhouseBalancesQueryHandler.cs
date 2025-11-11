using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Accounts;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.DTOs;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Queries;

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

    public async Task<IReadOnlyList<BalanceDto>> HandleAsync(
        GetCoinhouseBalancesQuery query,
        CancellationToken cancellationToken = default)
    {
        if (!Guid.TryParse(query.PersonaId.Value, out Guid holderId))
        {
            return Array.Empty<BalanceDto>();
        }

        IReadOnlyList<CoinhouseAccountDto> accounts = await _coinhouses
            .GetAccountsForHolderAsync(holderId, cancellationToken);

        if (accounts.Count == 0)
        {
            return Array.Empty<BalanceDto>();
        }

        List<BalanceDto> balances = new(accounts.Count);
        Dictionary<long, CoinhouseDto> coinhouseCache = new();

        foreach (CoinhouseAccountDto account in accounts)
        {
            CoinhouseDto? coinhouse = account.Coinhouse;

            if (coinhouse is null)
            {
                if (!coinhouseCache.TryGetValue(account.CoinHouseId, out coinhouse))
                {
                    coinhouse = await _coinhouses.GetByIdAsync(account.CoinHouseId, cancellationToken);
                    if (coinhouse is null)
                    {
                        continue;
                    }

                    coinhouseCache[account.CoinHouseId] = coinhouse;
                }
            }

            balances.Add(BalanceDto.Create(
                query.PersonaId,
                coinhouse.Tag,
                account.Balance,
                account.LastAccessedAt));
        }

        return balances;
    }
}

