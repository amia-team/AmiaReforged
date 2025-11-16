using System;
using System.Threading;
using System.Threading.Tasks;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.Personas;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Treasuries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Shops.PlayerStalls;

[ServiceBinding(typeof(ReeveFundsService))]
public sealed class ReeveFundsService
{
    private readonly VaultService _vaults;

    public ReeveFundsService(VaultService vaults)
    {
        _vaults = vaults ?? throw new ArgumentNullException(nameof(vaults));
    }

    public Task<int> GetHeldFundsAsync(PersonaId persona, string? areaResRef, CancellationToken ct = default)
    {
        Guid guid = PersonaId.ToGuid(persona);
        CharacterId owner = CharacterId.From(guid);
        string area = areaResRef ?? string.Empty;
        return _vaults.GetBalanceAsync(owner, area, ct);
    }

    public async Task<int> ReleaseHeldFundsAsync(PersonaId persona, string? areaResRef, int requestedAmount,
        Func<int, Task<bool>> grantToPlayerAsync,
        CancellationToken ct = default)
    {
        Guid guid = PersonaId.ToGuid(persona);
        CharacterId owner = CharacterId.From(guid);
        string area = areaResRef ?? string.Empty;

        int amount = requestedAmount <= 0
            ? await _vaults.GetBalanceAsync(owner, area, ct)
            : requestedAmount;
        if (amount <= 0)
        {
            return 0;
        }

        int withdrawn = await _vaults.WithdrawAsync(owner, area, amount, ct);
        if (withdrawn <= 0)
        {
            return 0;
        }

        bool granted = await grantToPlayerAsync(withdrawn);
        if (!granted)
        {
            // Return funds if granting failed
            await _vaults.DepositAsync(owner, area, withdrawn, ct);
            return 0;
        }

        return withdrawn;
    }
}
