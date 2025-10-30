using System.Threading;
using System.Threading.Tasks;
using AmiaReforged.PwEngine.Database.Entities.Economy.Treasuries;
using AmiaReforged.PwEngine.Features.WorldEngine.Economy.Accounts;
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

    public async Task<CoinhouseAccountDto?> GetAccountForAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using PwEngineContext ctx = factory.CreateDbContext();

        CoinHouseAccount? account = await ctx.CoinHouseAccounts
            .Include(x => x.CoinHouse)
            .Include(x => x.AccountHolders)
            .Include(x => x.Receipts)
            .FirstOrDefaultAsync(
                a => a.Id == id ||
                     (a.AccountHolders != null && a.AccountHolders.Any(x => x.HolderId == id)),
                cancellationToken);

        return account?.ToDto();
    }

    public async Task SaveAccountAsync(CoinhouseAccountDto account, CancellationToken cancellationToken = default)
    {
        await using PwEngineContext ctx = factory.CreateDbContext();

        CoinHouseAccount? existing = await ctx.CoinHouseAccounts
            .Include(a => a.AccountHolders)
            .FirstOrDefaultAsync(a => a.Id == account.Id, cancellationToken);

        if (existing is null)
        {
            CoinHouseAccount entity = account.ToEntity();
            ctx.CoinHouseAccounts.Add(entity);
        }
        else
        {
            existing.UpdateFrom(account);
        }

        await ctx.SaveChangesAsync(cancellationToken);
    }

    public CoinHouse? GetSettlementCoinhouse(SettlementId settlementId)
    {
        using PwEngineContext ctx = factory.CreateDbContext();

        // Implicit conversion from SettlementId to int
        CoinHouse? coinhouse = ctx.CoinHouses.FirstOrDefault(x => x.Settlement == settlementId);

        return coinhouse;
    }

    public async Task<CoinhouseDto?> GetByTagAsync(CoinhouseTag tag, CancellationToken cancellationToken = default)
    {
        await using PwEngineContext ctx = factory.CreateDbContext();

        // Implicit conversion from CoinhouseTag to string
        CoinHouse? coinhouse = await ctx.CoinHouses
            .FirstOrDefaultAsync(x => x.Tag == tag, cancellationToken);

        return coinhouse?.ToDto();
    }

    public async Task<CoinhouseDto?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        await using PwEngineContext ctx = factory.CreateDbContext();

        CoinHouse? coinhouse = await ctx.CoinHouses.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        return coinhouse?.ToDto();
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
