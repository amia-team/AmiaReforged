using System;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Treasuries;
using Anvil.Services;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Treasuries;

[ServiceBinding(typeof(VaultService))]
public class VaultService
{
    private readonly IVaultRepository _repo;

    public VaultService(IVaultRepository repo)
    {
        _repo = repo ?? throw new ArgumentNullException(nameof(repo));
    }

    public Task<int> GetBalanceAsync(CharacterId owner, string areaResRef, CancellationToken ct = default)
        => _repo.GetBalanceAsync(owner.Value, areaResRef, ct);

    public Task<int> DepositAsync(CharacterId owner, string areaResRef, int amount, CancellationToken ct = default)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "Deposit must be greater than zero.");
        return _repo.DepositAsync(owner.Value, areaResRef, amount, ct);
    }

    public Task<int> WithdrawAsync(CharacterId owner, string areaResRef, int amount, CancellationToken ct = default)
    {
        if (amount <= 0) throw new ArgumentOutOfRangeException(nameof(amount), "Withdraw must be greater than zero.");
        return _repo.WithdrawAsync(owner.Value, areaResRef, amount, ct);
    }
}

