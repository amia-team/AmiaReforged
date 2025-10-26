using AmiaReforged.PwEngine.Database.Entities.Shops;
using Anvil.Services;
using NLog;

namespace AmiaReforged.PwEngine.Database;

[ServiceBinding(typeof(PersistentCoinhouseRepository))]
public class PersistentCoinhouseRepository(PwContextFactory factory) : ICoinhouseRepository
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public void AddNewCoinhouse(CoinHouse newCoinhouse)
    {
        using PwEngineContext ctx = factory.CreateDbContext();

        try
        {
            ctx.CoinHouses.Add(newCoinhouse);
            ctx.SaveChanges();
        }
        catch (Exception e)
        {
            Log.Error(e);
        }
    }

    public CoinHouse? GetAccountFor(Guid id)
    {
        using PwEngineContext ctx = factory.CreateDbContext();

        CoinHouse? account = ctx.CoinHouses.FirstOrDefault(x => x.AccountHolderId == id);

        return account;
    }

    public CoinHouse? GetSettlementCoinhouse(int settlementId)
    {
        using PwEngineContext ctx = factory.CreateDbContext();

        CoinHouse? coinhouse = ctx.CoinHouses.FirstOrDefault(x => x.Settlement == settlementId);

        return coinhouse;
    }

    public CoinHouse? GetByTag(string tag)
    {
        using PwEngineContext ctx = factory.CreateDbContext();

        CoinHouse? coinhouse = ctx.CoinHouses.FirstOrDefault(x => x.Tag == tag);

        return coinhouse;
    }

    public bool TagExists(string tag)
    {
        using PwEngineContext ctx = factory.CreateDbContext();

        return ctx.CoinHouses.Any(x => x.Tag == tag);
    }

    public bool SettlementHasCoinhouse(int settlementId)
    {
        using PwEngineContext ctx = factory.CreateDbContext();

        return ctx.CoinHouses.Any(x => x.Settlement == settlementId);
    }
}

public interface ICoinhouseRepository
{
    void AddNewCoinhouse(CoinHouse newCoinhouse);
    CoinHouse? GetAccountFor(Guid id);
    CoinHouse? GetSettlementCoinhouse(int settlementId);
    CoinHouse? GetByTag(string tag);
    bool TagExists(string tag);
    bool SettlementHasCoinhouse(int settlementId);
}
