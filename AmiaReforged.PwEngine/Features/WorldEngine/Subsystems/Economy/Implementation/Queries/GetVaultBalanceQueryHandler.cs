using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Queries;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Treasuries;
using Anvil.Services;
namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Queries;
[ServiceBinding(typeof(IQueryHandler<GetVaultBalanceQuery, int>))]
[ServiceBinding(typeof(IQueryHandlerMarker))]
public sealed class GetVaultBalanceQueryHandler : IQueryHandler<GetVaultBalanceQuery, int>
{
    private readonly IVaultRepository _vaults;
    public GetVaultBalanceQueryHandler(IVaultRepository vaults)
    {
        _vaults = vaults ?? throw new ArgumentNullException(nameof(vaults));
    }
    public async Task<int> HandleAsync(
        GetVaultBalanceQuery query,
        CancellationToken cancellationToken = default)
    {
        return await _vaults.GetBalanceAsync(
            query.Owner.Value,
            query.AreaResRef,
            cancellationToken);
    }
}
