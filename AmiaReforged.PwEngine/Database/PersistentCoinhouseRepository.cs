using AmiaReforged.PwEngine.Database.Entities.Economy.Treasuries;
using AmiaReforged.PwEngine.Features.WorldEngine.SharedKernel.ValueObjects;
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

        CoinHouseAccount? account = ctx.CoinHouseAccounts
            .Include(x => x.AccountHolders)
            .Include(x => x.Receipts)
            .FirstOrDefault(a => a.Id == id || (a.AccountHolders != null && a.AccountHolders.Any(x => x.HolderId == id)));

        return account;
    }

    public CoinHouse? GetSettlementCoinhouse(SettlementId settlementId)
    {
        using PwEngineContext ctx = factory.CreateDbContext();

        // Implicit conversion from SettlementId to int
        CoinHouse? coinhouse = ctx.CoinHouses.FirstOrDefault(x => x.Settlement == settlementId);

        return coinhouse;
    }

    public CoinHouse? GetByTag(CoinhouseTag tag)
    {
        using PwEngineContext ctx = factory.CreateDbContext();

        // Implicit conversion from CoinhouseTag to string
        CoinHouse? coinhouse = ctx.CoinHouses.FirstOrDefault(x => x.Tag == tag);

        return coinhouse;
    }

    public bool TagExists(CoinhouseTag tag)
    {
        using PwEngineContext ctx = factory.CreateDbContext();

        return ctx.CoinHouses.Any(x => x.Tag == tag);
    }

    public bool SettlementHasCoinhouse(SettlementId settlementId)
    {
        using PwEngineContext ctx = factory.CreateDbContext();

        return ctx.CoinHouses.Any(x => x.Settlement == settlementId);
    }
}
