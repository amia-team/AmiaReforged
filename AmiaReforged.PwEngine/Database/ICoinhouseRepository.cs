using AmiaReforged.PwEngine.Database.Entities.Economy.Treasuries;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Accounts;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

namespace AmiaReforged.PwEngine.Database;

public interface ICoinhouseRepository
{
    void AddNewCoinhouse(CoinHouse newCoinhouse);
    Task<CoinhouseAccountDto?> GetAccountForAsync(Guid id, CancellationToken cancellationToken = default);
    Task SaveAccountAsync(CoinhouseAccountDto account, CancellationToken cancellationToken = default);
    Task<CoinhouseDto?> GetByTagAsync(CoinhouseTag tag, CancellationToken cancellationToken = default);
    Task<CoinhouseDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
    CoinHouse? GetSettlementCoinhouse(SettlementId settlementId);
    bool TagExists(CoinhouseTag tag);
    bool SettlementHasCoinhouse(SettlementId settlementId);
}
