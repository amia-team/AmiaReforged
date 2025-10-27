using AmiaReforged.PwEngine.Database.Entities.Economy.Treasuries;

namespace AmiaReforged.PwEngine.Database;

public interface ICoinhouseRepository
{
    void AddNewCoinhouse(CoinHouse newCoinhouse);
    CoinHouseAccount? GetAccountFor(Guid id);
    CoinHouse? GetSettlementCoinhouse(int settlementId);
    CoinHouse? GetByTag(string tag);
    bool TagExists(string tag);
    bool SettlementHasCoinhouse(int settlementId);
}
