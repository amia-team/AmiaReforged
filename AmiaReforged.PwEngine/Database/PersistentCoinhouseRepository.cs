using AmiaReforged.PwEngine.Database.Entities.Economy.Treasuries;
using Anvil.Services;
using Microsoft.EntityFrameworkCore;
using NLog;

namespace AmiaReforged.PwEngine.Database;

[ServiceBinding(typeof(ICoinhouseRepository))]
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

    public CoinHouseAccount? GetAccountFor(Guid id)
    {
        using PwEngineContext ctx = factory.CreateDbContext();

        CoinHouseAccount? account = ctx.CoinHouseAccounts.Include(x => x.AccountHolders)
            .FirstOrDefault(a => a.AccountHolders != null && a.AccountHolders.Any(x => x.HolderId == id));

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
