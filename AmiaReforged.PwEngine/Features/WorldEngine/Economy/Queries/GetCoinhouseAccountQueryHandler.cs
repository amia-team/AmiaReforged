using System;
using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Accounts;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Queries;

/// <summary>
/// Returns the account summary for a persona at a given coinhouse, including debit and credit tallies.
/// </summary>
[ServiceBinding(typeof(IQueryHandler<GetCoinhouseAccountQuery, CoinhouseAccountQueryResult?>))]
public sealed class GetCoinhouseAccountQueryHandler : IQueryHandler<GetCoinhouseAccountQuery, CoinhouseAccountQueryResult?>
{
    private readonly ICoinhouseRepository _coinhouses;

    public GetCoinhouseAccountQueryHandler(ICoinhouseRepository coinhouses)
    {
        _coinhouses = coinhouses;
    }

    public async Task<CoinhouseAccountQueryResult?> HandleAsync(
        GetCoinhouseAccountQuery query,
        CancellationToken cancellationToken = default)
    {
    Guid accountId = PersonaAccountId.ForCoinhouse(query.Persona, query.Coinhouse);
        CoinhouseAccountDto? account = await _coinhouses.GetAccountForAsync(accountId, cancellationToken);

        if (account is null)
        {
            return null;
        }

        CoinhouseDto? coinhouse = account.Coinhouse ??
            await _coinhouses.GetByIdAsync(account.CoinHouseId, cancellationToken);

        if (coinhouse is null)
        {
            return null;
        }

        if (!string.Equals(coinhouse.Tag.Value, query.Coinhouse.Value, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        CoinhouseAccountSummary summary = new()
        {
            CoinhouseId = coinhouse.Id,
            CoinhouseTag = coinhouse.Tag,
            Debit = account.Debit,
            Credit = account.Credit,
            OpenedAt = account.OpenedAt,
            LastAccessedAt = account.LastAccessedAt
        };

        return new CoinhouseAccountQueryResult
        {
            AccountExists = true,
            Account = summary
        };
    }
}
