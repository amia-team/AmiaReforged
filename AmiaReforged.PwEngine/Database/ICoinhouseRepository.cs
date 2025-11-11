using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AmiaReforged.PwEngine.Database.Entities.Economy.Treasuries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
using AmiaReforged.PwEngine.Features.WorldEngine.Subsystems.Economy.Implementation.Accounts;

namespace AmiaReforged.PwEngine.Database;

public interface ICoinhouseRepository
{
    void AddNewCoinhouse(CoinHouse newCoinhouse);
    CoinHouse? GetCoinhouseByTag(CoinhouseTag tag);
    Task<CoinhouseAccountDto?> GetAccountForAsync(Guid id, CancellationToken cancellationToken = default);
    Task SaveAccountAsync(CoinhouseAccountDto account, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CoinhouseAccountDto>> GetAccountsForHolderAsync(Guid holderId, CancellationToken cancellationToken = default);
    Task<CoinhouseDto?> GetByTagAsync(CoinhouseTag tag, CancellationToken cancellationToken = default);
    Task<CoinhouseDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    CoinHouse? GetSettlementCoinhouse(SettlementId settlementId);
    bool TagExists(CoinhouseTag tag);
    bool SettlementHasCoinhouse(SettlementId settlementId);
}
