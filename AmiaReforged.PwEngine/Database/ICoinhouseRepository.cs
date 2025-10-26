using AmiaReforged.PwEngine.Database.Entities.Shops;

namespace AmiaReforged.PwEngine.Database;

public interface ICoinhouseRepository
{
    void AddNewCoinhouse(CoinHouse newCoinhouse);
    CoinHouse? GetAccountFor(Guid id);
    CoinHouse? GetSettlementCoinhouse(int settlementId);
    CoinHouse? GetByTag(string tag);
    bool TagExists(string tag);
    bool SettlementHasCoinhouse(int settlementId);
}
