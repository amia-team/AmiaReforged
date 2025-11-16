using System;
using System.Threading;
using System.Threading.Tasks;
using AmiaReforged.PwEngine.Database.Entities.Economy.Treasuries;

namespace AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Treasuries;

public interface IVaultRepository
{
    Task<Vault> GetOrCreateAsync(Guid ownerCharacterId, string areaResRef, CancellationToken ct = default);
    Task<int> GetBalanceAsync(Guid ownerCharacterId, string areaResRef, CancellationToken ct = default);
    Task<int> DepositAsync(Guid ownerCharacterId, string areaResRef, int amount, CancellationToken ct = default);
    Task<int> WithdrawAsync(Guid ownerCharacterId, string areaResRef, int amount, CancellationToken ct = default);
}

