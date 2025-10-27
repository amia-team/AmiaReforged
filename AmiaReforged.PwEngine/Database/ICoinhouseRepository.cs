using AmiaReforged.PwEngine.Database.Entities.Economy.Treasuries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;

namespace AmiaReforged.PwEngine.Database;

public interface ICoinhouseRepository
{
    void AddNewCoinhouse(CoinHouse newCoinhouse);
    CoinHouseAccount? GetAccountFor(Guid id);
    CoinHouse? GetSettlementCoinhouse(SettlementId settlementId);
    CoinHouse? GetByTag(CoinhouseTag tag);
    bool TagExists(CoinhouseTag tag);
    bool SettlementHasCoinhouse(SettlementId settlementId);
}
