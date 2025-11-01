using System;
using System.Collections.Generic;
using System.Linq;
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
        CoinhouseDto? coinhouse = await _coinhouses.GetByTagAsync(query.Coinhouse, cancellationToken);
        if (coinhouse is null)
        {
            return null;
        }

        Guid accountId = PersonaAccountId.ForCoinhouse(query.Persona, query.Coinhouse);
        CoinhouseAccountDto? account = await _coinhouses.GetAccountForAsync(accountId, cancellationToken);

        if (account is null && Guid.TryParse(query.Persona.Value, out Guid holderGuid))
        {
            IReadOnlyList<CoinhouseAccountDto>? accounts = await _coinhouses
                .GetAccountsForHolderAsync(holderGuid, cancellationToken);

            if (accounts is not null)
            {
                account = accounts.FirstOrDefault(a => a.CoinHouseId == coinhouse.Id);
            }
        }

        if (account is null)
        {
            return null;
        }

        if (account.CoinHouseId != coinhouse.Id)
        {
            return null;
        }

        if (account.Coinhouse is null)
        {
            account = account with { Coinhouse = coinhouse };
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
            Account = summary,
            Holders = account.Holders ?? Array.Empty<CoinhouseAccountHolderDto>()
        };
    }
}
