using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Accounts;
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

    public async Task<IReadOnlyList<BalanceDto>> HandleAsync(
        GetCoinhouseBalancesQuery query,
        CancellationToken cancellationToken = default)
    {
        Guid accountId = PersonaAccountId.From(query.PersonaId);
        CoinhouseAccountDto? account = await _coinhouses.GetAccountForAsync(accountId, cancellationToken);

        if (account is null)
        {
            return Array.Empty<BalanceDto>();
        }

        CoinhouseDto? coinhouse = account.Coinhouse ??
            await _coinhouses.GetByIdAsync(account.CoinHouseId, cancellationToken);

        if (coinhouse is null)
        {
            return Array.Empty<BalanceDto>();
        }

        BalanceDto balance = BalanceDto.Create(
            query.PersonaId,
            coinhouse.Tag,
            account.Balance,
            account.LastAccessedAt);

        return new[] { balance };
    }
}

