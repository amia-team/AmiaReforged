using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Accounts;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Queries;
/// <summary>
/// Handles GetBalanceQuery - retrieves a persona's balance at a specific coinhouse.
/// Returns null if the persona has no account at the coinhouse.
/// </summary>
[ServiceBinding(typeof(IQueryHandler<GetBalanceQuery, int?>))]
public class GetBalanceQueryHandler : IQueryHandler<GetBalanceQuery, int?>
{
    private readonly ICoinhouseRepository _coinhouses;
    public GetBalanceQueryHandler(ICoinhouseRepository coinhouses)
    {
        _coinhouses = coinhouses;
    }
    public async Task<int?> HandleAsync(GetBalanceQuery query, CancellationToken cancellationToken = default)
    {
        CoinhouseDto? coinhouse = await _coinhouses.GetByTagAsync(query.Coinhouse, cancellationToken);
        if (coinhouse is null)
        {
            return null;
        }

        Guid accountId = PersonaAccountId.ForCoinhouse(query.PersonaId, query.Coinhouse);
        CoinhouseAccountDto? account = await _coinhouses.GetAccountForAsync(accountId, cancellationToken);

        if (account is null)
        {
            return null;
        }

        return account.Balance;
    }
}
