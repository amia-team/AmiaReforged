using AmiaReforged.PwEngine.Database;
using AmiaReforged.PwEngine.Database.Entities.Economy.Treasuries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using Anvil.Services;
namespace AmiaReforged.PwEngine.Features.WorldEngine.Economy.Queries;
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
    public Task<int?> HandleAsync(GetBalanceQuery query, CancellationToken cancellationToken = default)
    {
        // Validate coinhouse exists
        CoinHouse? coinhouse = _coinhouses.GetByTag(query.Coinhouse);
        if (coinhouse == null)
        {
            return Task.FromResult<int?>(null);
        }
        // Extract account ID from PersonaId
        Guid accountId = ExtractAccountId(query.PersonaId);
        // Get account
        CoinHouseAccount? account = _coinhouses.GetAccountFor(accountId);
        if (account == null)
        {
            return Task.FromResult<int?>(null);
        }
        // Return balance (debit - credit)
        return Task.FromResult<int?>(account.Balance);
    }
    private static Guid ExtractAccountId(PersonaId personaId)
    {
        // PersonaId format: "Type:Value"
        string[] parts = personaId.ToString().Split(':');
        if (parts.Length != 2)
        {
            throw new ArgumentException($"Invalid PersonaId format: {personaId}");
        }
        if (Guid.TryParse(parts[1], out Guid guid))
        {
            return guid;
        }
        // For non-Guid persona types, generate deterministic Guid
        return Guid.NewGuid(); // TODO: Implement deterministic Guid generation from string
    }
}
